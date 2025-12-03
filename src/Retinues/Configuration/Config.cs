using System.Collections.Generic;
using Retinues.Mods;
using Retinues.Utils;

namespace Retinues.Configuration
{
    /// <summary>
    /// High-level presets corresponding to the three built-in profiles.
    /// </summary>
    public enum ConfigPreset
    {
        Default,
        Freeform,
        Realistic,
    }

    /// <summary>
    /// Named configuration presets used for option overrides.
    /// </summary>
    public static class Presets
    {
        public const string Freeform = "freeform";
        public const string Realistic = "realistic";
    }

    /// <summary>
    /// Configuration options for the mod, grouped and exposed as typed Option<T> fields.
    /// </summary>
    public static partial class Config
    {
        /// <summary>
        /// Helper factory to create an Option<T> with metadata and optional per-preset overrides.
        /// </summary>
        public static Option<T> CreateOption<T>(
            System.Func<string> section,
            System.Func<string> name,
            string key,
            System.Func<string> hint,
            T @default,
            int minValue = 0,
            int maxValue = 1000,
            bool requiresRestart = false,
            IReadOnlyDictionary<string, object> presets = null,
            bool disabled = false,
            T disabledOverride = default
        ) =>
            new(
                section,
                name,
                key,
                hint,
                @default,
                minValue,
                maxValue,
                requiresRestart,
                presets,
                disabled,
                disabledOverride
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Options                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // ─────────────────────────────────────────────────────
        // Retinues
        // ─────────────────────────────────────────────────────

        public static readonly Option<float> MaxEliteRetinueRatio = CreateOption(
            section: () => L.S("mcm_section_retinues", "Retinues"),
            name: () => L.S("mcm_option_max_elite_retinue_ratio", "Max Elite Retinue Ratio"),
            key: "MaxEliteRetinueRatio",
            hint: () =>
                L.S(
                    "mcm_option_max_elite_retinue_ratio_hint",
                    "Maximum proportion of elite retinues in player party."
                ),
            @default: 0.10f,
            minValue: 0,
            maxValue: 1,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 1.00f,
                [Presets.Realistic] = 0.05f,
            }
        );

        public static readonly Option<float> MaxBasicRetinueRatio = CreateOption(
            section: () => L.S("mcm_section_retinues", "Retinues"),
            name: () => L.S("mcm_option_max_basic_retinue_ratio", "Max Basic Retinue Ratio"),
            key: "MaxBasicRetinueRatio",
            hint: () =>
                L.S(
                    "mcm_option_max_basic_retinue_ratio_hint",
                    "Maximum proportion of basic retinues in player party."
                ),
            @default: 0.20f,
            minValue: 0,
            maxValue: 1,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 1.00f,
                [Presets.Realistic] = 0.10f,
            }
        );

