using System.Linq;
using Retinues.Core.Features.Doctrines;
using Retinues.Core.Features.Doctrines.Catalog;
using Retinues.Core.Features.Xp.Behaviors;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.Core;

namespace Retinues.Core.Editor
{
    [SafeClass]
    public static class TroopRules
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       All Troops                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static int SkillCapByTier(WCharacter troop)
        {
            int cap = troop.Tier switch
            {
                0 => Config.GetOption<int>("SkillCapTier0"), // was 10
                1 => Config.GetOption<int>("SkillCapTier1"), // was 20
                2 => Config.GetOption<int>("SkillCapTier2"), // was 40
                3 => Config.GetOption<int>("SkillCapTier3"), // was 80
                4 => Config.GetOption<int>("SkillCapTier4"), // was 120
                5 => Config.GetOption<int>("SkillCapTier5"), // was 180
                6 => Config.GetOption<int>("SkillCapTier6"), // was 220
                _ => Config.GetOption<int>("SkillCapTier7Plus"), // was 260
            };

            if (troop.IsMilitia && troop.IsElite)
                cap += 20; // +20 cap for elite militia
            else if (troop.IsRetinue)
                cap += 5; // +5 cap for retinues

            if (DoctrineAPI.IsDoctrineUnlocked<IronDiscipline>())
                cap += 5; // +5 skill cap with Iron Discipline

            return cap;
        }

        public static int SkillTotalByTier(WCharacter troop)
        {
            int total = troop.Tier switch
            {
                0 => Config.GetOption<int>("SkillTotalTier0"), // was 90
                1 => Config.GetOption<int>("SkillTotalTier1"), // was 120
                2 => Config.GetOption<int>("SkillTotalTier2"), // was 180
                3 => Config.GetOption<int>("SkillTotalTier3"), // was 360
                4 => Config.GetOption<int>("SkillTotalTier4"), // was 510
                5 => Config.GetOption<int>("SkillTotalTier5"), // was 750
                6 => Config.GetOption<int>("SkillTotalTier6"), // was 900
                _ => Config.GetOption<int>("SkillTotalTier7Plus"), // was 1500
            };

            if (troop.IsMilitia)
                if (troop.IsElite)
                    total += 160; // +160 skill total for elite militia
                else
                    total += 30; // +30 skill total for militia

            if (DoctrineAPI.IsDoctrineUnlocked<SteadfastSoldiers>())
                total += 10; // +10 skill total with Steadfast Soldiers

            return total;
        }

        public static int SkillPointsLeft(WCharacter character)
        {
            if (character == null)
                return 0;

            return SkillTotalByTier(character) - character.Skills.Values.Sum();
        }

        public static bool CanIncrementSkill(WCharacter character, SkillObject skill)
        {
            if (character == null || skill == null)
                return false;

            if (character.GetSkill(skill) >= SkillCapByTier(character))
                return false;

            if (SkillPointsLeft(character) <= 0)
                return false;

            // Must be able to afford the next point from the troop XP bank
            if (!HasEnoughXpForNextPoint(character, skill))
                return false;

            return true;
        }

        public static bool CanDecrementSkill(WCharacter character, SkillObject skill)
        {
            if (character == null || skill == null)
                return false;

            // Skills can't go below zero
            if (character.GetSkill(skill) <= 0)
                return false;

            // Check for equipment skill requirements
            if (character.GetSkill(skill) <= character.Equipment.GetSkillRequirement(skill))
                return false;

            // Check for parent skill (can't go below parent's skill level)
            if (
                character.Parent != null
                && character.GetSkill(skill) <= character.Parent.GetSkill(skill)
            )
                return false;

            return true;
        }

        public static bool CanUpgradeTroop(WCharacter character)
        {
            if (character == null)
                return false;

            if (character.IsMilitia)
                return false; // Militia cannot be upgraded

            // Max tier reached
            if (character.IsMaxTier)
                return false;

            int maxUpgrades;

            if (character.IsRetinue)
                maxUpgrades = 1; // 1 upgrade for retinues
            else if (character.IsElite)
                if (DoctrineAPI.IsDoctrineUnlocked<MastersAtArms>())
                    maxUpgrades = 2; // 2 upgrades for elite troops with Masters at Arms
                else
                    maxUpgrades = 1; // 1 upgrade for elite troops without Masters at Arms
            else
                maxUpgrades = 2; // 2 upgrades for basic troops

            // Max upgrades reached
            if (character.UpgradeTargets.Count() >= maxUpgrades)
                return false;

            return true;
        }

        public static int SkillPointXpCost(int fromValue)
        {
            int baseCost = Config.GetOption<int>("BaseSkillXpCost");
            int perPoint = Config.GetOption<int>("SkillXpCostPerPoint");

            return baseCost + perPoint * fromValue;
        }

        public static bool HasEnoughXpForNextPoint(WCharacter c, SkillObject s)
        {
            if (c == null || s == null)
                return false;
            int cost = SkillPointXpCost(c.GetSkill(s));
            return TroopXpBehavior.Get(c) >= cost;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Retinues                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static int MaxEliteRetinue
        {
            get
            {
                var limit = Player.Party?.PartySizeLimit ?? 0;
                int max = (int)(limit * Config.GetOption<float>("MaxEliteRetinueRatio"));
                if (DoctrineAPI.IsDoctrineUnlocked<Vanguard>())
                    max = (int)(max * 1.15f);
                return max;
            }
        }

        public static int MaxBasicRetinue
        {
            get
            {
                var limit = Player.Party?.PartySizeLimit ?? 0;
                int max = (int)(limit * Config.GetOption<float>("MaxBasicRetinueRatio"));
                if (DoctrineAPI.IsDoctrineUnlocked<Vanguard>())
                    max = (int)(max * 1.15f);
                return max;
            }
        }

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

        public static int RetinueCapFor(WCharacter retinue) =>
            retinue.IsElite ? MaxEliteRetinue : MaxBasicRetinue;
    }
}
