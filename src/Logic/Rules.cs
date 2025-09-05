using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using CustomClanTroops.Wrappers.Objects;

namespace CustomClanTroops.Logic
{
    public static class Rules
    {
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
                _ => throw new ArgumentOutOfRangeException(),
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
                _ => throw new ArgumentOutOfRangeException(),
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

            return true;
        }

        public static bool CanUpgradeTroop(WCharacter character)
        {
            if (character == null) return false;

            // Max tier reached
            if (character.IsMaxTier)
                return false;

            // Max upgrades reached
            if (character.UpgradeTargets.Count() >= 4)
                return false;

            return true;
        }
    }
}
