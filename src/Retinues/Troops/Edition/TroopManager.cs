using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Features.Upgrade.Behaviors;
using Retinues.Features.Xp.Behaviors;
using Retinues.Game;
using Retinues.Game.Helpers;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Core;

namespace Retinues.Troops.Edition
{
    /// <summary>
    /// Static helpers for managing troop editing, upgrades, conversions, and retinue logic.
    /// Used by the troop editor UI and backend.
    /// </summary>
    [SafeClass]
    public static class TroopManager
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       All Troops                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Modifies a troop's skill by delta, spending or refunding XP as needed.
        /// </summary>
        public static void ModifySkill(WCharacter troop, SkillObject skill, bool increment)
        {
            if (troop == null || skill == null)
                return;

            // Already staged changes
            int staged = TroopTrainBehavior.Get(troop, skill)?.PointsRemaining ?? 0;
            int stagedSkill = troop.GetSkill(skill) + staged;

            if (increment)
            {
                if (!TroopXpBehavior.TrySpend(troop, TroopRules.SkillPointXpCost(stagedSkill)))
                    return; // Not enough XP to increment

                // Stage timed training (or instant if TrainingTakesTime==false)
                TroopTrainBehavior.Stage(troop, skill, 1);
            }
            else
            {
                // Force refunds for staged skill points
                var force = staged > 0;
                // Refund one skill point, if allowed
                TroopXpBehavior.RefundOnePoint(troop, stagedSkill, force);
                TroopTrainBehavior.ApplyChange(troop.StringId, skill, -1);
            }
        }

        /// <summary>
        /// Adds an upgrade target to a troop, creating and activating the child.
        /// </summary>
        public static WCharacter AddUpgradeTarget(WCharacter troop, string targetName)
        {
            Log.Debug($"Adding upgrade target '{targetName}' to troop {troop?.Name}.");

            // Wrap the custom troop
            var child = new WCharacter();

            bool keepEquipment = false; // default

            if (Config.EquipmentChangeTakesTime == false && Config.PayForEquipment == false)
                keepEquipment = true; // Copy equipment if no time or cost is involved

            // Copy from the original troop
            child.FillFrom(
                troop,
                keepUpgrades: false,
                keepEquipment: keepEquipment,
                keepSkills: true
            );

            // Set name and level
            child.Name = targetName.Trim();
            child.Level = troop.Level + 5;

            // Add to upgrade targets of the parent
            troop.AddUpgradeTarget(child);

            // Activate
            child.Activate();

            return child;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Retinues                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Ranks up a troop, spending gold and XP, and increasing level.
        /// </summary>
        public static void RankUp(WCharacter troop)
        {
            Log.Debug($"Ranking up troop {troop?.Name} (current level: {troop?.Level}).");

            if (troop == null || troop.IsMaxTier)
                return;

            int cost = TroopRules.RankUpCost(troop);
            if (Player.Gold < cost)
                return;

            // check XP first
            if (!TroopXpBehavior.TrySpend(troop, cost))
                return;

            // now charge gold and upgrade
            Player.ChangeGold(-cost);
            troop.Level += 5;
        }

        /// <summary>
        /// Gets the maximum number of troops that can be converted from one type to another.
        /// </summary>
        public static int GetMaxConvertible(WCharacter from, WCharacter to)
        {
            Log.Debug($"Getting max convertible from {from?.Name} to {to?.Name}.");

            // Number of 'from' troops available in party
            int maxConvertible = Player.Party.MemberRoster.CountOf(from);

            if (to.IsRetinue)
            {
                // Number of 'to' troops already in party
                int currentRetinue = Player.Party.MemberRoster.CountOf(to);

                // Cap left for retinue troops
                int cap = TroopRules.RetinueCapFor(to);

                // Apply cap
                maxConvertible = Math.Min(maxConvertible, Math.Max(0, cap - currentRetinue));
            }

            return maxConvertible;
        }

        /// <summary>
        /// Converts troops from one type to another, spending gold and mutating the roster.
        /// </summary>
        public static void Convert(WCharacter from, WCharacter to, int amountRequested)
        {
            Log.Info($"Converting {amountRequested} troops from {from?.Name} to {to?.Name}.");

            // Clamp to max possible
            int max = GetMaxConvertible(from, to);
            int amount = Math.Min(amountRequested, max);
            if (amount <= 0)
                return;

            // Calculate costs
            var goldCost = to.IsRetinue ? TroopRules.ConversionGoldCostPerUnit(to) * amount : 0;
            var influenceCost = to.IsRetinue
                ? TroopRules.ConversionInfluenceCostPerUnit(to) * amount
                : 0;

            // Check gold
            if (Player.Gold < goldCost)
                return;

            // Check influence
            if (Player.Influence < influenceCost)
                return;

            // Charge gold
            if (goldCost > 0)
                Player.ChangeGold(-goldCost);

            // Charge influence
            if (influenceCost > 0)
                Player.ChangeInfluence(-influenceCost);

            // Mutate roster
            Player.Party.MemberRoster.RemoveTroop(from, amount);
            Player.Party.MemberRoster.AddTroop(to, amount);
        }

        /// <summary>
        /// Gets the source troops for a retinue (culture and faction best matches).
        /// </summary>
        public static List<WCharacter> GetRetinueSourceTroops(WCharacter retinue)
        {
            var sources = new List<WCharacter>(2);

            if (retinue is null || !retinue.IsRetinue)
                return sources;

            // Identify which root to look under for culture and faction
            WCharacter cultureRoot;
            WCharacter factionRoot;

            if (retinue.IsElite)
            {
                cultureRoot = retinue.Culture?.RootElite;
                factionRoot = retinue.Faction?.RootElite;
            }
            else
            {
                cultureRoot = retinue.Culture?.RootBasic;
                factionRoot = retinue.Faction?.RootBasic;
            }

            // Precompute troop weapons and skills data
            var (weapons, skills) = TroopMatcher.GetTroopClassesSkills(retinue);

            // Culture pick
            var culturePick = TroopMatcher.PickBestFromTree(
                cultureRoot,
                retinue,
                troopWeapons: weapons,
                troopSkills: skills
            );
            if (culturePick?.IsValid == true)
                sources.Add(culturePick);

            // Faction pick (avoid duplicate)
            var factionPick = TroopMatcher.PickBestFromTree(
                factionRoot,
                retinue,
                exclude: culturePick,
                troopWeapons: weapons,
                troopSkills: skills
            );
            if (factionPick?.IsValid == true)
                sources.Add(factionPick);

            return sources;
        }
    }
}
