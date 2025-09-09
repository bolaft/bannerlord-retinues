using System.Linq;
using TaleWorlds.Core;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Game;
using Retinues.Core.Utils;

namespace Retinues.Core.Editor
{
    public static class TroopRules
    {
        // ================================================================
        // Retinues
        // ================================================================

        public static int MaxEliteRetinue => (int)(Player.Party.PartySizeLimit * Config.GetOption<float>("MaxEliteRetinueRatio"));

        public static int MaxBasicRetinue => (int)(Player.Party.PartySizeLimit * Config.GetOption<float>("MaxBasicRetinueRatio"));

        public static int ConversionCostPerUnit(WCharacter retinue)
        {
            int tier = retinue?.Tier ?? 1;
            int baseCost = Config.GetOption<int>("RetinueConversionCostPerTier");
            return tier * baseCost;
        }

        public static int RankUpCost(WCharacter retinue)
        {
            int tier = retinue?.Tier ?? 1;
            int baseCost = Config.GetOption<int>("RetinueRankUpCostPerTier");
            return tier * baseCost;
        }

        public static int RetinueCapFor(WCharacter retinue) => retinue.IsElite ? MaxEliteRetinue : MaxBasicRetinue;

        // ================================================================
        // All Troops
        // ================================================================

        public static int SkillCapByTier(int tier)
        {
            return tier switch
            {
                1 => 20,
                2 => 50,
                3 => 80,
                4 => 120,
                5 => 160,
                6 => 260,
                _ => 260, // Higher tiers for retinues
            };
        }

        public static int SkillTotalByTier(int tier)
        {
            return tier switch
            {
                1 => 90,
                2 => 210,
                3 => 360,
                4 => 535,
                5 => 710,
                6 => 915,
                _ => 915, // Higher tiers for retinues
            };
        }

        public static int SkillPointsLeft(WCharacter character)
        {
            if (character == null) return 0;

            return SkillTotalByTier(character.Tier) - character.Skills.Values.Sum();
        }

        public static bool CanIncrementSkill(WCharacter character, SkillObject skill)
        {
            if (character == null || skill == null) return false;

            // Skills can't go above the tier skill cap
            if (character.GetSkill(skill) >= SkillCapByTier(character.Tier))
                return false;

            // Check if we have enough skill points left
            if (SkillPointsLeft(character) <= 0)
                return false;

            return true;
        }

        public static bool CanDecrementSkill(WCharacter character, SkillObject skill)
        {
            if (character == null || skill == null) return false;

            // Skills can't go below zero
            if (character.GetSkill(skill) <= 0)
                return false;

            // Check for equipment skill requirements
            if (character.GetSkill(skill) <= character.Equipment.GetSkillRequirement(skill))
                return false;

            // Check for parent skill (can't go below parent's skill level)
            if (character.Parent != null && character.GetSkill(skill) <= character.Parent.GetSkill(skill))
                return false;

            return true;
        }

        public static bool CanUpgradeTroop(WCharacter character)
        {
            if (character == null) return false;

            // Max tier reached
            if (character.IsMaxTier)
                return false;

            // Max upgrades reached
            if (character.IsElite || character.IsRetinue)
            {
                if (character.UpgradeTargets.Count() >= 1)
                    return false;  // Elite/Retinue troops can have 1 upgrade target
            }
            else
            {
                if (character.UpgradeTargets.Count() >= 2)
                    return false;  // Basic troops can have 2 upgrade targets
            }

            return true;
        }
    }
}
