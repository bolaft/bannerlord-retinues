using System.Collections;
using System.ComponentModel;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Library;

namespace Retinues.Configuration.Patches
{
    internal static class MCMDeps
    {
        internal static readonly string SettingsPropertyVMTypeName =
            "MCM.UI.GUI.ViewModels.SettingsPropertyVM";
        internal static readonly string SettingsVMGroupsPropName = "SettingPropertyGroups";
        internal static readonly string GroupGroupsPropName = "SettingPropertyGroups";
        internal static readonly string GroupPropsPropName = "SettingProperties";
        internal static readonly string PropDefPropName = "SettingPropertyDefinition";
        internal static readonly string PropSettingsVMPropName = "SettingsVM";
        internal static readonly string DefIdPropName = "Id";

        internal static bool CanPatch()
        {
            return AccessTools.TypeByName(SettingsPropertyVMTypeName) != null;
        }

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

        internal static object TryGetSettingsVM(object settingsPropertyVm)
        {
            if (settingsPropertyVm == null)
                return null;

            var t = settingsPropertyVm.GetType();
            var p = AccessTools.Property(t, PropSettingsVMPropName);
            return p?.GetValue(settingsPropertyVm);
        }

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

    // Patch 1: SettingsPropertyVM.IsSettingVisible getter -> additionally check our dependency graph.
    [HarmonyPatch]
    internal static class SettingsPropertyVM_IsSettingVisible_Patch
    {
        private static bool Prepare()
        {
            return MCMDeps.CanPatch();
        }

        private static MethodBase TargetMethod()
        {
            var t = AccessTools.TypeByName(MCMDeps.SettingsPropertyVMTypeName);
            if (t == null)
                return null;

            return AccessTools.PropertyGetter(t, "IsSettingVisible");
        }

        private static void Postfix(object __instance, ref bool __result)
        {
            if (!__result)
                return;

            try
            {
                var id = MCMDeps.TryGetSettingId(__instance);
                if (string.IsNullOrWhiteSpace(id))
                    return;

                if (!SettingsManager.IsVisibleInMCM(id))
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
        private static bool Prepare()
        {
            return MCMDeps.CanPatch();
        }

        private static MethodBase TargetMethod()
        {
            var t = AccessTools.TypeByName(MCMDeps.SettingsPropertyVMTypeName);
            if (t == null)
                return null;

            return AccessTools.Method(
                t,
                "OnPropertyChanged",
                new[] { typeof(object), typeof(PropertyChangedEventArgs) }
            );
        }

        private static void Postfix(object __instance)
        {
            try
            {
                var settingsVm = MCMDeps.TryGetSettingsVM(__instance);
                if (settingsVm != null)
                    MCMDeps.RefreshAllVisibility(settingsVm);
            }
            catch
            {
                // never break MCM
            }
        }
    }
}