        public static readonly Option<int> RankUpCostPerTier = CreateOption(
            section: () => L.S("mcm_section_retinues", "Retinues"),
            name: () => L.S("mcm_option_rank_up_cost_per_tier", "Rank Up Cost (Per Tier)"),
            key: "RankUpCostPerTier",
            hint: () =>
                L.S(
                    "mcm_option_rank_up_cost_per_tier_hint",
                    "Rank up cost for retinue troops per tier."
                ),
            @default: 1000,
            minValue: 0,
            maxValue: 5000,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 0,
                [Presets.Realistic] = 2000,
            }
        );

        public static readonly Option<int> GoldConversionCostPerTier = CreateOption(
            section: () => L.S("mcm_section_retinues", "Retinues"),
            name: () =>
                L.S("mcm_option_gold_conversion_cost_per_tier", "Gold Conversion Cost (Per Tier)"),
            key: "GoldConversionCostPerTier",
            hint: () =>
                L.S(
                    "mcm_option_gold_conversion_cost_per_tier_hint",
                    "Gold cost to convert a troop into a retinue, per tier of the target retinue troop."
                ),
            @default: 50,
            minValue: 0,
            maxValue: 200,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 0,
                [Presets.Realistic] = 100,
            }
        );

        public static readonly Option<int> InfluenceConversionCostPerTier = CreateOption(
            section: () => L.S("mcm_section_retinues", "Retinues"),
            name: () =>
                L.S(
                    "mcm_option_influence_conversion_cost_per_tier",
                    "Influence Conversion Cost (Per Tier)"
                ),
            key: "InfluenceConversionCostPerTier",
            hint: () =>
                L.S(
                    "mcm_option_influence_conversion_cost_per_tier_hint",
                    "Influence cost to convert a troop into a retinue, per tier of the target retinue troop."
                ),
            @default: 5,
            minValue: 0,
            maxValue: 10,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 0,
                [Presets.Realistic] = 10,
            }
        );

        public static readonly Option<int> RenownRequiredPerTier = CreateOption(
            section: () => L.S("mcm_section_retinues", "Retinues"),
            name: () => L.S("mcm_option_renown_required_per_tier", "Renown Required (Per Tier)"),
            key: "RenownRequiredPerTier",
            hint: () =>
                L.S(
                    "mcm_option_renown_required_per_tier_hint",
                    "Renown required for a retinue to join automatically, per tier of the retinue troop."
                ),
            @default: 10,
            minValue: 1,
            maxValue: 100,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 10,
                [Presets.Realistic] = 20,
            }
        );

        // ─────────────────────────────────────────────────────
        // Troop Unlocks
        // ─────────────────────────────────────────────────────

        public static readonly Option<bool> NoFiefRequirements = CreateOption(
            section: () => L.S("mcm_section_troop_unlocks", "Troop Unlocks"),
            name: () => L.S("mcm_option_no_fief_requirements", "No Fief Requirements"),
            key: "NoFiefRequirement",
            hint: () =>
                L.S(
                    "mcm_option_no_fief_requirements_hint",
                    "Troops can be unlocked without having to own a fief."
                ),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = false,
            }
        );

        public static readonly Option<bool> NoDoctrineRequirements = CreateOption(
            section: () => L.S("mcm_section_troop_unlocks", "Troop Unlocks"),
            name: () => L.S("mcm_option_no_doctrine_requirements", "No Doctrine Requirements"),
            key: "NoDoctrineRequirements",
            hint: () =>
                L.S(
                    "mcm_option_no_doctrine_requirements_hint",
                    "Special troops (militias, villagers, caravan guards) can be acquired without the appropriate doctrines."
                ),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = false,
            }
        );

        public static readonly Option<bool> DisableKingdomTroops = CreateOption(
            section: () => L.S("mcm_section_troop_unlocks", "Troop Unlocks"),
            name: () => L.S("mcm_option_disable_kingdom_troops", "Disable Kingdom Troops"),
            key: "DisableKingdomTroops",
            hint: () =>
                L.S(
                    "mcm_option_disable_kingdom_troops_hint",
                    "The custom kingdom troop tree will be disabled and clan troops will be used instead."
                ),
            @default: false
        );

        public static readonly Option<bool> CopyAllSetsOnUnlock = CreateOption(
            section: () => L.S("mcm_section_troop_unlocks", "Troop Unlocks"),
            name: () => L.S("mcm_option_copy_all_sets_on_unlock", "Copy All Sets On Unlock"),
            key: "CopyAllSetsOnUnlock",
            hint: () =>
                L.S(
                    "mcm_option_copy_all_sets_on_unlock_hint",
                    "When unlocking a new troop, copy all equipment sets from the original troop instead of only the main battle and civilian sets."
                ),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = false,
            }
        );

        // ─────────────────────────────────────────────────────
        // Recruitment
        // ─────────────────────────────────────────────────────

        public static readonly Option<float> CustomVolunteersProportion = CreateOption(
            section: () => L.S("mcm_section_recruitment", "Recruitment"),
            name: () =>
                L.S("mcm_option_custom_volunteer_proportion", "Custom Volunteer Proportion"),
            key: "CustomVolunteerProportion",
            hint: () =>
                L.S(
                    "mcm_option_custom_volunteer_proportion_hint",
                    "Chance for each vanilla volunteer to be replaced by a custom troop (0 = never, 1 = always). Set a lower value if you want to keep some vanilla volunteers in your settlements."
                ),
            @default: 1f,
            minValue: 0,
            maxValue: 1
        );

        public static readonly Option<float> KingdomVolunteersInClanFiefsProportion = CreateOption(
            section: () => L.S("mcm_section_recruitment", "Recruitment"),
            name: () =>
                L.S(
                    "mcm_option_kingdom_volunteers_in_clan_fiefs_proportion",
                    "Kingdom Volunteers In Clan Fiefs Proportion"
                ),
            key: "KingdomVolunteersInClanFiefsProportion",
            hint: () =>
                L.S(
                    "mcm_option_kingdom_volunteers_in_clan_fiefs_proportion_hint",
                    "Chance for each volunteer in the player clan's fiefs to be a kingdom troop (0 = never, 1 = always). Set a higher value if you want to mix kingdom troops with clan troops."
                ),
            @default: 0.0f,
            minValue: 0,
            maxValue: 1
        );

        public static readonly Option<float> ClanVolunteersInKingdomFiefsProportion = CreateOption(
            section: () => L.S("mcm_section_recruitment", "Recruitment"),
            name: () =>
                L.S(
                    "mcm_option_clan_volunteers_in_kingdom_fiefs_proportion",
                    "Clan Volunteers In Kingdom Fiefs Proportion"
                ),
            key: "ClanVolunteersInKingdomFiefsProportion",
            hint: () =>
                L.S(
                    "mcm_option_clan_volunteers_in_kingdom_fiefs_proportion_hint",
                    "Chance for each volunteer in the player kingdom's fiefs to be a clan troop (0 = never, 1 = always). Set a higher value if you want to mix clan troops with kingdom troops."
                ),
            @default: 0.0f,
            minValue: 0,
            maxValue: 1
        );

        public static readonly Option<bool> RestrictToOwnedSettlements = CreateOption(
            section: () => L.S("mcm_section_recruitment", "Recruitment"),
            name: () =>
                L.S("mcm_option_restrict_to_owned_settlements", "Restrict To Owned Settlements"),
            key: "RestrictToOwnedSettlements",
            hint: () =>
                L.S(
                    "mcm_option_restrict_to_owned_settlements_hint",
                    "Custom troops can only be recruited in settlements owned by the player's clan or kingdom."
                ),
            @default: true,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = true,
            }
        );

        public static readonly Option<bool> RestrictToSameCultureSettlements = CreateOption(
            section: () => L.S("mcm_section_recruitment", "Recruitment"),
            name: () =>
                L.S(
                    "mcm_option_restrict_to_same_culture_settlements",
                    "Restrict To Same Culture Settlements"
                ),
            key: "RestrictToSameCultureSettlements",
            hint: () =>
                L.S(
                    "mcm_option_restrict_to_same_culture_settlements_hint",
                    "Volunteers in settlements of a different culture will not be replaced by custom troops."
                ),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = true,
            }
        );

        public static readonly Option<bool> VassalLordsCanRecruitCustomTroops = CreateOption(
            section: () => L.S("mcm_section_recruitment", "Recruitment"),
            name: () =>
                L.S(
                    "mcm_option_vassal_lords_recruit_custom_troops",
                    "Vassal Lords Recruit Custom Troops"
                ),
            key: "VassalLordsCanRecruitCustomTroops",
            hint: () =>
                L.S(
                    "mcm_option_vassal_lords_recruit_custom_troops_hint",
                    "Lords of the player's clan or kingdom can recruit custom troops in their fiefs."
                ),
            @default: true
        );

        public static readonly Option<bool> VassalLordsRecruitCustomTroopsAnywhere = CreateOption(
            section: () => L.S("mcm_section_recruitment", "Recruitment"),
            name: () =>
                L.S(
                    "mcm_option_vassal_lords_recruit_custom_troops_anywhere",
                    "Vassal Lords Recruit Custom Troops Anywhere"
                ),
            key: "VassalLordsRecruitCustomTroopsAnywhere",
            hint: () =>
                L.S(
                    "mcm_option_vassal_lords_recruit_custom_troops_anywhere_hint",
                    "Lords of the player's clan or kingdom can recruit custom troops in any settlement."
                ),
            @default: false
        );

        public static readonly Option<bool> AllLordsCanRecruitCustomTroops = CreateOption(
            section: () => L.S("mcm_section_recruitment", "Recruitment"),
            name: () =>
                L.S(
                    "mcm_option_all_lords_recruit_custom_troops",
                    "All Lords Recruit Custom Troops"
                ),
            key: "AllLordsCanRecruitCustomTroops",
            hint: () =>
                ModCompatibility.HasImprovedGarrisons
                    ? L.S(
                        "mcm_option_all_lords_recruit_custom_troops_hint_improved_garrisons",
                        "Any lord can recruit custom troops in the player's fiefs (required for Improved Garrisons compatibility)."
                    )
                    : L.S(
                        "mcm_option_all_lords_recruit_custom_troops_hint",
                        "Any lord can recruit custom troops in the player's fiefs."
                    ),
            @default: true
        );

        // ─────────────────────────────────────────────────────
        // Global Troop Editor
        // ─────────────────────────────────────────────────────

        public static readonly Option<bool> EnableGlobalEditor = CreateOption(
            section: () => L.S("mcm_section_global_editor", "Global Editor"),
            name: () => L.S("mcm_option_global_editor_enabled", "Enable Global Troop Editor"),
            key: "EnableGlobalEditor",
            hint: () =>
                L.S(
                    "mcm_option_global_editor_enabled_hint",
                    "Enables the global troop editor to modify any troop in the game. Disable if you encounter issues with non-player troops or other mods."
                ),
            @default: true,
            requiresRestart: true
        );

        public static readonly Option<bool> VanillaUpgradeRequirements = CreateOption(
            section: () => L.S("mcm_section_global_editor", "Global Editor"),
            name: () =>
                L.S("mcm_option_vanilla_upgrade_requirements", "Vanilla Upgrade Requirements"),
            key: "VanillaUpgradeRequirements",
            hint: () =>
                L.S(
                    "mcm_option_vanilla_upgrade_requirements_hint",
                    "Vanilla troops retain their original upgrade item requirements when edited."
                ),
            @default: true
        );

        // ─────────────────────────────────────────────────────
        // Restrictions
        // ─────────────────────────────────────────────────────

        public static readonly Option<bool> RestrictEditingToFiefs = CreateOption(
            section: () => L.S("mcm_section_restrictions", "Restrictions"),
            name: () => L.S("mcm_option_restrict_editing_to_fiefs", "Restrict Editing To Fiefs"),
            key: "RestrictEditingToFiefs",
            hint: () =>
                L.S(
                    "mcm_option_restrict_editing_to_fiefs_hint",
                    "Player can only edit troops when in a fief owned by their clan or kingdom (retinues can be edited in any settlement)."
                ),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = true,
            }
        );

        public static readonly Option<int> MaxEliteUpgrades = CreateOption(
            section: () => L.S("mcm_section_restrictions", "Restrictions"),
            name: () => L.S("mcm_option_max_elite_upgrades", "Max Elite Upgrades"),
            key: "MaxEliteUpgrades",
            hint: () =>
                L.S(
                    "mcm_option_max_elite_upgrades_hint",
                    "Maximum number of upgrade paths each elite troop can have."
                ),
            @default: 1,
            minValue: 1,
            maxValue: 4,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 4,
                [Presets.Realistic] = 1,
            }
        );

        public static readonly Option<int> MaxBasicUpgrades = CreateOption(
            section: () => L.S("mcm_section_restrictions", "Restrictions"),
            name: () => L.S("mcm_option_max_basic_upgrades", "Max Basic Upgrades"),
            key: "MaxBasicUpgrades",
            hint: () =>
                L.S(
                    "mcm_option_max_basic_upgrades_hint",
                    "Maximum number of upgrade paths each basic troop can have."
                ),
            @default: 2,
            minValue: 1,
            maxValue: 4,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 4,
                [Presets.Realistic] = 2,
            }
        );

        // ─────────────────────────────────────────────────────
        // Doctrines
        // ─────────────────────────────────────────────────────

        public static readonly Option<bool> EnableDoctrines = CreateOption(
            section: () => L.S("mcm_section_doctrines", "Doctrines"),
            name: () => L.S("mcm_option_enable_doctrines", "Enable Doctrines"),
            key: "EnableDoctrines",
            hint: () =>
                L.S(
                    "mcm_option_enable_doctrines_hint",
                    "Enables the Doctrines system and its bonuses. Warning: saving with doctrines disabled will clear all doctrine data in the save."
                ),
            @default: true,
            requiresRestart: true
        );

        public static readonly Option<bool> EnableFeatRequirements = CreateOption(
            section: () => L.S("mcm_section_doctrines", "Doctrines"),
            name: () => L.S("mcm_option_enable_feat_requirements", "Enable Feat Requirements"),
            key: "EnableFeatRequirements",
            hint: () =>
                L.S(
                    "mcm_option_enable_feat_requirements_hint",
                    "Enables feat requirements for unlocking doctrines. Warning: saving with feats disabled will clear all feat data in the save."
                ),
            @default: true,
            requiresRestart: true
        );

        public static readonly Option<float> DoctrineGoldCostMultiplier = CreateOption(
            section: () => L.S("mcm_section_doctrines", "Doctrines"),
            name: () =>
                L.S("mcm_option_doctrine_gold_cost_multiplier", "Doctrine Gold Cost Multiplier"),
            key: "DoctrineGoldCostMultiplier",
            hint: () =>
                L.S(
                    "mcm_option_doctrine_gold_cost_multiplier_hint",
                    "Multiplier for doctrine gold costs."
                ),
            @default: 1.0f,
            minValue: 0,
            maxValue: 5,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 0.0f,
                [Presets.Realistic] = 1.0f,
            },
            requiresRestart: true
        );

        public static readonly Option<float> DoctrineInfluenceCostMultiplier = CreateOption(
            section: () => L.S("mcm_section_doctrines", "Doctrines"),
            name: () =>
                L.S(
                    "mcm_option_doctrine_influence_cost_multiplier",
                    "Doctrine Influence Cost Multiplier"
                ),
            key: "DoctrineInfluenceCostMultiplier",
            hint: () =>
                L.S(
                    "mcm_option_doctrine_influence_cost_multiplier_hint",
                    "Multiplier for doctrine influence costs."
                ),
            @default: 1.0f,
            minValue: 0,
            maxValue: 5,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 0.0f,
                [Presets.Realistic] = 1.0f,
            },
            requiresRestart: true
        );

        // ─────────────────────────────────────────────────────
        // Equipment
        // ─────────────────────────────────────────────────────

        public static readonly Option<bool> EquippingTroopsCostsGold = CreateOption(
            section: () => L.S("mcm_section_equipment", "Equipment"),
            name: () =>
                L.S("mcm_option_equipping_troops_costs_gold", "Equipping Troops Costs Gold"),
            key: "EquippingTroopsCostsGold",
            hint: () =>
                L.S(
                    "mcm_option_equipping_troops_costs_gold_hint",
                    "Upgrading troop equipment costs money."
                ),
            @default: true,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = true,
            }
        );

        public static readonly Option<float> EquipmentCostMultiplier = CreateOption(
            section: () => L.S("mcm_section_equipment", "Equipment"),
            name: () => L.S("mcm_option_equipment_cost_multiplier", "Equipment Cost Multiplier"),
            key: "EquipmentCostMultiplier",
            hint: () =>
                L.S(
                    "mcm_option_equipment_cost_multiplier_hint",
                    "Multiplier for equipment prices compared to base game prices."
                ),
            @default: 2.0f,
            minValue: 0,
            maxValue: 5,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 0.0f,
                [Presets.Realistic] = 4.0f,
            }
        );

        public static readonly Option<float> EquipmentCostReductionPerPurchase = CreateOption(
            section: () => L.S("mcm_section_equipment", "Equipment"),
            name: () =>
                L.S(
                    "mcm_option_equipment_cost_reduction_per_purchase",
                    "Equipment Cost Reduction Per Purchase"
                ),
            key: "EquipmentCostReductionPerPurchase",
            hint: () =>
                L.S(
                    "mcm_option_equipment_cost_reduction_per_purchase_hint",
                    "Each time a troop purchases an item, the cost for future purchases of that item is reduced by this proportion (0 = no reduction, 1 = free after first purchase)."
                ),
            @default: 0.20f,
            minValue: 0,
            maxValue: 1
        );

        public static readonly Option<bool> EquippingTroopsTakesTime = CreateOption(
            section: () => L.S("mcm_section_equipment", "Equipment"),
            name: () =>
                L.S("mcm_option_equipping_troops_takes_time", "Equipping Troops Takes Time"),
            key: "EquippingTroopsTakesTime",
            hint: () =>
                L.S(
                    "mcm_option_equipping_troops_takes_time_hint",
                    "To apply item changes, troops must spend time upgrading equipment in a fief."
                ),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = true,
            }
        );

        public static readonly Option<int> EquipmentTimeMultiplier = CreateOption(
            section: () => L.S("mcm_section_equipment", "Equipment"),
            name: () => L.S("mcm_option_equipment_time_multiplier", "Equipment Time Multiplier"),
            key: "EquipmentTimeMultiplier",
            hint: () =>
                L.S(
                    "mcm_option_equipment_time_multiplier_hint",
                    "Multiplier for equipment change time."
                ),
            @default: 2,
            minValue: 1,
            maxValue: 5,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 2,
                [Presets.Realistic] = 4,
            }
        );

        public static readonly Option<bool> RestrictItemsToTownInventory = CreateOption(
            section: () => L.S("mcm_section_equipment", "Equipment"),
            name: () =>
                L.S(
                    "mcm_option_restrict_items_to_town_inventory",
                    "Restrict Items To Town Inventory"
                ),
            key: "RestrictItemsToTownInventory",
            hint: () =>
                L.S(
                    "mcm_option_restrict_items_to_town_inventory_hint",
                    "Troops equipment can only be purchased if available in the current town inventory."
                ),
            @default: false,
            presets: new Dictionary<string, object> { [Presets.Realistic] = true }
        );

        public static readonly Option<int> AllowedTierDifference = CreateOption(
            section: () => L.S("mcm_section_equipment", "Equipment"),
            name: () => L.S("mcm_option_allowed_tier_difference", "Allowed Tier Difference"),
            key: "AllowedTierDifference",
            hint: () =>
                L.S(
                    "mcm_option_allowed_tier_difference_hint",
                    "Maximum allowed difference between troop tier and item tier."
                ),
            @default: 3,
            minValue: 0,
            maxValue: 6,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 6,
                [Presets.Realistic] = 2,
            }
        );

        public static readonly Option<bool> DisallowMountsForT1Troops = CreateOption(
            section: () => L.S("mcm_section_equipment", "Equipment"),
            name: () =>
                L.S("mcm_option_disallow_mounts_for_t1_troops", "Disallow Mounts For T1 Troops"),
            key: "DisallowMountsForT1Troops",
            hint: () =>
                L.S(
                    "mcm_option_disallow_mounts_for_t1_troops_hint",
                    "Tier 1 troops cannot have mounts."
                ),
            @default: true,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = true,
            }
        );

        public static readonly Option<bool> ForceMainBattleSetInCombat = CreateOption(
            section: () => L.S("mcm_section_equipment", "Equipment"),
            name: () =>
                L.S(
                    "mcm_option_force_main_battle_set_in_combat",
                    "Force Main Battle Set In Combat"
                ),
            key: "ForceMainBattleSetInCombat",
            hint: () =>
                L.S(
                    "mcm_option_force_main_battle_set_in_combat_hint",
                    "Troops always use their main battle equipment set in combat, ignoring alternate sets. Use this setting if you experience issues with troops using incorrect equipment sets in battle."
                ),
            @default: false
        );

        public static readonly Option<bool> AllowFormationOverrides = CreateOption(
            section: () => L.S("mcm_section_equipment", "Equipment"),
            name: () => L.S("mcm_option_allow_formation_overrides", "Allow Formation Overrides"),
            key: "AllowFormationOverrides",
            hint: () =>
                L.S(
                    "mcm_option_allow_formation_overrides_hint",
                    "Allow manual overriding of troop formation class. If enabled, may cause awkward AI behavior and slow down the pre-battle formation screen."
                ),
            @default: false
        );

        public static readonly Option<bool> AdditionalFormationOverrides = CreateOption(
            section: () => L.S("mcm_section_equipment", "Equipment"),
            name: () =>
                L.S("mcm_option_additional_formation_overrides", "Additional Formation Overrides"),
            key: "AdditionalFormationOverrides",
            hint: () =>
                L.S(
                    "mcm_option_additional_formation_overrides_hint",
                    "Adds special formation classes (skirmisher, bodyguard, etc.) to the list of selectable formation overrides. Requires 'Allow Formation Overrides' to be enabled to have an effect."
                ),
            @default: false
        );

        public static readonly Option<bool> NoCivilianSetUpgradeRequirements = CreateOption(
            section: () => L.S("mcm_section_equipment", "Equipment"),
            name: () =>
                L.S(
                    "mcm_option_no_civilian_set_upgrade_requirements",
                    "No Civilian Set Upgrade Requirements"
                ),
            key: "NoCivilianSetUpgradeRequirements",
            hint: () =>
                L.S(
                    "mcm_option_no_civilian_set_upgrade_requirements_hint",
                    "When checking mount requirements for upgrades, ignore any horse in civilian sets."
                ),
            @default: true,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = false,
            }
        );

        public static readonly Option<bool> NoNobleHorseUpgradeRequirements = CreateOption(
            section: () => L.S("mcm_section_equipment", "Equipment"),
            name: () =>
                L.S(
                    "mcm_option_no_noble_horse_upgrade_requirements",
                    "No Noble Horse Upgrade Requirements"
                ),
            key: "NoNobleHorseUpgradeRequirements",
            hint: () =>
                L.S(
                    "mcm_option_no_noble_horse_upgrade_requirements_hint",
                    "Troops never require noble horses for upgrades, a war horse is always enough."
                ),
            @default: true,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = false,
            }
        );

        // ─────────────────────────────────────────────────────
        // Skills (training)
        // ─────────────────────────────────────────────────────

        public static readonly Option<bool> TrainingTroopsTakesTime = CreateOption(
            section: () => L.S("mcm_section_skills", "Skills"),
            name: () => L.S("mcm_option_training_troops_takes_time", "Training Troops Takes Time"),
            key: "TrainingTroopsTakesTime",
            hint: () =>
                L.S(
                    "mcm_option_training_troops_takes_time_hint",
                    "To apply skill increases, troops must spend time training in a fief."
                ),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = true,
            }
        );

        public static readonly Option<int> TrainingTimeMultiplier = CreateOption(
            section: () => L.S("mcm_section_skills", "Skills"),
            name: () => L.S("mcm_option_training_time_multiplier", "Training Time Multiplier"),
            key: "TrainingTimeMultiplier",
            hint: () =>
                L.S(
                    "mcm_option_training_time_multiplier_hint",
                    "Multiplier for troop training time."
                ),
            @default: 2,
            minValue: 1,
            maxValue: 5,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 2,
                [Presets.Realistic] = 3,
            }
        );

        public static readonly Option<int> BaseSkillXpCost = CreateOption(
            section: () => L.S("mcm_section_skills", "Skills"),
            name: () => L.S("mcm_option_skill_xp_cost_base", "Skill XP Cost (Base)"),
            key: "BaseSkillXpCost",
            hint: () =>
                L.S("mcm_option_skill_xp_cost_base_hint", "Base XP cost for increasing a skill."),
            @default: 100,
            minValue: 0,
            maxValue: 1000,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 0,
                [Presets.Realistic] = 200,
            }
        );

        public static readonly Option<int> SkillXpCostPerPoint = CreateOption(
            section: () => L.S("mcm_section_skills", "Skills"),
            name: () => L.S("mcm_option_skill_xp_cost_per_point", "Skill XP Cost (Per Point)"),
            key: "SkillXpCostPerPoint",
            hint: () =>
                L.S(
                    "mcm_option_skill_xp_cost_per_point_hint",
                    "Scalable XP cost for each additional skill point."
                ),
            @default: 1,
            minValue: 0,
            maxValue: 10,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 0,
                [Presets.Realistic] = 2,
            }
        );

        public static readonly Option<bool> SharedXpPool = CreateOption(
            section: () => L.S("mcm_section_skills", "Skills"),
            name: () => L.S("mcm_option_shared_xp_pool", "Shared XP Pool"),
            key: "SharedXpPool",
            hint: () =>
                L.S(
                    "mcm_option_shared_xp_pool_hint",
                    "All edited troops share a single XP pool instead of having individual XP."
                ),
            @default: false
        );

        public static readonly Option<bool> ForceXpRefunds = CreateOption(
            section: () => L.S("mcm_section_skills", "Skills"),
            name: () => L.S("mcm_option_force_xp_refunds", "Force XP Refunds"),
            key: "ForceXpRefunds",
            hint: () =>
                L.S(
                    "mcm_option_force_xp_refunds_hint",
                    "When lowering a troop's skill, always refund the XP previously spent on those points."
                ),
            @default: false
        );

        // ─────────────────────────────────────────────────────
        // Equipment Unlocks
        // ─────────────────────────────────────────────────────

        public static readonly Option<bool> AllEquipmentUnlocked = CreateOption(
            section: () => L.S("mcm_section_equipment_unlocks", "Equipment Unlocks"),
            name: () => L.S("mcm_option_all_equipment_unlocked", "All Equipment Unlocked"),
            key: "AllEquipmentUnlocked",
            hint: () =>
                L.S("mcm_option_all_equipment_unlocked_hint", "All items are always available."),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = false,
            }
        );

        public static readonly Option<bool> AllCultureEquipmentUnlocked = CreateOption(
            section: () => L.S("mcm_section_equipment_unlocks", "Equipment Unlocks"),
            name: () =>
                L.S("mcm_option_all_culture_equipment_unlocked", "All Culture Equipment Unlocked"),
            key: "AllCultureEquipmentUnlocked",
            hint: () =>
                L.S(
                    "mcm_option_all_culture_equipment_unlocked_hint",
                    "Player clan and kingdom culture items are always available."
                ),
            @default: false
        );

        public static readonly Option<bool> UnlockItemsFromKills = CreateOption(
            section: () => L.S("mcm_section_equipment_unlocks", "Equipment Unlocks"),
            name: () => L.S("mcm_option_unlock_items_from_kills", "Unlock Items From Kills"),
            key: "UnlockItemsFromKills",
            hint: () =>
                L.S(
                    "mcm_option_unlock_items_from_kills_hint",
                    "Unlock equipment by defeating enemies wearing it."
                ),
            @default: true,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = true,
            }
        );

        public static readonly Option<int> RequiredKillsPerItem = CreateOption(
            section: () => L.S("mcm_section_equipment_unlocks", "Equipment Unlocks"),
            name: () => L.S("mcm_option_required_kills_per_item", "Required Kills Per Item"),
            key: "RequiredKillsPerItem",
            hint: () =>
                L.S(
                    "mcm_option_required_kills_per_item_hint",
                    "How many enemies wearing an item must be defeated to unlock it."
                ),
            @default: 100,
            minValue: 1,
            maxValue: 1000,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 100,
                [Presets.Realistic] = 200,
            }
        );

        public static readonly Option<bool> UnlockItemsFromDiscards = CreateOption(
            section: () => L.S("mcm_section_equipment_unlocks", "Equipment Unlocks"),
            name: () => L.S("mcm_option_unlock_items_from_discards", "Unlock Items From Discards"),
            key: "UnlockItemsFromDiscards",
            hint: () =>
                L.S(
                    "mcm_option_unlock_items_from_discards_hint",
                    "Unlock equipment by discarding it."
                ),
            @default: false
        );

        public static readonly Option<int> RequiredDiscardsPerItem = CreateOption(
            section: () => L.S("mcm_section_equipment_unlocks", "Equipment Unlocks"),
            name: () => L.S("mcm_option_required_discards_per_item", "Required Discards Per Item"),
            key: "DiscardsForUnlock",
            hint: () =>
                L.S(
                    "mcm_option_required_discards_per_item_hint",
                    "How many times an item must be discarded to unlock it."
                ),
            @default: 10,
            minValue: 1,
            maxValue: 100,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 10,
                [Presets.Realistic] = 20,
            }
        );

        public static readonly Option<bool> PlayerCultureUnlockBonus = CreateOption(
            section: () => L.S("mcm_section_equipment_unlocks", "Equipment Unlocks"),
            name: () =>
                L.S("mcm_option_player_culture_unlock_bonus", "Player Culture Unlock Bonus"),
            key: "PlayerCultureUnlockBonus",
            hint: () =>
                L.S(
                    "mcm_option_player_culture_unlock_bonus_hint",
                    "Whether item unlock progression also adds progress to random items of the player culture."
                ),
            @default: true,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = false,
            }
        );

        public static readonly Option<bool> UnlockPopup = CreateOption(
            section: () => L.S("mcm_section_equipment_unlocks", "Equipment Unlocks"),
            name: () => L.S("mcm_option_unlock_popup", "Unlock Popup"),
            key: "UnlockPopup",
            hint: () =>
                L.S(
                    "mcm_option_unlock_popup_hint",
                    "Displays a popup notification when items are unlocked. If disabled, unlocks are only shown in the log."
                ),
            @default: true
        );

        // ─────────────────────────────────────────────────────
        // Debug
        // ─────────────────────────────────────────────────────

        public static readonly Option<bool> DebugMode = CreateOption(
            section: () => L.S("mcm_section_debug", "Debug"),
            name: () => L.S("mcm_option_debug_mode", "Debug Mode"),
            key: "DebugMode",
            hint: () => L.S("mcm_option_debug_mode_hint", "Displays debug logs in game."),
            @default: false
        );

        // ─────────────────────────────────────────────────────
        // User Interface
        // ─────────────────────────────────────────────────────

        public static readonly Option<bool> EnableEditorHotkey = CreateOption(
            section: () => L.S("mcm_section_ui", "User Interface"),
            name: () => L.S("mcm_option_enable_editor_hotkey", "Enable Editor Hotkey (Shift + R)"),
            key: "EnableEditorHotkey",
            hint: () =>
                L.S(
                    "mcm_option_enable_editor_hotkey_hint",
                    "Enables the hotkey (Shift + R) to open the editor from the campaign map."
                ),
            @default: false
        );

        public static readonly Option<bool> EnableItemComparisonIcons = CreateOption(
            section: () => L.S("mcm_section_ui", "User Interface"),
            name: () =>
                L.S("mcm_option_enable_item_comparison_icons", "Enable Item Comparison Icons"),
            key: "EnableItemComparisonIcons",
            hint: () =>
                L.S(
                    "mcm_option_enable_item_comparison_icons_hint",
                    "Adds comparison icons to the equipment list to show if an item is better or worse than the equipped one when browsing troop equipment."
                ),
            @default: true
        );

        public static readonly Option<bool> EnableTroopCustomization = CreateOption(
            section: () => L.S("mcm_section_ui", "User Interface"),
            name: () => L.S("mcm_option_enable_troop_customization", "Enable Appearance Controls"),
            key: "EnableTroopCustomization",
            hint: () =>
                L.S(
                    "mcm_option_enable_troop_customization_hint",
                    "Adds appearance customization controls (age, height, weight, build) to the editor. Cosmetic only; no gameplay effect."
                ),
            @default: true
        );

        public static readonly Option<int> MaxEquipmentRowsPerPage = CreateOption(
            section: () => L.S("mcm_section_ui", "User Interface"),
            name: () =>
                L.S("mcm_option_max_equipment_rows_per_page", "Max Equipment Rows Per Page"),
            key: "MaxEquipmentRowsPerPage",
            hint: () =>
                L.S(
                    "mcm_option_max_equipment_rows_per_page_hint",
                    "Maximum number of equipment rows to show per page in the troop editor. Smaller values improve UI reactivity."
                ),
            @default: 100,
            minValue: 10,
            maxValue: 1000
        );

        // ─────────────────────────────────────────────────────
        // Skill Caps
        // ─────────────────────────────────────────────────────

        public static readonly Option<int> RetinueSkillCapBonus = CreateOption(
            section: () => L.S("mcm_section_skill_caps", "Skill Caps"),
            key: "RetinueSkillCapBonus",
            name: () => L.S("mcm_option_retinue_skill_cap_bonus", "Retinue Skill Cap Bonus"),
            hint: () =>
                L.S(
                    "mcm_option_retinue_skill_cap_bonus_hint",
                    "Additional skill cap for retinue troops."
                ),
            @default: 5,
            minValue: 0,
            maxValue: 50,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 50,
                [Presets.Realistic] = 0,
            }
        );

        public static readonly Option<int> SkillCapTier0 = CreateOption(
            section: () => L.S("mcm_section_skill_caps", "Skill Caps"),
            key: "SkillCapTier0",
            name: () =>
                L.T("mcm_option_skill_cap", "Tier {TIER} Cap")
                    .SetTextVariable("TIER", "0")
                    .ToString(),
            hint: () =>
                L.T("mcm_option_skill_cap_hint", "The maximum skill level for tier {TIER} troops.")
                    .SetTextVariable("TIER", "0")
                    .ToString(),
            @default: 20,
            minValue: 20,
            maxValue: 360,
            presets: new Dictionary<string, object> { [Presets.Freeform] = 360 }
        );

        public static readonly Option<int> SkillCapTier1 = CreateOption(
            section: () => L.S("mcm_section_skill_caps", "Skill Caps"),
            key: "SkillCapTier1",
            name: () =>
                L.T("mcm_option_skill_cap", "Tier {TIER} Cap")
                    .SetTextVariable("TIER", "1")
                    .ToString(),
            hint: () =>
                L.T("mcm_option_skill_cap_hint", "The maximum skill level for tier {TIER} troops.")
                    .SetTextVariable("TIER", "1")
                    .ToString(),
            @default: 20,
            minValue: 20,
            maxValue: 360,
            presets: new Dictionary<string, object> { [Presets.Freeform] = 360 }
        );

        public static readonly Option<int> SkillCapTier2 = CreateOption(
            section: () => L.S("mcm_section_skill_caps", "Skill Caps"),
            key: "SkillCapTier2",
            name: () =>
                L.T("mcm_option_skill_cap", "Tier {TIER} Cap")
                    .SetTextVariable("TIER", "2")
                    .ToString(),
            hint: () =>
                L.T("mcm_option_skill_cap_hint", "The maximum skill level for tier {TIER} troops.")
                    .SetTextVariable("TIER", "2")
                    .ToString(),
            @default: 50,
            minValue: 20,
            maxValue: 360,
            presets: new Dictionary<string, object> { [Presets.Freeform] = 360 }
        );

        public static readonly Option<int> SkillCapTier3 = CreateOption(
            section: () => L.S("mcm_section_skill_caps", "Skill Caps"),
            key: "SkillCapTier3",
            name: () =>
                L.T("mcm_option_skill_cap", "Tier {TIER} Cap")
                    .SetTextVariable("TIER", "3")
                    .ToString(),
            hint: () =>
                L.T("mcm_option_skill_cap_hint", "The maximum skill level for tier {TIER} troops.")
                    .SetTextVariable("TIER", "3")
                    .ToString(),
            @default: 80,
            minValue: 20,
            maxValue: 360,
            presets: new Dictionary<string, object> { [Presets.Freeform] = 360 }
        );

        public static readonly Option<int> SkillCapTier4 = CreateOption(
            section: () => L.S("mcm_section_skill_caps", "Skill Caps"),
            key: "SkillCapTier4",
            name: () =>
                L.T("mcm_option_skill_cap", "Tier {TIER} Cap")
                    .SetTextVariable("TIER", "4")
                    .ToString(),
            hint: () =>
                L.T("mcm_option_skill_cap_hint", "The maximum skill level for tier {TIER} troops.")
                    .SetTextVariable("TIER", "4")
                    .ToString(),
            @default: 120,
            minValue: 20,
            maxValue: 360,
            presets: new Dictionary<string, object> { [Presets.Freeform] = 360 }
        );

        public static readonly Option<int> SkillCapTier5 = CreateOption(
            section: () => L.S("mcm_section_skill_caps", "Skill Caps"),
            key: "SkillCapTier5",
            name: () =>
                L.T("mcm_option_skill_cap", "Tier {TIER} Cap")
                    .SetTextVariable("TIER", "5")
                    .ToString(),
            hint: () =>
                L.T("mcm_option_skill_cap_hint", "The maximum skill level for tier {TIER} troops.")
                    .SetTextVariable("TIER", "5")
                    .ToString(),
            @default: 160,
            minValue: 20,
            maxValue: 360,
            presets: new Dictionary<string, object> { [Presets.Freeform] = 360 }
        );

        public static readonly Option<int> SkillCapTier6 = CreateOption(
            section: () => L.S("mcm_section_skill_caps", "Skill Caps"),
            key: "SkillCapTier6",
            name: () =>
                L.T("mcm_option_skill_cap", "Tier {TIER} Cap")
                    .SetTextVariable("TIER", "6")
                    .ToString(),
            hint: () =>
                L.T("mcm_option_skill_cap_hint", "The maximum skill level for tier {TIER} troops.")
                    .SetTextVariable("TIER", "6")
                    .ToString(),
            @default: 260,
            minValue: 20,
            maxValue: 360,
            presets: new Dictionary<string, object> { [Presets.Freeform] = 360 }
        );

        public static readonly Option<int> SkillCapTier7Plus = CreateOption(
            section: () => L.S("mcm_section_skill_caps", "Skill Caps"),
            key: "SkillCapTier7Plus",
            name: () =>
                L.T("mcm_option_skill_cap", "Tier {TIER} Cap")
                    .SetTextVariable("TIER", "7+")
                    .ToString(),
            hint: () =>
                L.T("mcm_option_skill_cap_hint", "The maximum skill level for tier {TIER} troops.")
                    .SetTextVariable("TIER", "7+")
                    .ToString(),
            @default: 360,
            minValue: 20,
            maxValue: 360,
            presets: new Dictionary<string, object> { [Presets.Freeform] = 360 }
        );

        public static readonly Option<int> SkillCapHeroes = CreateOption(
            section: () => L.S("mcm_section_skill_caps", "Skill Caps"),
            key: "SkillCapHeroes",
            name: () => L.S("mcm_option_skill_cap_heroes", "Hero Skill Cap"),
            hint: () =>
                L.S("mcm_option_skill_cap_heroes_hint", "The maximum skill level for hero troops."),
            @default: 420,
            minValue: 20,
            maxValue: 420,
            presets: new Dictionary<string, object> { [Presets.Freeform] = 420 }
        );

        // ─────────────────────────────────────────────────────
        // Skill Totals
        // ─────────────────────────────────────────────────────

        public static readonly Option<int> RetinueSkillTotalBonus = CreateOption(
            section: () => L.S("mcm_section_skill_totals", "Skill Totals"),
            key: "RetinueSkillTotalBonus",
            name: () => L.S("mcm_option_retinue_skill_total_bonus", "Retinue Skill Total Bonus"),
            hint: () =>
                L.S(
                    "mcm_option_retinue_skill_total_bonus_hint",
                    "Additional skill total for retinue troops."
                ),
            @default: 10,
            minValue: 0,
            maxValue: 100,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 100,
                [Presets.Realistic] = 0,
            }
        );

        public static readonly Option<int> SkillTotalTier0 = CreateOption(
            section: () => L.S("mcm_section_skill_totals", "Skill Totals"),
            key: "SkillTotalTier0",
            name: () =>
                L.T("mcm_option_skill_total", "Tier {TIER} Skill Total")
                    .SetTextVariable("TIER", "0")
                    .ToString(),
            hint: () =>
                L.T(
                        "mcm_option_skill_total_hint",
                        "The total available skill points for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "0")
                    .ToString(),
            @default: 90,
            minValue: 90,
            maxValue: 1600,
            presets: new Dictionary<string, object> { [Presets.Freeform] = 1600 }
        );

        public static readonly Option<int> SkillTotalTier1 = CreateOption(
            section: () => L.S("mcm_section_skill_totals", "Skill Totals"),
            key: "SkillTotalTier1",
            name: () =>
                L.T("mcm_option_skill_total", "Tier {TIER} Skill Total")
                    .SetTextVariable("TIER", "1")
                    .ToString(),
            hint: () =>
                L.T(
                        "mcm_option_skill_total_hint",
                        "The total available skill points for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "1")
                    .ToString(),
            @default: 90,
            minValue: 90,
            maxValue: 1600,
            presets: new Dictionary<string, object> { [Presets.Freeform] = 1600 }
        );

        public static readonly Option<int> SkillTotalTier2 = CreateOption(
            section: () => L.S("mcm_section_skill_totals", "Skill Totals"),
            key: "SkillTotalTier2",
            name: () =>
                L.T("mcm_option_skill_total", "Tier {TIER} Skill Total")
                    .SetTextVariable("TIER", "2")
                    .ToString(),
            hint: () =>
                L.T(
                        "mcm_option_skill_total_hint",
                        "The total available skill points for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "2")
                    .ToString(),
            @default: 210,
            minValue: 90,
            maxValue: 1600,
            presets: new Dictionary<string, object> { [Presets.Freeform] = 1600 }
        );

        public static readonly Option<int> SkillTotalTier3 = CreateOption(
            section: () => L.S("mcm_section_skill_totals", "Skill Totals"),
            key: "SkillTotalTier3",
            name: () =>
                L.T("mcm_option_skill_total", "Tier {TIER} Skill Total")
                    .SetTextVariable("TIER", "3")
                    .ToString(),
            hint: () =>
                L.T(
                        "mcm_option_skill_total_hint",
                        "The total available skill points for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "3")
                    .ToString(),
            @default: 360,
            minValue: 90,
            maxValue: 1600,
            presets: new Dictionary<string, object> { [Presets.Freeform] = 1600 }
        );

        public static readonly Option<int> SkillTotalTier4 = CreateOption(
            section: () => L.S("mcm_section_skill_totals", "Skill Totals"),
            key: "SkillTotalTier4",
            name: () =>
                L.T("mcm_option_skill_total", "Tier {TIER} Skill Total")
                    .SetTextVariable("TIER", "4")
                    .ToString(),
            hint: () =>
                L.T(
                        "mcm_option_skill_total_hint",
                        "The total available skill points for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "4")
                    .ToString(),
            @default: 555,
            minValue: 90,
            maxValue: 1600,
            presets: new Dictionary<string, object> { [Presets.Freeform] = 1600 }
        );

        public static readonly Option<int> SkillTotalTier5 = CreateOption(
            section: () => L.S("mcm_section_skill_totals", "Skill Totals"),
            key: "SkillTotalTier5",
            name: () =>
                L.T("mcm_option_skill_total", "Tier {TIER} Skill Total")
                    .SetTextVariable("TIER", "5")
                    .ToString(),
            hint: () =>
                L.T(
                        "mcm_option_skill_total_hint",
                        "The total available skill points for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "5")
                    .ToString(),
            @default: 780,
            minValue: 90,
            maxValue: 1600,
            presets: new Dictionary<string, object> { [Presets.Freeform] = 1600 }
        );

        public static readonly Option<int> SkillTotalTier6 = CreateOption(
            section: () => L.S("mcm_section_skill_totals", "Skill Totals"),
            key: "SkillTotalTier6",
            name: () =>
                L.T("mcm_option_skill_total", "Tier {TIER} Skill Total")
                    .SetTextVariable("TIER", "6")
                    .ToString(),
            hint: () =>
                L.T(
                        "mcm_option_skill_total_hint",
                        "The total available skill points for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "6")
                    .ToString(),
            @default: 1015,
            minValue: 90,
            maxValue: 1600,
            presets: new Dictionary<string, object> { [Presets.Freeform] = 1600 }
        );

        public static readonly Option<int> SkillTotalTier7Plus = CreateOption(
            section: () => L.S("mcm_section_skill_totals", "Skill Totals"),
            key: "SkillTotalTier7Plus",
            name: () =>
                L.T("mcm_option_skill_total", "Tier {TIER} Skill Total")
                    .SetTextVariable("TIER", "7+")
                    .ToString(),
            hint: () =>
                L.T(
                        "mcm_option_skill_total_hint",
                        "The total available skill points for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "7+")
                    .ToString(),
            @default: 1600,
            minValue: 90,
            maxValue: 1600,
            presets: new Dictionary<string, object> { [Presets.Freeform] = 1600 }
        );
    }
}
