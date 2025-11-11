using System.Collections.Generic;
using Retinues.Mods;
using Retinues.Utils;

namespace Retinues.Configuration
{
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
            System.Func<string> disabledHint = null,
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
                disabledHint,
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
                    "Maximum proportion of elite retinue troops in player party."
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
                    "Maximum proportion of basic retinue troops in player party."
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

        public static readonly Option<int> RetinueConversionCostPerTier = CreateOption(
            section: () => L.S("mcm_section_retinues", "Retinues"),
            name: () =>
                L.S(
                    "mcm_option_retinue_conversion_cost_per_tier",
                    "Retinue Conversion Cost Per Tier"
                ),
            key: "RetinueConversionCostPerTier",
            hint: () =>
                L.S(
                    "mcm_option_retinue_conversion_cost_per_tier_hint",
                    "Conversion cost for retinue troops per tier."
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

        public static readonly Option<int> RetinueRankUpCostPerTier = CreateOption(
            section: () => L.S("mcm_section_retinues", "Retinues"),
            name: () =>
                L.S("mcm_option_retinue_rank_up_cost_per_tier", "Retinue Rank Up Cost Per Tier"),
            key: "RetinueRankUpCostPerTier",
            hint: () =>
                L.S(
                    "mcm_option_retinue_rank_up_cost_per_tier_hint",
                    "Rank up cost for retinue troops per tier."
                ),
            @default: 1000,
            minValue: 0,
            maxValue: 5000,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 0,
                [Presets.Realistic] = 1000,
            }
        );

        public static readonly Option<bool> RestrictConversionToFiefs = CreateOption(
            section: () => L.S("mcm_section_retinues", "Retinues"),
            name: () =>
                L.S(
                    "mcm_option_restrict_conversion_to_fiefs",
                    "Restrict Retinue Conversion To Fiefs"
                ),
            key: "RestrictConversionToFiefs",
            hint: () =>
                L.S(
                    "mcm_option_restrict_conversion_to_fiefs_hint",
                    "Player can only convert retinues when in a fief (clan retinues can be converted in any settlement)."
                ),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = true,
            }
        );

        // ─────────────────────────────────────────────────────
        // Recruitment
        // ─────────────────────────────────────────────────────

