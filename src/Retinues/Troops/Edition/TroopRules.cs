using System;
using System.Linq;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Features.Upgrade.Behaviors;
using Retinues.Features.Xp.Behaviors;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor.VM;
using Retinues.GUI.Helpers;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.Troops.Edition
{
    /// <summary>
    /// Static helpers for troop skill caps, totals, upgrade rules, and retinue limits.
    /// Used by the troop editor and validation logic.
    /// </summary>
    [SafeClass]
    public static class TroopRules
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       All Troops                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns true if editing is allowed in the current context (fief ownership, location, etc).
        /// </summary>
        public static bool IsAllowedInContext(WCharacter troop, string action) =>
            EditorVM.IsStudioMode || GetContextReason(troop, action) == null;

        /// <summary>
        /// Returns the string reason why editing is not allowed, or null if allowed.
        /// </summary>
        public static TextObject GetContextReason(WCharacter troop, string action)
        {
            if (troop == null)
                return null;

            var faction = troop.Faction;

            if (faction == null)
                return null;

            if (Config.RestrictEditingToFiefs == false)
                return null;

            var settlement = Player.CurrentSettlement;

            if (troop.IsRetinue == true && faction == Player.Clan)
            {
                if (settlement != null)
                    return null;
                else
                    return L.T(
                            "not_in_settlement_text",
                            "You must be in a settlement to {ACTION} this troop."
                        )
                        .SetTextVariable("ACTION", action);
            }

            if (faction.IsPlayerClan)
            {
                if (settlement?.Clan == Player.Clan)
                    return null;
                return L.T(
                        "not_in_clan_fief_text",
                        "You must be in one of your clan's fiefs to {ACTION} this troop."
                    )
                    .SetTextVariable("ACTION", action);
            }

            if (faction.IsPlayerKingdom)
            {
                if (settlement?.Kingdom == Player.Kingdom)
                    return null;
                return L.T(
                        "not_in_kingdom_fief_text",
                        "You must be in one of your kingdom's fiefs to {ACTION} this troop."
                    )
                    .SetTextVariable("ACTION", action);
            }

            return null;
        }

        /// <summary>
        /// Displays a popup if editing is not allowed in the current context.
        /// Returns true if allowed, false otherwise.
        /// </summary>
        public static bool IsAllowedInContextWithPopup(WCharacter troop, string action)
        {
            if (EditorVM.IsStudioMode)
                return true; // Always allow in Studio Mode

            var reason = GetContextReason(troop, action);
            if (reason == null)
                return true;

            var faction = troop.Faction;

            TextObject title = L.T("not_allowed_title", "Not Allowed");
            if (troop.IsRetinue == true && faction == Player.Clan)
                title = L.T("not_in_settlement", "Not in Settlement");
            else if (faction.IsPlayerClan)
                title = L.T("not_in_clan_fief", "Not in Clan Fief");
            else if (faction.IsPlayerKingdom)
                title = L.T("not_in_kingdom_fief", "Not in Kingdom Fief");

            Notifications.Popup(title, reason);
            return false;
        }

        /// <summary>
        /// Returns the skill cap for a troop based on tier, type, and unlocked doctrines.
        /// </summary>
        public static int SkillCapByTier(WCharacter troop)
        {
            if (troop == null)
                return 0;

            int cap = troop.Tier switch
            {
                0 => Config.SkillCapTier0, // was 10
                1 => Config.SkillCapTier1, // was 20
                2 => Config.SkillCapTier2, // was 40
                3 => Config.SkillCapTier3, // was 80
                4 => Config.SkillCapTier4, // was 120
                5 => Config.SkillCapTier5, // was 180
                6 => Config.SkillCapTier6, // was 220
                _ => Config.SkillCapTier7Plus, // was 260
            };

            if (troop.IsRetinue)
                cap += Config.RetinueSkillCapBonus; // +5 cap for retinues

            if (DoctrineAPI.IsDoctrineUnlocked<IronDiscipline>())
                cap += 5; // +5 skill cap with Iron Discipline

            return cap;
        }

        /// <summary>
        /// Returns the total skill points allowed for a troop based on tier, type, and unlocked doctrines.
        /// </summary>
        public static int SkillTotalByTier(WCharacter troop)
        {
            if (troop == null)
                return 0;

            int total = troop.Tier switch
            {
                0 => Config.SkillTotalTier0, // was 90
                1 => Config.SkillTotalTier1, // was 120
                2 => Config.SkillTotalTier2, // was 180
                3 => Config.SkillTotalTier3, // was 360
                4 => Config.SkillTotalTier4, // was 510
                5 => Config.SkillTotalTier5, // was 750
                6 => Config.SkillTotalTier6, // was 900
                _ => Config.SkillTotalTier7Plus, // was 1500
            };

            if (troop.IsRetinue)
                total += Config.RetinueSkillTotalBonus; // +5 cap for retinues

            if (DoctrineAPI.IsDoctrineUnlocked<SteadfastSoldiers>())
                total += 10; // +10 skill total with Steadfast Soldiers

            return total;
        }

        /// <summary>
        /// Returns the number of skill points left for a troop.
        /// </summary>
        public static int SkillPointsLeft(WCharacter character)
        {
            if (character == null)
                return 0;

            return SkillTotalByTier(character) - character.Skills.Values.Sum();
        }

        /// <summary>
        /// Returns true if the troop can increment the given skill.
        /// </summary>
        public static bool CanIncrementSkill(WCharacter character, SkillObject skill) =>
            GetIncrementSkillReason(character, skill) == null;

        /// <summary>
        /// Returns a text reason why the troop cannot increment the given skill, or null if allowed.
        /// </summary>
        public static TextObject GetIncrementSkillReason(WCharacter character, SkillObject skill)
        {
            if (character == null || skill == null)
                return L.T("invalid_args", "Invalid arguments.");

            // Actual + staged for THIS skill
            var trueSkillValue = GetTrueSkillValue(character, skill);

            // staged across ALL skills for this troop
            var stagedAll =
                TroopTrainBehavior.GetAllStagedChanges(character)?.Sum(d => d.PointsRemaining) ?? 0;

            // per-skill cap
            if (trueSkillValue >= SkillCapByTier(character))
                return L.T("skill_at_cap", "Skill is at cap for this troop.");

            // total points cap (use all staged, not just this skill)
            if (SkillPointsLeft(character) - stagedAll <= 0)
                return L.T("no_skill_points_left", "No skill points left for this troop.");

            // xp affordability for the next point
            if (!HasEnoughXpForNextPoint(character, skill))
                return L.T("not_enough_xp", "Not enough XP for next skill point.");

            // can't go above children
            foreach (var child in character.UpgradeTargets)
            {
                if (trueSkillValue >= GetTrueSkillValue(child, skill))
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
        public static bool CanDecrementSkill(WCharacter character, SkillObject skill) =>
            GetDecrementSkillReason(character, skill) == null;

        /// <summary>
        /// Returns a text reason why the troop cannot decrement the given skill, or null if allowed.
        /// </summary>
        public static TextObject GetDecrementSkillReason(WCharacter character, SkillObject skill)
        {
            if (character == null || skill == null)
                return L.T("invalid_args", "Invalid arguments.");

            // Actual + staged for THIS skill
            var trueSkillValue = GetTrueSkillValue(character, skill);

            // Skills can't go below zero
            if (trueSkillValue <= 0)
                return L.T("skill_zero", "Skill cannot go below zero.");

            // Check for equipment skill requirements
            if (trueSkillValue <= character.Loadout.ComputeSkillRequirement(skill))
                return L.T("equipment_requirement", "Skill is required for equipped items.");

            if (character.Parent != null)
            {
                // Check for parent skill (can't go below parent's skill level)
                if (trueSkillValue <= GetTrueSkillValue(character.Parent, skill))
                    return L.T("parent_skill", "Cannot go below parent {PARENT}'s skill level.")
                        .SetTextVariable("PARENT", character.Parent.Name);
            }

            return null;
        }

        /// <summary>
        /// Returns the true skill value for a troop, including staged changes.
        /// </summary>
        private static int GetTrueSkillValue(WCharacter character, SkillObject skill)
        {
            return character.GetSkill(skill)
                + (TroopTrainBehavior.GetStagedChange(character, skill)?.PointsRemaining ?? 0);
        }

        /// <summary>
        /// Returns true if the troop can be upgraded (not militia, not max tier, upgrade slots available).
        /// </summary>
        public static bool CanAddUpgradeToTroop(WCharacter character) =>
            GetAddUpgradeToTroopReason(character) == null;

        /// <summary>
        /// Returns a text reason why the troop cannot be upgraded, or null if allowed.
        /// </summary>
        public static TextObject GetAddUpgradeToTroopReason(WCharacter character)
        {
            if (character == null)
                return L.T("invalid_args", "Invalid arguments.");

            if (character.IsMilitia)
                return L.T("militia_no_upgrade", "Militia cannot be upgraded.");

            if (character.IsRetinue)
                return L.T("retinue_no_upgrade", "Retinues cannot be upgraded.");

            // Max tier reached
            if (character.IsMaxTier)
                return L.T("max_tier", "Troop is at max tier.");

            // Determine max upgrades allowed
            int maxUpgrades = character.IsElite ? Config.MaxEliteUpgrades : Config.MaxBasicUpgrades;

            if (DoctrineAPI.IsDoctrineUnlocked<MastersAtArms>() && character.IsElite)
                maxUpgrades += 1; // +1 upgrade slot for elite troops with Masters at Arms

            // Cap upgrades at 4
            maxUpgrades = Math.Min(maxUpgrades, 4);

            // Max upgrades reached
            if (character.UpgradeTargets.Count() >= maxUpgrades)
                return L.T("max_upgrades_reached", "Troop has reached maximum amount of upgrades.");

            return null;
        }

        /// <summary>
        /// Returns the XP cost for the next skill point, based on current value.
        /// </summary>
        public static int SkillPointXpCost(int fromValue)
        {
            if (EditorVM.IsStudioMode)
                return 0; // No XP cost in Studio Mode

            int baseCost = Config.BaseSkillXpCost;
            int perPoint = Config.SkillXpCostPerPoint;

            return baseCost + perPoint * fromValue;
        }

        /// <summary>
        /// Returns true if the troop has enough XP for the next skill point.
        /// </summary>
        public static bool HasEnoughXpForNextPoint(WCharacter c, SkillObject s)
        {
            if (EditorVM.IsStudioMode)
                return true; // No XP checks in Studio Mode

            if (c == null || s == null)
                return false;

            var staged = TroopTrainBehavior.GetStagedChange(c, s)?.PointsRemaining ?? 0;
            int cost = SkillPointXpCost(c.GetSkill(s) + staged);
            return TroopXpBehavior.Get(c) >= cost;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Retinues                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns the max number of elite retinues allowed in the party.
        /// </summary>
        public static int MaxEliteRetinue
        {
            get
            {
                var limit = Player.Party?.PartySizeLimit ?? 0;
                int max = (int)(limit * Config.MaxEliteRetinueRatio);
                if (DoctrineAPI.IsDoctrineUnlocked<Vanguard>())
                    max = (int)(max * 1.15f);
                return max;
            }
        }

        /// <summary>
        /// Returns the max number of basic retinues allowed in the party.
        /// </summary>
        public static int MaxBasicRetinue
        {
            get
            {
                var limit = Player.Party?.PartySizeLimit ?? 0;
                int max = (int)(limit * Config.MaxBasicRetinueRatio);
                if (DoctrineAPI.IsDoctrineUnlocked<Vanguard>())
                    max = (int)(max * 1.15f);
                return max;
            }
        }

        /// <summary>
        /// Returns the gold cost per unit for converting a retinue, based on tier.
        /// </summary>
        public static int ConversionGoldCostPerUnit(WCharacter retinue)
        {
            if (retinue == null || !retinue.IsRetinue)
                return 0;
            int tier = retinue?.Tier ?? 1;
            int baseCost = Config.RetinueConversionCostPerTier;
            return tier * baseCost;
        }

        /// <summary>
        /// Returns the influence cost per unit for converting a retinue, based on tier.
        /// </summary>
        public static int ConversionInfluenceCostPerUnit(WCharacter retinue)
        {
            return ConversionGoldCostPerUnit(retinue) / 20;
        }

        /// <summary>
        /// Returns the renown cost per unit for converting a retinue, based on tier.
        /// </summary>
        public static int ConversionRenownCostPerUnit(WCharacter retinue)
        {
            return ConversionGoldCostPerUnit(retinue) / 5;
        }

        /// <summary>
        /// Returns the gold cost for ranking up a retinue, based on tier.
        /// </summary>
        public static int RankUpCost(WCharacter retinue)
        {
            int tier = retinue?.Tier ?? 1;
            int baseCost = Config.RetinueRankUpCostPerTier;
            return tier * baseCost;
        }

        /// <summary>
        /// Returns the retinue cap for a troop (elite or basic).
        /// </summary>
        public static int RetinueCapFor(WCharacter retinue) =>
            retinue.IsElite ? MaxEliteRetinue : MaxBasicRetinue;
    }
}
