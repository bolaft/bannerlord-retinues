using System;
using System.Collections.Generic;
using Retinues.Core.Features.Upgrade.Behaviors;
using Retinues.Core.Features.Xp.Behaviors;
using Retinues.Core.Game;
using Retinues.Core.Game.Helpers;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.Core;

namespace Retinues.Core.Editor
{
    /// <summary>
    /// Static helpers for managing troop editing, upgrades, conversions, and retinue logic.
    /// Used by the troop editor UI and backend.
    /// </summary>
    [SafeClass]
    public static class TroopManager
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Collection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Collects retinue troops for a faction (elite and basic).
        /// </summary>
        public static List<WCharacter> CollectRetinueTroops(WFaction faction)
        {
            Log.Debug(
                $"Collecting retinue troops for faction {faction?.Name} (culture {faction?.Culture?.Name})."
            );
            try
            {
                return [faction.RetinueElite, faction.RetinueBasic];
            }
            catch
            {
                return [];
            }
        }

        /// <summary>
        /// Collects elite troops for a faction.
        /// </summary>
        public static List<WCharacter> CollectEliteTroops(WFaction faction)
        {
            Log.Debug(
                $"Collecting elite troops for faction {faction?.Name} (culture {faction?.Culture?.Name})."
            );
            try
            {
                return [.. faction.EliteTroops];
            }
            catch
            {
                return [];
            }
        }

        /// <summary>
        /// Collects basic troops for a faction.
        /// </summary>
        public static List<WCharacter> CollectBasicTroops(WFaction faction)
        {
            Log.Debug(
                $"Collecting basic troops for faction {faction?.Name} (culture {faction?.Culture?.Name})."
            );
            try
            {
                return [.. faction.BasicTroops];
            }
            catch
            {
                return [];
            }
        }

        /// <summary>
        /// Collects militia troops for a faction.
        /// </summary>
        public static List<WCharacter> CollectMilitiaTroops(WFaction faction)
        {
            Log.Debug(
                $"Collecting militia troops for faction {faction?.Name} (culture {faction?.Culture?.Name})."
            );
            try
            {
                return
                [
                    faction.MilitiaMelee,
                    faction.MilitiaMeleeElite,
                    faction.MilitiaRanged,
                    faction.MilitiaRangedElite,
                ];
            }
            catch
            {
                return [];
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       All Troops                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Renames a troop.
        /// </summary>
        public static void Rename(WCharacter troop, string newName)
        {
            Log.Debug($"Renaming troop {troop?.Name} to '{newName}'.");

            troop.Name = newName.Trim();
        }

        /// <summary>
        /// Changes the gender of a troop.
        /// </summary>
        public static void ChangeGender(WCharacter troop)
        {
            Log.Debug($"Changing gender for troop {troop?.Name}.");

            troop.IsFemale = !troop.IsFemale;
        }

        /// <summary>
        /// Modifies a troop's skill by delta, spending or refunding XP as needed.
        /// </summary>
        public static void ModifySkill(WCharacter troop, SkillObject skill, bool increment)
        {
            Log.Debug(
                $"Modifying skill {skill?.Name} for troop {troop?.Name} by {(increment ? 1 : -1)} (current: {troop?.GetSkill(skill)})."
            );
            if (troop == null || skill == null)
                return;

            // Already staged changes
            int staged = TroopTrainingBehavior.GetStaged(troop, skill);
            int stagedSkill = troop.GetSkill(skill) + staged;

            if (increment)
            {
                if (!TroopXpBehavior.TrySpend(troop, TroopRules.SkillPointXpCost(stagedSkill)))
                    return; // Not enough XP to increment

                // Stage timed training (or instant if TrainingTakesTime==false)
                TroopTrainingBehavior.StageTraining(troop, skill);
            }
            else
            {
                // Force refunds for staged skill points
                var force = staged > 0;
                // Refund one skill point, if allowed
                TroopXpBehavior.RefundOnePoint(troop, stagedSkill, force);
                TroopTrainingBehavior.ApplyChange(troop.StringId, skill, -1);
            }
        }

        /// <summary>
        /// Adds an upgrade target to a troop, creating and activating the child.
        /// </summary>
        public static WCharacter AddUpgradeTarget(WCharacter troop, string targetName)
        {
            Log.Debug($"Adding upgrade target '{targetName}' to troop {troop?.Name}.");

            // Determine the position in the tree
            List<int> path = [.. troop.PositionInTree, troop.UpgradeTargets.Length];

            // Wrap the custom troop
            var child = new WCharacter(troop.Faction == Player.Kingdom, troop.IsElite, path: path);

            // Copy from the original troop
            child.FillFrom(troop, keepUpgrades: false, keepEquipment: false, keepSkills: true);

            // Set name and level
            child.Name = targetName.Trim();
            child.Level = troop.Level + 5;

            // Add to upgrade targets of the parent
            troop.AddUpgradeTarget(child);

            // Activate
            child.Activate();

            return child;
        }

        /// <summary>
        /// Removes a troop and stocks its equipment.
        /// </summary>
        public static void Remove(WCharacter troop)
        {
            Log.Debug($"Removing troop {troop?.Name}.");

            // Stock the troop's equipment
            foreach (var item in troop.Equipment.Items)
                item.Stock();

            // Remove the troop
            troop.Remove();
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
        public static void Convert(
            WCharacter from,
            WCharacter to,
            int amountRequested,
            int cost = 0
        )
        {
            Log.Debug(
                $"Converting {amountRequested} of {from?.Name} to {to?.Name} at cost {cost}."
            );

            // Clamp to max possible
            int max = GetMaxConvertible(from, to);
            int amount = Math.Min(amountRequested, max);

            // Apply cost
            if (Player.Gold < cost)
                return;
            Player.ChangeGold(-cost);

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
            {
                Log.Error($"RetinueSources: {retinue} is not a retinue.");
                return sources;
            }

            // Identify which root to look under for culture and faction
            WCharacter cultureRoot = null,
                factionRoot = null;

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

            // Culture pick
            var culturePick = TroopMatcher.PickBestFromTree(cultureRoot, retinue);
            if (culturePick?.IsValid == true)
                sources.Add(culturePick);

            // Faction pick (avoid duplicate)
            var factionPick = TroopMatcher.PickBestFromTree(
                factionRoot,
                retinue,
                exclude: culturePick
            );
            if (factionPick?.IsValid == true)
                sources.Add(factionPick);

            return sources;
        }
    }
}
