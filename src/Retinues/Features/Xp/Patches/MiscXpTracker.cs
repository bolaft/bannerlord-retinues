using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Retinues.Features.Xp.Behaviors;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem.Roster;

namespace Retinues.Features.Xp.Patches
{
    /// <summary>
    /// Harmony patch for TroopRoster.AddXpToTroopAtIndex.
    /// Tracks and awards XP to custom troops, skipping blacklisted sources and normalizing XP gain.
    /// </summary>
    [HarmonyPatch]
    internal static class MiscXpTracker
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
            "MapEventParty.CommitXpGain", // separately handled in BattleXpTracker
        };

        public const float xpMultiplier = 0.1f; // 10% of the original XP
        public const float xpMultiplierNonMain = 0.25f; // 25% of XP for non-main parties

        /// <summary>
        /// Postfix: awards normalized XP to custom troops, skips blacklisted sources and non-player factions.
        /// </summary>
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
                // Normalize XP gain
                xpAmount = (int)(xpAmount * xpMultiplier);

                // Reduce XP for non-main parties
                if (party.IsMainParty == false)
                    xpAmount = (int)(xpAmount * xpMultiplierNonMain);

                if (xpAmount <= 0)
                    return; // no XP to add after multiplier

                Log.Debug(
                    $"Awarding {xpAmount} XP to {troop.Name} in party {party} ({Caller.GetLabel()})."
                );
                TroopXpBehavior.Add(troop, xpAmount);
            }
        }
    }
}
