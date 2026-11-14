using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace Retinues.Safety.Uninstall
{
    [HarmonyPatch]
    internal static class UninstallPersistenceGuard
    {
        // SyncData is not an event: removing event listeners alone still saves queued jobs,
        // equipment policies and other Retinues-only types that require our DLL to load.
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods()
        {
            var store = typeof(Campaign).Assembly.GetType("TaleWorlds.CampaignSystem.CampaignBehaviorDataStore", true);
            foreach (string name in new[] { "SaveBehaviorData", "LoadBehaviorData" })
            {
                var method = AccessTools.Method(store, name, new[] { typeof(CampaignBehaviorBase) });
                if (method == null) throw new MissingMethodException(store.FullName, name);
                yield return method;
            }
        }

        [HarmonyPrefix]
        private static bool Prefix(CampaignBehaviorBase __0) => ShouldSync(__0, UninstallBehavior.IsPrepared);

        internal static bool ShouldSync(CampaignBehaviorBase behavior, bool prepared) =>
            !prepared || behavior is UninstallBehavior || behavior.GetType().Assembly != typeof(UninstallBehavior).Assembly;

        internal static void EnsureInstalled()
        {
            foreach (var method in TargetMethods())
                if (Harmony.GetPatchInfo(method)?.Prefixes.Any(p => p.PatchMethod.DeclaringType == typeof(UninstallPersistenceGuard)) != true)
                    throw new InvalidOperationException("The uninstall persistence guard is unavailable on this game version.");
        }
    }
}
