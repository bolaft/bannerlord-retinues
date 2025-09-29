using System;
using System.Linq;
using Retinues.Core.Features.Doctrines;
using Retinues.Core.Features.Doctrines.Catalog;
using Retinues.Core.Features.Xp;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.Core;

namespace Retinues.Core.Editor
{
    public static class TroopRules
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       All Troops                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static int SkillCapByTier(WCharacter troop)
        {
            try
            {
                int cap = troop.Tier switch
                {
                    0 => 20,
                    1 => 20,
                    2 => 50,
                    3 => 80,
                    4 => 120,
                    5 => 160,
                    6 => 260,
                    _ => 260, // Higher tiers for retinues
                };

                if (troop.IsMilitia && troop.IsElite)
                    cap += 20; // +20 cap for elite militia

                if (DoctrineAPI.IsDoctrineUnlocked<IronDiscipline>())
                    cap += 5; // +5 skill cap with Iron Discipline

                return cap;
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return 0;
            }
        }

        public static int SkillTotalByTier(WCharacter troop)
        {
            try
            {
                int total = troop.Tier switch
                {
                    0 => 90,
                    1 => 90,
                    2 => 210,
                    3 => 360,
                    4 => 535,
                    5 => 710,
                    6 => 915,
                    _ => 915, // Higher tiers for retinues
                };

                if (troop.IsMilitia)
                    total += 30; // +30 skill total for militia

                if (DoctrineAPI.IsDoctrineUnlocked<SteadfastSoldiers>())
                        total += 10; // +10 skill total with Steadfast Soldiers

                return total;
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return 0;
            }
        }

        public static int SkillPointsLeft(WCharacter character)
        {
            try
            {
                if (character == null)
                    return 0;

                return SkillTotalByTier(character.Tier) - character.Skills.Values.Sum();
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return 0;
            }
        }

        public static bool CanIncrementSkill(WCharacter character, SkillObject skill)
        {
            try
            {
                if (character == null || skill == null)
                    return false;

                if (character.GetSkill(skill) >= SkillCapByTier(character.Tier))
                    return false;

                if (SkillPointsLeft(character) <= 0)
                    return false;

                // Must be able to afford the next point from the troop XP bank
                if (!HasEnoughXpForNextPoint(character, skill))
                    return false;

                return true;
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return false;
            }
        }

        public static bool CanDecrementSkill(WCharacter character, SkillObject skill)
        {
            try
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
            catch (Exception e)
            {
                Log.Exception(e);
                return false;
            }
        }

        public static bool CanUpgradeTroop(WCharacter character)
        {
            try
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
            catch (Exception e)
            {
                Log.Exception(e);
                return false;
            }
        }

        public static int SkillPointXpCost(int fromValue)
        {
            try
            {
                int baseCost = Config.GetOption<int>("BaseSkillXpCost");
                int perPoint = Config.GetOption<int>("SkillXpCostPerPoint");

                return baseCost + perPoint * fromValue;
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return 0;
            }
        }

        public static bool HasEnoughXpForNextPoint(WCharacter c, SkillObject s)
        {
            try
            {
                if (c == null || s == null)
                    return false;
                int cost = SkillPointXpCost(c.GetSkill(s));
                return TroopXpService.GetPool(c) >= cost;
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return false;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Retinues                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static int MaxEliteRetinue
        {
            get
            {
                try
                {
                    var limit = Player.Party?.PartySizeLimit ?? 0;
                    int max = (int)(limit * Config.GetOption<float>("MaxEliteRetinueRatio"));
                    if (DoctrineAPI.IsDoctrineUnlocked<Vanguard>())
                        max = (int)(max * 1.15f);
                    return max;
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                    return 0;
                }
            }
        }

        public static int MaxBasicRetinue
        {
            get
            {
                try
                {
                    var limit = Player.Party?.PartySizeLimit ?? 0;
                    int max = (int)(limit * Config.GetOption<float>("MaxBasicRetinueRatio"));
                    if (DoctrineAPI.IsDoctrineUnlocked<Vanguard>())
                        max = (int)(max * 1.15f);
                    return max;
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                    return 0;
                }
            }
        }

        public static int ConversionCostPerUnit(WCharacter retinue)
        {
            try
            {
                int tier = retinue?.Tier ?? 1;
                int baseCost = Config.GetOption<int>("RetinueConversionCostPerTier");
                return tier * baseCost;
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return 0;
            }
        }

        public static int RankUpCost(WCharacter retinue)
        {
            try
            {
                int tier = retinue?.Tier ?? 1;
                int baseCost = Config.GetOption<int>("RetinueRankUpCostPerTier");
                return tier * baseCost;
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return 0;
            }
        }

        public static int RetinueCapFor(WCharacter retinue) =>
            retinue.IsElite ? MaxEliteRetinue : MaxBasicRetinue;
    }
}
