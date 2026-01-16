using System.Collections;
using System.ComponentModel;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Library;

namespace Retinues.Configuration.Menu.Patches
{
    internal static class MCMDependencies
    {
        internal static readonly string SettingsPropertyVMTypeName =
            "MCM.UI.GUI.ViewModels.SettingsPropertyVM";
        internal static readonly string SettingsVMGroupsPropName = "SettingPropertyGroups";
        internal static readonly string GroupGroupsPropName = "SettingPropertyGroups";
        internal static readonly string GroupPropsPropName = "SettingProperties";
        internal static readonly string PropDefPropName = "SettingPropertyDefinition";
        internal static readonly string PropSettingsVMPropName = "SettingsVM";
        internal static readonly string DefIdPropName = "Id";

        /// <summary>
        /// Check if MCM types are available to patch against.
        /// </summary>
        internal static bool CanPatch()
        {
            return AccessTools.TypeByName(SettingsPropertyVMTypeName) != null;
        }

        /// <summary>
        /// Get the setting ID from a SettingsPropertyVM instance.
        /// </summary>
        internal static string TryGetSettingId(object settingsPropertyVm)
        {
            if (settingsPropertyVm == null)
                return null;

            var t = settingsPropertyVm.GetType();
            var defProp = AccessTools.Property(t, PropDefPropName);
            if (defProp == null)
                return null;

            var def = defProp.GetValue(settingsPropertyVm);
            if (def == null)
                return null;

            var idProp = AccessTools.Property(def.GetType(), DefIdPropName);
            return idProp?.GetValue(def) as string;
        }

        /// <summary>
        /// Get the SettingsVM from a SettingsPropertyVM instance.
        /// </summary>
        internal static object TryGetSettingsVM(object settingsPropertyVm)
        {
            if (settingsPropertyVm == null)
                return null;

            var t = settingsPropertyVm.GetType();
            var p = AccessTools.Property(t, PropSettingsVMPropName);
            return p?.GetValue(settingsPropertyVm);
        }

        /// <summary>
        /// Refresh visibility state for all groups & properties under the given SettingsVM.
        /// </summary>
        internal static void RefreshAllVisibility(object settingsVm)
        {
            if (settingsVm == null)
                return;

            try
            {
                var prop = AccessTools.Property(settingsVm.GetType(), SettingsVMGroupsPropName);
                if (prop?.GetValue(settingsVm) is not IEnumerable groups)
                    return;

                foreach (var g in groups)
                    RefreshGroupRecursive(g);
            }
            catch
            {
                // never break MCM
            }
        }

        /// <summary>
        /// Refresh visibility state for the given group and its children.
        /// </summary>
        private static void RefreshGroupRecursive(object group)
        {
            if (group == null)
                return;

            if (group is ViewModel vmGroup)
                vmGroup.OnPropertyChanged("IsGroupVisible");

            var propsProp = AccessTools.Property(group.GetType(), GroupPropsPropName);
            if (propsProp?.GetValue(group) is IEnumerable props)
            {
                foreach (var p in props)
                {
                    if (p is ViewModel vmProp)
                        vmProp.OnPropertyChanged("IsSettingVisible");
                }
            }

            var groupsProp = AccessTools.Property(group.GetType(), GroupGroupsPropName);
            if (groupsProp?.GetValue(group) is IEnumerable subs)
            {
                foreach (var sub in subs)
                    RefreshGroupRecursive(sub);
            }
        }
    }

    internal static class MCMVisibilityBridge
    {
        private static MethodInfo _isVisibleById;
        private static MethodInfo _isVisibleByOption;

        /// <summary>
        /// Check if the setting with the given ID is visible according to MCM.
        /// </summary>
        internal static bool IsVisible(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return true;

            try
            {
                // Prefer option-based visibility if possible (more robust if ids get normalized).
                if (SettingsManager.TryGetOption(id, out var opt) && opt != null)
                {
                    var byOpt = GetIsVisibleByOption();
                    if (byOpt != null)
                        return SafeInvokeBool(byOpt, null, [opt]);
                }

                var byId = GetIsVisibleById();
                if (byId != null)
                    return SafeInvokeBool(byId, null, [id]);

                return true;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Get the IsVisibleInMCM(string id) method.
        /// </summary>
        private static MethodInfo GetIsVisibleById()
        {
            _isVisibleById ??= AccessTools.Method(
                typeof(SettingsManager),
                "IsVisibleInMCM",
                [typeof(string)]
            );

            return _isVisibleById;
        }

        /// <summary>
        /// Get the IsVisibleInMCM(IOption option) method.
        /// </summary>
        private static MethodInfo GetIsVisibleByOption()
        {
            _isVisibleByOption ??= AccessTools.Method(
                typeof(SettingsManager),
                "IsVisibleInMCM",
                [typeof(IOption)]
            );

            return _isVisibleByOption;
        }

        /// <summary>
        /// Safely invoke a MethodInfo that returns bool.
        /// </summary>
        private static bool SafeInvokeBool(MethodInfo mi, object instance, object[] args)
        {
            var r = mi.Invoke(instance, args);
            if (r is bool b)
                return b;
            return true;
        }
    }

    // Patch 1: SettingsPropertyVM.IsSettingVisible getter -> additionally check the dependency graph.
    [HarmonyPatch]
    internal static class SettingsPropertyVM_IsSettingVisible_Patch
    {
        /// <summary>
        /// Only patch if MCM types are available to patch against.
        /// </summary>
        private static bool Prepare()
        {
            return MCMDependencies.CanPatch();
        }

        /// <summary>
        /// Target method to patch.
        /// </summary>
        private static MethodBase TargetMethod()
        {
            var t = AccessTools.TypeByName(MCMDependencies.SettingsPropertyVMTypeName);
            if (t == null)
                return null;

            return AccessTools.PropertyGetter(t, "IsSettingVisible");
        }

        /// <summary>
        /// Postfix for IsSettingVisible getter.
        /// </summary>
        private static void Postfix(object __instance, ref bool __result)
        {
            if (!__result)
                return;

            try
            {
                var id = MCMDependencies.TryGetSettingId(__instance);
                if (string.IsNullOrWhiteSpace(id))
                    return;

                if (!MCMVisibilityBridge.IsVisible(id))
                    __result = false;
            }
            catch
            {
                // never break MCM
            }
        }
    }

    // Patch 2: SettingsPropertyVM.OnPropertyChanged(...) -> refresh visibility so dependents show/hide immediately.
    [HarmonyPatch]
    internal static class SettingsPropertyVM_OnPropertyChanged_Patch
    {
        /// <summary>
        /// Only patch if MCM types are available to patch against.
        /// </summary>
        private static bool Prepare()
        {
            return MCMDependencies.CanPatch();
        }

        /// <summary>
        /// Target method to patch.
        /// </summary>
        private static MethodBase TargetMethod()
        {
            var t = AccessTools.TypeByName(MCMDependencies.SettingsPropertyVMTypeName);
            if (t == null)
                return null;

            return AccessTools.Method(
                t,
                "OnPropertyChanged",
                [typeof(object), typeof(PropertyChangedEventArgs)]
            );
        }

        /// <summary>
        /// Postfix for OnPropertyChanged.
        /// </summary>
        /// <param name="__instance"></param>
        private static void Postfix(object __instance)
        {
            try
            {
                var settingsVm = MCMDependencies.TryGetSettingsVM(__instance);
                if (settingsVm != null)
                    MCMDependencies.RefreshAllVisibility(settingsVm);
            }
            catch
            {
                // never break MCM
            }
        }
    }
}
