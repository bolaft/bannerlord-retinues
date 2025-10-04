using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Retinues.Core.Features.Xp.Behaviors;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem.Roster;

namespace Retinues.Core.Features.Xp.Patches
{
    [HarmonyPatch]
    internal static class TroopXpTracker
    {
        static MethodBase TargetMethod() =>
            AccessTools.Method(
                typeof(TroopRoster),
                "AddXpToTroopAtIndex",
                [typeof(int), typeof(int)]
            );

        private static readonly HashSet<string> _blocked = new(StringComparer.Ordinal)
        {
            "CampaignEvents.OnTroopRecruited", // skip recruitment XP
            "PartyScreenLogic.TransferTroop", // skip transfer XP
            "MapEventParty.CommitXpGain", // battle xp handled by the behavior directly
        };

        public const float xpMultiplier = 0.02f; // 2% of the original XP

        [SafeMethod]
        static void Postfix(TroopRoster __instance, int index, int xpAmount)
        {
            if (Caller.IsBlocked(_blocked))
                return; // blacklisted xp source

            if (xpAmount <= 0)
                return; // no XP to add

            if (__instance == null)
                return; // defensive

            var party = new WRoster(__instance).Party;

            if (party?.PlayerFaction == null)
                return; // non-player faction, skip

            var co = __instance.GetCharacterAtIndex(index);
            var troop = new WCharacter(co);

            // Add to XP pool only if the troop is custom
            if (troop.IsCustom)
            {
                xpAmount = (int)(xpAmount * xpMultiplier);
                if (xpAmount <= 0)
                    return; // no XP to add after multiplier
                Log.Info($"TroopXpTracker: Adding {xpAmount} XP to {troop.StringId}.");
                TroopXpBehavior.Add(troop, xpAmount);
            }
        }
    }
}