        public static readonly Option<float> VolunteerSwapProportion = CreateOption(
            section: () => L.S("mcm_section_recruitment", "Recruitment"),
            name: () => L.S("mcm_option_swap_proportion", "Volunteer Swap Proportion"),
            key: "VolunteerSwapProportion",
            hint: () =>
                L.S(
                    "mcm_option_swap_proportion_hint",
                    "Proportion of volunteers that get swapped for custom troops."
                ),
            @default: 1f,
            minValue: 0,
            maxValue: 1,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = 1.0f,
                [Presets.Realistic] = 1.0f,
            }
        );

        public static readonly Option<bool> RecruitAnywhere = CreateOption(
            section: () => L.S("mcm_section_recruitment", "Recruitment"),
            name: () => L.S("mcm_option_recruit_anywhere", "Recruit Clan Troops Anywhere"),
            key: "RecruitAnywhere",
            hint: () =>
                L.S(
                    "mcm_option_recruit_anywhere_hint",
                    "Player can recruit clan troops in any settlement."
                ),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = false,
            }
        );

        public static readonly Option<bool> SwapOnlyForCorrectCulture = CreateOption(
            section: () => L.S("mcm_section_recruitment", "Recruitment"),
            name: () =>
                L.S(
                    "mcm_option_swap_only_for_correct_culture",
                    "Swap Volunteers Only For Correct Culture"
                ),
            key: "SwapOnlyForCorrectCulture",
            hint: () =>
                L.S(
                    "mcm_option_swap_only_for_correct_culture_hint",
                    "Volunteers in settlements of a different culture will not be replaced by custom troops."
                ),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = true,
            }
        );

        public static readonly Option<bool> ClanTroopsOverKingdomTroops = CreateOption(
            section: () => L.S("mcm_section_recruitment", "Recruitment"),
            name: () =>
                L.S(
                    "mcm_option_clan_troops_over_kingdom_troops",
                    "Clan Troops Over Kingdom Troops"
                ),
            key: "ClanTroopsOverKingdomTroops",
            hint: () =>
                L.S(
                    "mcm_option_clan_troops_over_kingdom_troops_hint",
                    "If a fief is both a clan fief and a kingdom fief, clan troops will be prioritized over kingdom troops."
                ),
            @default: true,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = true,
            }
        );

        public static readonly Option<bool> NoKingdomTroops = CreateOption(
            section: () => L.S("mcm_section_recruitment", "Recruitment"),
            name: () => L.S("mcm_option_no_kingdom_troops", "No Kingdom Troops"),
            key: "NoKingdomTroops",
            hint: () =>
                L.S(
                    "mcm_option_no_kingdom_troops_hint",
                    "The custom kingdom troop tree will be disabled."
                ),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = false,
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
            @default: true,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = true,
            }
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
                L.S(
                    "mcm_option_all_lords_recruit_custom_troops_hint",
                    "Any lord can recruit custom troops in the player's fiefs."
                ),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = true,
            }
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
                    "Enables the global troop editor to modify any troop in the game. Disable if you are encountering issues with non-player troops."
                ),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = false,
            },
            requiresRestart: true,
            disabled: ModCompatibility.NoGlobalEditor,
            disabledOverride: false,
            disabledHint: () =>
                L.S(
                    "mcm_option_global_editor_enabled_disabled_hint",
                    "The global troop editor is disabled due to compatibility issues with other activated mods."
                )
        );

        public static readonly Option<bool> KeepUpgradeRequirementsForVanilla = CreateOption(
            section: () => L.S("mcm_section_global_editor", "Global Editor"),
            name: () =>
                L.S(
                    "mcm_option_keep_upgrade_requirements_for_vanilla",
                    "Keep Upgrade Requirements For Vanilla Troops"
                ),
            key: "KeepUpgradeRequirementsForVanilla",
            hint: () =>
                L.S(
                    "mcm_option_keep_upgrade_requirements_for_vanilla_hint",
                    "Vanilla troops retain their original upgrade item requirements when edited."
                ),
            @default: true,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = true,
            }
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
                    "Player can only edit troops when in a fief owned by their clan or kingdom (clan retinues can be edited in any settlement)."
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
                    "Maximum number of upgrade targets for elite troops."
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
                    "Maximum number of upgrade targets for basic troops."
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
                    "Enable the Doctrines system and its features."
                ),
            requiresRestart: true,
            @default: true,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = true,
            }
        );

        public static readonly Option<bool> DisableFeatRequirements = CreateOption(
            section: () => L.S("mcm_section_doctrines", "Doctrines"),
            name: () => L.S("mcm_option_disable_feat_requirements", "Disable Feat Requirements"),
            key: "DisableFeatRequirements",
            hint: () =>
                L.S(
                    "mcm_option_disable_feat_requirements_hint",
                    "Disables feat requirements for unlocking doctrines."
                ),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = false,
            }
        );

        // ─────────────────────────────────────────────────────
        // Immersion & Fluff
        // ─────────────────────────────────────────────────────

        public static readonly Option<bool> EnableTroopCustomization = CreateOption(
            section: () => L.S("mcm_section_fluff", "Immersion & Fluff"),
            name: () => L.S("mcm_option_enable_troop_customization", "Enable Appearance Controls"),
            key: "EnableTroopCustomization",
            hint: () =>
                L.S(
                    "mcm_option_enable_troop_customization_hint",
                    "Adds appearance customization controls (age, height, weight, build)."
                ),
            @default: true,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = true,
            }
        );

        public static readonly Option<bool> ReplaceAmbientNPCs = CreateOption(
            section: () => L.S("mcm_section_fluff", "Immersion & Fluff"),
            name: () => L.S("mcm_option_replace_ambient_npcs", "Replace Ambient NPCs"),
            key: "ReplaceAmbientNPCs",
            hint: () =>
                L.S(
                    "mcm_option_replace_ambient_npcs_hint",
                    "Replaces ambient NPCs in settlements with player faction characters."
                ),
            @default: true,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = true,
            }
        );

        // ─────────────────────────────────────────────────────
        // Equipment
        // ─────────────────────────────────────────────────────

        public static readonly Option<bool> PayForEquipment = CreateOption(
            section: () => L.S("mcm_section_equipment", "Equipment"),
            name: () => L.S("mcm_option_pay_for_equipment", "Pay For Troop Equipment"),
            key: "PayForEquipment",
            hint: () =>
                L.S("mcm_option_pay_for_equipment_hint", "Upgrading troop equipment costs money."),
            @default: true,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = true,
            }
        );

        public static readonly Option<float> EquipmentPriceModifier = CreateOption(
            section: () => L.S("mcm_section_equipment", "Equipment"),
            name: () => L.S("mcm_option_equipment_price_modifier", "Equipment Price Modifier"),
            key: "EquipmentPriceModifier",
            hint: () =>
                L.S(
                    "mcm_option_equipment_price_modifier_hint",
                    "Modifier for equipment price compared to base game prices."
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

        public static readonly Option<bool> EquipmentChangeTakesTime = CreateOption(
            section: () => L.S("mcm_section_equipment", "Equipment"),
            name: () =>
                L.S(
                    "mcm_option_equipment_change_takes_time",
                    "Changing Troop Equipment Takes Time"
                ),
            key: "EquipmentChangeTakesTime",
            hint: () =>
                L.S(
                    "mcm_option_equipment_change_takes_time_hint",
                    "Changing a troop's equipment takes time."
                ),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = true,
            }
        );

        public static readonly Option<int> EquipmentChangeTimeModifier = CreateOption(
            section: () => L.S("mcm_section_equipment", "Equipment"),
            name: () =>
                L.S("mcm_option_equipment_change_time_modifier", "Equipment Change Time Modifier"),
            key: "EquipmentChangeTimeModifier",
            hint: () =>
                L.S(
                    "mcm_option_equipment_change_time_modifier_hint",
                    "Modifier for equipment change time."
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
                    "Player can only purchase items available in the town inventory."
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
                    "Maximum allowed tier difference between troops and equipment."
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
                    "Troops always use their main battle equipment set in combat."
                ),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = false,
            }
        );

        public static readonly Option<bool> IgnoreCivilianHorseForUpgradeRequirements =
            CreateOption(
                section: () => L.S("mcm_section_equipment", "Equipment"),
                name: () =>
                    L.S(
                        "mcm_option_ignore_civilian_horse_for_upgrade_requirements",
                        "Ignore Civilian Horse for Upgrade Requirements"
                    ),
                key: "IgnoreCivilianHorseForUpgradeRequirements",
                hint: () =>
                    L.S(
                        "mcm_option_ignore_civilian_horse_for_upgrade_requirements_hint",
                        "When checking for mount requirements when upgrading troops, ignore the civilian set's horse."
                    ),
                @default: true,
                presets: new Dictionary<string, object>
                {
                    [Presets.Freeform] = true,
                    [Presets.Realistic] = false,
                }
            );

        public static readonly Option<bool> NeverRequireNobleHorse = CreateOption(
            section: () => L.S("mcm_section_equipment", "Equipment"),
            name: () => L.S("mcm_option_never_require_noble_horse", "Never Require Noble Horse"),
            key: "NeverRequireNobleHorse",
            hint: () =>
                L.S(
                    "mcm_option_never_require_noble_horse_hint",
                    "Troops never require noble horses for upgrades, a war horse is always enough."
                ),
            @default: true,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = true,
            }
        );

        public static readonly Option<bool> NoMountForTier1 = CreateOption(
            section: () => L.S("mcm_section_equipment", "Equipment"),
            name: () => L.S("mcm_option_disallow_mounts_for_tier_1", "Disallow Mounts For Tier 1"),
            key: "NoMountForTier1",
            hint: () =>
                L.S(
                    "mcm_option_disallow_mounts_for_tier_1_hint",
                    "Tier 1 troops cannot have mounts."
                ),
            @default: true,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = true,
            }
        );

        public static readonly Option<bool> CopyAllSetsWhenCloning = CreateOption(
            section: () => L.S("mcm_section_equipment", "Equipment"),
            name: () => L.S("mcm_option_copy_all_sets_when_cloning", "Copy All Sets When Cloning"),
            key: "CopyAllSetsWhenCloning",
            hint: () =>
                L.S(
                    "mcm_option_copy_all_sets_when_cloning_hint",
                    "When cloning troop equipment, copy all equipment sets instead of just one battle and one civilian set."
                ),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = false,
            },
            disabled: ModCompatibility.NoAlternateEquipmentSets,
            disabledOverride: false,
            disabledHint: () =>
                L.S(
                    "mcm_option_copy_all_sets_disabled_hint",
                    "Alternate sets are disabled due to incompatibilities with other activated mods."
                )
        );

        // ─────────────────────────────────────────────────────
        // Skills (training)
        // ─────────────────────────────────────────────────────

        public static readonly Option<bool> TrainingTakesTime = CreateOption(
            section: () => L.S("mcm_section_skills", "Skills"),
            name: () => L.S("mcm_option_training_takes_time", "Troop Training Takes Time"),
            key: "TrainingTakesTime",
            hint: () => L.S("mcm_option_training_takes_time_hint", "Troop training takes time."),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = true,
            }
        );

        public static readonly Option<int> TrainingTimeModifier = CreateOption(
            section: () => L.S("mcm_section_skills", "Skills"),
            name: () => L.S("mcm_option_training_time_modifier", "Training Time Modifier"),
            key: "TrainingTimeModifier",
            hint: () =>
                L.S("mcm_option_training_time_modifier_hint", "Modifier for troop training time."),
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
            name: () => L.S("mcm_option_base_skill_xp_cost", "Base Skill XP Cost"),
            key: "BaseSkillXpCost",
            hint: () =>
                L.S("mcm_option_base_skill_xp_cost_hint", "Base XP cost for increasing a skill."),
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
            name: () => L.S("mcm_option_skill_xp_cost_per_point", "Skill XP Cost Per Point"),
            key: "SkillXpCostPerPoint",
            hint: () =>
                L.S(
                    "mcm_option_skill_xp_cost_per_point_hint",
                    "Scalable XP cost for each point of skill increase."
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
            hint: () => L.S("mcm_option_shared_xp_pool_hint", "All troops share the same XP pool."),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = false,
            }
        );

        public static readonly Option<bool> RefundXpOnDecrease = CreateOption(
            section: () => L.S("mcm_section_skills", "Skills"),
            name: () => L.S("mcm_option_refund_xp_on_decrease", "Refund XP On Decrease"),
            key: "RefundXpOnDecrease",
            hint: () =>
                L.S(
                    "mcm_option_refund_xp_on_decrease_hint",
                    "When decreasing a troop's skill, refund the XP cost."
                ),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = false,
            }
        );

        // ─────────────────────────────────────────────────────
        // Unlocks
        // ─────────────────────────────────────────────────────

        public static readonly Option<bool> UnlockFromKills = CreateOption(
            section: () => L.S("mcm_section_unlocks", "Unlocks"),
            name: () => L.S("mcm_option_unlock_from_kills", "Unlock From Kills"),
            key: "UnlockFromKills",
            hint: () =>
                L.S(
                    "mcm_option_unlock_from_kills_hint",
                    "Unlock equipment by defeating enemies wearing it."
                ),
            @default: true,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = true,
            }
        );

        public static readonly Option<int> KillsForUnlock = CreateOption(
            section: () => L.S("mcm_section_unlocks", "Unlocks"),
            name: () => L.S("mcm_option_required_kills_for_unlock", "Required Kills For Unlock"),
            key: "KillsForUnlock",
            hint: () =>
                L.S(
                    "mcm_option_required_kills_for_unlock_hint",
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

        public static readonly Option<bool> UnlockFromDiscards = CreateOption(
            section: () => L.S("mcm_section_unlocks", "Unlocks"),
            name: () => L.S("mcm_option_unlock_from_discarded", "Unlock From Discarded Items"),
            key: "UnlockFromDiscarded",
            hint: () =>
                L.S("mcm_option_unlock_from_discarded_hint", "Unlock equipment by discarding it."),
            @default: true,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = false,
                [Presets.Realistic] = true,
            }
        );

        public static readonly Option<int> DiscardsForUnlock = CreateOption(
            section: () => L.S("mcm_section_unlocks", "Unlocks"),
            name: () =>
                L.S(
                    "mcm_option_required_discards_for_unlock",
                    "Required Discarded Items For Unlock"
                ),
            key: "DiscardsForUnlock",
            hint: () =>
                L.S(
                    "mcm_option_required_discards_for_unlock_hint",
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

        public static readonly Option<bool> OwnCultureUnlockBonuses = CreateOption(
            section: () => L.S("mcm_section_unlocks", "Unlocks"),
            name: () => L.S("mcm_option_own_culture_unlock_bonuses", "Own Culture Unlock Bonuses"),
            key: "OwnCultureUnlockBonuses",
            hint: () =>
                L.S(
                    "mcm_option_own_culture_unlock_bonuses_hint",
                    "Whether kills also unlock items from the custom troop's culture."
                ),
            @default: true,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = false,
            }
        );

        public static readonly Option<bool> UnlockFromCulture = CreateOption(
            section: () => L.S("mcm_section_unlocks", "Unlocks"),
            name: () => L.S("mcm_option_unlock_from_culture", "Unlock From Culture"),
            key: "UnlockFromCulture",
            hint: () =>
                L.S(
                    "mcm_option_unlock_from_culture_hint",
                    "Player culture and player-led kingdom culture equipment is always available."
                ),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = false,
            }
        );

        public static readonly Option<bool> AllEquipmentUnlocked = CreateOption(
            section: () => L.S("mcm_section_unlocks", "Unlocks"),
            name: () => L.S("mcm_option_all_equipment_unlocked", "All Equipment Unlocked"),
            key: "AllEquipmentUnlocked",
            hint: () =>
                L.S(
                    "mcm_option_all_equipment_unlocked_hint",
                    "All equipment unlocked on game start."
                ),
            @default: false,
            presets: new Dictionary<string, object>
            {
                [Presets.Freeform] = true,
                [Presets.Realistic] = false,
            }
        );

        // ─────────────────────────────────────────────────────
        // Debug
        // ─────────────────────────────────────────────────────

        public static readonly Option<bool> DebugMode = CreateOption(
            section: () => L.S("mcm_section_debug", "Debug"),
            name: () => L.S("mcm_option_debug_mode", "Debug Mode"),
            key: "DebugMode",
            hint: () =>
                L.S(
                    "mcm_option_debug_mode_hint",
                    "Outputs many more logs (may impact performance)."
                ),
            @default: false
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
            @default: 535,
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
            @default: 710,
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
            @default: 915,
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
