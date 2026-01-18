using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Interface.Services;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace Retinues.Behaviors.Experience
{
    /// <summary>
    /// XP to skill point progress conversion utilities for custom-tree troops.
    /// </summary>
    internal static class SkillPointExperienceGain
    {
        private const int RequiredXpMultiplier = 20;
        private const int VanillaInvalidXpCost = 100000000;

        public static bool IsEnabled => Settings.SkillPointsMustBeEarned;

        /// <summary>
        /// Applies gained XP to skill point progress for the given wrapped character.
        /// </summary>
        public static void ApplyXpToSkillPointProgress(WCharacter wc, PartyBase party, int gainedXp)
        {
            if (wc == null || gainedXp <= 0)
                return;

            if (!IsEnabled)
                return;

            if (wc.IsHero)
                return;

            if (!wc.IsFactionTroop)
                return;

            if (wc.IsVanilla)
                return; // Extra safety.

            // Determine XP required per skill point.
            int xpRequired = GetXpRequiredForSkillPoint(wc.Base, party);
            if (xpRequired <= 0 || xpRequired >= VanillaInvalidXpCost)
                return;

            // Apply gained XP to skill point experience.
            wc.SkillPointsExperience += gainedXp;

            // Convert full XP chunks to skill points.
            if (wc.SkillPointsExperience >= xpRequired)
            {
                int chunks = wc.SkillPointsExperience / xpRequired;
                wc.SkillPoints += chunks;
                wc.SkillPointsExperience -= chunks * xpRequired;

                if (chunks > 1)
                    Notifications.Message(
                        L.T("skill_points_gained", "{TROOP} earned {POINTS} skill points.")
                            .SetTextVariable("TROOP", wc.Name)
                            .SetTextVariable("POINTS", chunks)
                    );
                else
                    Notifications.Message(
                        L.T("skill_point_gained", "{TROOP} earned a skill point.")
                            .SetTextVariable("TROOP", wc.Name)
                    );
            }
        }

        /// <summary>
        /// Gets the XP required to gain one skill point for the given troop.
        /// </summary>
        public static int GetXpRequiredForSkillPoint(CharacterObject troop, PartyBase party = null)
        {
            return (int)(
                GetXpRequiredToUpgradeThisUnit(troop, party)
                * RequiredXpMultiplier
                / Settings.SkillPointsGainRate
            );
        }

        /// <summary>
        /// Gets the XP required to upgrade this unit, using vanilla logic.
        /// </summary>
        public static int GetXpRequiredToUpgradeThisUnit(
            CharacterObject troop,
            PartyBase party = null
        )
        {
            party ??= PartyBase.MainParty;

            if (troop == null || troop.IsHero)
                return 0;

            party ??= PartyBase.MainParty;

            // If the troop has upgrade targets, use the same "max target XP cost" logic the game uses
            // for clamping and UI.
            var targets = troop.UpgradeTargets;
            if (targets != null && targets.Length > 0)
            {
                int max = 0;

                for (int i = 0; i < targets.Length; i++)
                {
                    var t = targets[i];
                    if (t == null)
                        continue;

                    int cost = Campaign.Current.Models.PartyTroopUpgradeModel.GetXpCostForUpgrade(
                        party,
                        troop,
                        t
                    );

                    if (cost >= VanillaInvalidXpCost)
                        continue;

                    if (cost > max)
                        max = cost;
                }

                if (max > 0)
                    return max;
            }

            // No upgrade targets: vanilla returns "invalid", but we still need a stable XP scale.
            // We mirror DefaultPartyTroopUpgradeModel's tier step costs for a single step.
            // (Tier 0 -> 1, 1 -> 2, ...).
            return EstimateNextTierXpCost(troop);
        }

        /// <summary>
        /// Estimates the XP cost to upgrade to the next tier for a troop without upgrade targets.
        /// </summary>
        private static int EstimateNextTierXpCost(CharacterObject troop)
        {
            int nextTier = troop.Tier + 1;

            if (nextTier <= 1)
                return 100;

            switch (nextTier)
            {
                case 2:
                    return 300;
                case 3:
                    return 550;
                case 4:
                    return 900;
                case 5:
                    return 1300;
                case 6:
                    return 1700;
                case 7:
                    return 2100;
                default:
                    // For modded tiers, approximate with the same quadratic shape as vanilla's "default" branch.
                    // Vanilla uses (upgradeTarget.Level + 4); we don't have a target here, so use troop.Level + 4.
                    int n = troop.Level + 4;
                    return (int)(1.333f * n * n);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Cheats                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [CommandLineFunctionality.CommandLineArgumentFunction("add_skill_points", "retinues")]
        public static string AddSkillPointsCommand(List<string> args)
        {
            if (args.Count < 2)
                return "Usage: add_skill_points <troop_id> <amount>";

            var troopId = args[0];
            var retinueName = string.Join(" ", args.GetRange(1, args.Count - 1));

            var troop = WCharacter.Get(troopId);
            if (troop == null)
                return $"Error: Troop with id '{troopId}' not found.";

            if (!int.TryParse(retinueName, out var amount))
                return $"Error: Invalid amount '{retinueName}'.";

            troop.SkillPoints += amount;

            return $"Added {amount} skill points to troop '{troop.Name}' ({troopId}).";
        }
    }
}
