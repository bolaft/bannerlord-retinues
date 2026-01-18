using System;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Domain.Characters.Services.Matching;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Behaviors.Recruitement.Patches
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                    AI Recruit Rules                    //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //
    // This patch is downstream: it remaps what is being recruited
    // based on WSettlement rules. It does not affect base volunteers.
    //
    // Applies only to non-player recruiters (AI parties and garrisons),
    // and only when the settlement has base custom troops.

    /// <summary>
    /// Remaps AI recruitment to vanilla equivalents when settlements enforce base custom troops.
    /// Also enforces SameCultureOnly as a last-resort guard.
    /// </summary>
    [HarmonyPatch(typeof(RecruitmentCampaignBehavior), "ApplyInternal")]
    internal static class Recruitement_ApplyInternal_Patch
    {
        /// <summary>
        /// Prefix that may replace the recruited troop with a vanilla fallback.
        /// </summary>
        [HarmonyPrefix]
        private static void Prefix(
            MobileParty side1Party,
            Settlement settlement,
            Hero individual,
            ref CharacterObject troop,
            int number,
            int bitCode,
            RecruitmentCampaignBehavior.RecruitingDetail detail
        )
        {
            try
            {
                if (troop == null || troop.IsHero)
                    return;

                if (settlement == null)
                    return;

                var ws = WSettlement.Get(settlement);
                if (ws == null)
                    return;

                var wc = WCharacter.Get(troop);
                if (!IsFactionCustomTroop(wc))
                    return;

                if (Settings.SameCultureOnly)
                {
                    var troopCulture = troop.Culture;
                    var settlementCulture = settlement.Culture;

                    if (
                        troopCulture != null
                        && settlementCulture != null
                        && troopCulture != settlementCulture
                    )
                    {
                        ReplaceWithVanillaEquivalent(ws, wc, ref troop);
                        return;
                    }
                }

                // Player gets a separate view override when opening the recruit menu.
                if (side1Party != null && side1Party.IsMainParty)
                    return;

                // If settlement has no base custom troops, do not interfere at all.
                if (ws.GetBaseTroopsFaction() == null)
                    return;

                var recruiter = side1Party != null ? WParty.Get(side1Party) : null;

                // If recruiter is allowed, no action.
                if (!ws.ShouldForceVanillaForRecruiter(recruiter))
                    return;

                ReplaceWithVanillaEquivalent(ws, wc, ref troop);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Recruitement: ApplyInternal patch failed.");
            }
        }

        /// <summary>
        /// Maps a custom troop back to a vanilla equivalent from the settlement culture tree.
        /// </summary>
        private static void ReplaceWithVanillaEquivalent(
            WSettlement ws,
            WCharacter wc,
            ref CharacterObject troop
        )
        {
            var culture = ws.Culture;
            if (culture == null)
                return;

            var root = wc.IsElite ? culture.RootElite : culture.RootBasic;
            root ??= culture.RootBasic ?? culture.RootElite;
            if (root == null)
                return;

            var replacement = CharacterMatcher.PickBestFromTree(wc, root);
            var replacementBase = replacement?.Base;
            if (replacementBase == null)
                return;

            if (replacementBase == troop)
                return;

            troop = replacementBase;
        }

        /// <summary>
        /// Determines if the wrapped character represents a faction custom basic/elite troop.
        /// </summary>
        private static bool IsFactionCustomTroop(WCharacter wc)
        {
            // 'Faction troops' here means the custom basic/elite trees (not retinues).
            if (wc == null)
                return false;

            if (!wc.IsFactionTroop)
                return false;

            if (wc.IsRetinue)
                return false;

            return wc.IsBasic || wc.IsElite;
        }
    }
}
