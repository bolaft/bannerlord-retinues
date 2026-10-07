using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.ObjectSystem;

namespace Retinues.Behaviors.Experience.Patches
{
    /// <summary>
    /// Record the roster element missing from external XP crash reports. Does not repair
    /// rosters, change XP, or suppress the exception. Work is limited to failed calls.
    /// </summary>
    [HarmonyPatch]
    internal static class XpFaultDiagnosticsPatch
    {
        private static readonly HashSet<string> Reported = new(StringComparer.Ordinal);

        [StaticClearAction]
        public static void Reset() => Reported.Clear();

#if BL12
        [HarmonyPatch(typeof(TroopRoster), "ClampXp")]
        [HarmonyFinalizer]
        private static Exception OnClampXp(TroopRoster __instance, int index, Exception __exception)
        {
            if (__exception == null)
                return null;
            try
            {
                var element = __instance != null && index >= 0 && index < __instance.Count
                    ? __instance.GetElementCopyAtIndex(index) : default;
                return Report(SkillPointExperiencePatches.GetOwnerParty(__instance), __instance, element, __exception);
            }
            catch { return __exception; }
        }
#else
        [HarmonyPatch(typeof(PartyBase), "OnXpChanged")]
        [HarmonyFinalizer]
        private static Exception OnXpChanged(PartyBase __instance, TroopRoster roster,
            TroopRosterElement element, Exception __exception) => Report(__instance, roster, element, __exception);
#endif

        internal static Exception Report(PartyBase party, TroopRoster roster,
            TroopRosterElement element, Exception error)
        {
            if (error == null)
                return null;
            try
            {
                if (Reported.Count >= 16)
                    return error;
                var troop = element.Character;
                var registered = string.IsNullOrEmpty(troop?.StringId)
                    ? null : MBObjectManager.Instance?.GetObject<CharacterObject>(troop.StringId);
                var owner = party?.MobileParty?.StringId ?? party?.Settlement?.StringId ?? "<unknown>";
                var rosterKind = roster == null ? "<null>" : party == null ? "<unknown>"
                    : ReferenceEquals(roster, party.PrisonRoster) ? "prisoners" : "members";
                var details = $"party={owner}; roster={rosterKind}; "
                    + DescribeElement(element)
                    + $"; registered={(registered == null ? "missing" : ReferenceEquals(registered, troop) ? "same instance" : "different instance")}";
                if (Reported.Add(details))
                    Log.Error($"[XP fault] {details}; {error.GetType().Name}: {error.Message}");
            }
            catch { /* Diagnostics must never replace the original failure. */ }
            return error;
        }

        internal static string DescribeElement(TroopRosterElement element)
        {
            var troop = element.Character;
            var targets = troop?.UpgradeTargets;
            string wounded = "<unavailable>";
            // The game's wounded getter itself dereferences Character and HeroObject.
            try { if (troop != null) wounded = element.WoundedNumber.ToString(); }
            catch { }
            var targetIds = targets == null ? "<null array>"
                : targets.Length == 0 ? "<empty>"
                : string.Join(",", targets.Take(16).Select(t => t?.StringId ?? "<null target>"))
                    + (targets.Length > 16 ? ",..." : "");
            return $"troop={troop?.StringId ?? "<null>"}; count={element.Number}; wounded={wounded}; "
                + $"xp={element.Xp}; upgrades=[{targetIds}]";
        }
    }
}
