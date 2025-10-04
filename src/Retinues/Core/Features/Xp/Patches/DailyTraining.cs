using System;
using System.Reflection;
using HarmonyLib;
using Retinues.Core.Features.Xp.Behaviors;
using Retinues.Core.Game.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;

namespace Retinues.Core.Features.Xp.Patches
{
    [HarmonyPatch(typeof(MobilePartyTrainingBehavior), "OnDailyTickParty")]
    internal static class DailyTrainingContext
    {
        [ThreadStatic]
        internal static MobileParty CurrentParty;

        static void Prefix(MobileParty mobileParty)
        {
            CurrentParty = mobileParty;
        }

        static void Finalizer(Exception __exception)
        {
            CurrentParty = null;
        }
    }

    [HarmonyPatch]
    internal static class RosterAddXpHook
    {
        // Only this exact overload:
        static MethodBase TargetMethod() =>
            AccessTools.Method(
                typeof(TroopRoster),
                "AddXpToTroop",
                [typeof(CharacterObject), typeof(int)]
            );

        // Match the method signature precisely
        static void Postfix(TroopRoster __instance, CharacterObject character, int xpAmount)
        {
            var party = DailyTrainingContext.CurrentParty;
            if (party == null || character == null || xpAmount <= 0)
                return;

            var troop = new WCharacter(character);

            // Add to XP pool only if the troop is custom
            if (troop.IsCustom)
                TroopXpBehavior.Add(troop, xpAmount);
        }
    }
}
