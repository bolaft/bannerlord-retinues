using System.Linq;
using Retinues.GUI.Helpers;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Features.Xp.Behaviors;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Core;

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
        public static bool IsAllowedInContext(
            WCharacter troop,
            WFaction faction,
            string action,
            bool showPopup = true
        )
        {
            if (faction == null)
                return true; // No faction, allow by default

            var settlement = Player.CurrentSettlement;

            if (troop.IsRetinue == true && faction == Player.Clan)
                if (settlement != null)
                    return true; // Clan retinues can be edited in any settlement
                else
                {
                    if (showPopup)
                        Popup.Display(
                            L.T("not_in_settlement", "Not in Settlement"),
                            L.T(
                                    "not_in_settlement_text",
                                    "You must be in a settlement to {ACTION} this troop."
                                )
                                .SetTextVariable("ACTION", action)
                        );
                    return false; // Clan retinues must be in settlement
                }

            if (faction.IsPlayerClan)
            {
                if (settlement?.Clan == Player.Clan)
                    return true; // In clan fief, allow

                if (showPopup)
                    Popup.Display(
                        L.T("not_in_clan_fief", "Not in Clan Fief"),
                        L.T(
                                "not_in_clan_fief_text",
                                "You must be in one of your clan's fiefs to {ACTION} this troop."
                            )
                            .SetTextVariable("ACTION", action)
                    );
                return false; // In settlement but not clan fief, disallow
            }

            if (faction.IsPlayerKingdom)
            {
                if (settlement?.Kingdom == Player.Kingdom)
                    return true; // In kingdom fief, allow

                if (showPopup)
                    Popup.Display(
                        L.T("not_in_kingdom_fief", "Not in Kingdom Fief"),
                        L.T(
                                "not_in_kingdom_fief_text",
                                "You must be in one of your kingdom's fiefs to {ACTION} this troop."
                            )
                            .SetTextVariable("ACTION", action)
                    );
                return false; // In settlement but not kingdom fief, disallow
            }

            return true; // Default allow if no faction
        }

        /// <summary>
        /// Returns the skill cap for a troop based on tier, type, and unlocked doctrines.
        /// </summary>
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

        /// <summary>
        /// Returns the total skill points allowed for a troop based on tier, type, and unlocked doctrines.
        /// </summary>
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

        /// <summary>
        /// Returns true if the troop can decrement the given skill.
        /// </summary>
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

        /// <summary>
        /// Returns true if the troop can be upgraded (not militia, not max tier, upgrade slots available).
        /// </summary>
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

        /// <summary>
        /// Returns the XP cost for the next skill point, based on current value.
        /// </summary>
        public static int SkillPointXpCost(int fromValue)
        {
            int baseCost = Config.GetOption<int>("BaseSkillXpCost");
            int perPoint = Config.GetOption<int>("SkillXpCostPerPoint");

            return baseCost + perPoint * fromValue;
        }

        /// <summary>
        /// Returns true if the troop has enough XP for the next skill point.
        /// </summary>
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

        /// <summary>
        /// Returns the max number of elite retinues allowed in the party.
        /// </summary>
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

        /// <summary>
        /// Returns the max number of basic retinues allowed in the party.
        /// </summary>
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

        /// <summary>
        /// Returns the gold cost per unit for converting a retinue, based on tier.
        /// </summary>
        public static int ConversionCostPerUnit(WCharacter retinue)
        {
            int tier = retinue?.Tier ?? 1;
            int baseCost = Config.GetOption<int>("RetinueConversionCostPerTier");
            return tier * baseCost;
        }

        /// <summary>
        /// Returns the gold cost for ranking up a retinue, based on tier.
        /// </summary>
        public static int RankUpCost(WCharacter retinue)
        {
            int tier = retinue?.Tier ?? 1;
            int baseCost = Config.GetOption<int>("RetinueRankUpCostPerTier");
            return tier * baseCost;
        }

        /// <summary>
        /// Returns the retinue cap for a troop (elite or basic).
        /// </summary>
        public static int RetinueCapFor(WCharacter retinue) =>
            retinue.IsElite ? MaxEliteRetinue : MaxBasicRetinue;
    }
}
