using System.Linq;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Features.Experience;
using Retinues.Features.Staging;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace OldRetinues.Managers
{
    /// <summary>
    /// Skill caps, totals, costs, availability and staging of skill changes.
    /// </summary>
    [SafeClass]
    public static class SkillManager
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Caps & Totals                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns the skill cap for a troop.
        /// </summary>
        public static int SkillCapByTier(WCharacter troop)
        {
            if (troop == null)
                return 0;

            if (troop.IsHero)
                return Config.SkillCapHeroes;

            int cap = troop.Tier switch
            {
                0 => Config.SkillCapTier0,
                1 => Config.SkillCapTier1,
                2 => Config.SkillCapTier2,
                3 => Config.SkillCapTier3,
                4 => Config.SkillCapTier4,
                5 => Config.SkillCapTier5,
                6 => Config.SkillCapTier6,
                _ => Config.SkillCapTier7Plus,
            };

            if (troop.IsRetinue)
                cap += Config.RetinueSkillCapBonus;
            if (DoctrineAPI.IsDoctrineUnlocked<IronDiscipline>())
                cap += 5;

            if (troop.IsMaxTier && troop.IsCaptain)
                cap += 25;

            return cap;
        }

        /// <summary>
        /// Returns the total skill points allowed for a troop.
        /// </summary>
        public static int SkillTotalByTier(WCharacter troop)
        {
            if (troop == null)
                return 0;

            int total = troop.Tier switch
            {
                0 => Config.SkillTotalTier0,
                1 => Config.SkillTotalTier1,
                2 => Config.SkillTotalTier2,
                3 => Config.SkillTotalTier3,
                4 => Config.SkillTotalTier4,
                5 => Config.SkillTotalTier5,
                6 => Config.SkillTotalTier6,
                _ => Config.SkillTotalTier7Plus,
            };

            if (troop.IsRetinue)
                total += Config.RetinueSkillTotalBonus;
            if (DoctrineAPI.IsDoctrineUnlocked<SteadfastSoldiers>())
                total += 10;

            if (troop.IsMaxTier && troop.IsCaptain)
                total += 50;

            return total;
        }

        /// <summary>
        /// Returns the number of skill points left for a troop.
        /// </summary>
        public static int SkillPointsLeft(WCharacter troop)
        {
            if (troop == null)
                return 0;
            return SkillTotalByTier(troop) - troop.Skills.Values.Sum();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        XP Costs                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// XP cost for the next skill point from a given value.
        /// </summary>
        public static int SkillPointXpCost(int fromValue)
        {
            if (ClanScreen.IsStudioMode)
                return 0;
            int baseCost = Config.BaseSkillXpCost;
            int perPoint = Config.SkillXpCostPerPoint;
            return baseCost + perPoint * fromValue;
        }

        /// <summary>
        /// True if the troop has enough XP for the next skill point.
        /// </summary>
        public static bool HasEnoughXpForNextPoint(WCharacter troop, SkillObject skill)
        {
            if (ClanScreen.IsStudioMode)
                return true;
            if (troop == null || skill == null)
                return false;

            var staged = TrainStagingBehavior.Get(troop, skill)?.PointsRemaining ?? 0;
            int cost = SkillPointXpCost(troop.GetSkill(skill) + staged);
            return TroopXpBehavior.Get(troop) >= cost;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Increment / Decrement                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns true if the troop can increment the given skill.
        /// </summary>
        public static bool CanIncrementSkill(WCharacter troop, SkillObject skill)
        {
            return GetIncrementSkillReason(troop, skill) == null;
        }

        /// <summary>
        /// Returns a reason the troop cannot increment the skill, or null if allowed.
        /// </summary>
        public static TextObject GetIncrementSkillReason(WCharacter troop, SkillObject skill)
        {
            if (troop == null || skill == null)
                return L.T("invalid_args", "Invalid arguments.");

            int trueValue = GetTrueSkillValue(troop, skill);
            int stagedAll = TrainStagingBehavior.Get(troop)?.Sum(d => d.PointsRemaining) ?? 0;

            if (trueValue >= SkillCapByTier(troop))
                return L.T("skill_at_cap", "Skill is at cap for this troop.");

            if (troop.IsHero)
                return null; // Heroes can always unless at hero cap above

            if (SkillPointsLeft(troop) - stagedAll <= 0)
                return L.T("no_skill_points_left", "No skill points left for this troop.");

            if (!HasEnoughXpForNextPoint(troop, skill))
                return L.T("not_enough_xp", "Not enough XP for next skill point.");

            if (Config.CannotRaiseSkillAboveUpgradeLevel)
            {
                foreach (var child in troop.UpgradeTargets)
                    if (trueValue >= GetTrueSkillValue(child, skill))
                        return L.T(
                                "cannot_exceed_child_skill",
                                "Cannot exceed skill level of upgrade {CHILD}."
                            )
                            .SetTextVariable("CHILD", child.Name);
            }

            return null;
        }

        /// <summary>
        /// Returns true if the troop can decrement the given skill.
        /// </summary>
        public static bool CanDecrementSkill(WCharacter troop, SkillObject skill)
        {
            return GetDecrementSkillReason(troop, skill) == null;
        }

        /// <summary>
        /// Returns a reason the troop cannot decrement the skill, or null if allowed.
        /// </summary>
        public static TextObject GetDecrementSkillReason(WCharacter troop, SkillObject skill)
        {
            if (troop == null || skill == null)
                return L.T("invalid_args", "Invalid arguments.");

            int trueValue = GetTrueSkillValue(troop, skill);
            if (trueValue <= 0)
                return L.T("skill_zero", "Skill cannot go below zero.");

            if (trueValue <= troop.Loadout.ComputeSkillRequirement(skill))
                return L.T("equipment_requirement", "Skill is required for equipped items.");

            if (Config.CannotRaiseSkillAboveUpgradeLevel)
            {
                if (troop.Parent != null && trueValue <= GetTrueSkillValue(troop.Parent, skill))
                    return L.T("parent_skill", "Cannot go below parent {PARENT}'s skill level.")
                        .SetTextVariable("PARENT", troop.Parent.Name);
            }

            return null;
        }

        /// <summary>
        /// Modify a troop's skill by one point, staging or applying immediately per settings.
        /// </summary>
        public static void ModifySkill(WCharacter troop, SkillObject skill, bool increment)
        {
            if (troop == null || skill == null)
                return;

            // Heroes: apply directly, no staging / XP system.
            if (troop.IsHero)
            {
                // Current value as seen by the UI / equipment rules.
                var current = troop.GetSkill(skill);
                var next = increment ? current + 1 : current - 1;
                if (next < 0)
                    next = 0;

                // This ultimately calls Hero.SetSkillValue(...) via WHero override.
                troop.SetSkill(skill, next);

                return;
            }

            // Troops: keep existing staged training behavior.
            int staged = TrainStagingBehavior.Get(troop, skill)?.PointsRemaining ?? 0;
            int stagedSkill = troop.GetSkill(skill) + staged;

            if (increment)
            {
                if (!TroopXpBehavior.TrySpend(troop, SkillPointXpCost(stagedSkill)))
                    return;
                TrainStagingBehavior.Stage(troop, skill, 1);
            }
            else
            {
                bool force = staged > 0;
                TroopXpBehavior.RefundOnePoint(troop, stagedSkill, force);
                TrainStagingBehavior.ApplyChange(troop.StringId, skill, -1);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Staged Training                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static int GetTrueSkillValue(WCharacter troop, SkillObject skill)
        {
            return troop.GetSkill(skill)
                + (TrainStagingBehavior.Get(troop, skill)?.PointsRemaining ?? 0);
        }
    }
}
