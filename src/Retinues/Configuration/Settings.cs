using Retinues.Editor.Events;
using Retinues.Framework.Runtime;
using Retinues.Modules;
using Retinues.UI.Services;
using static Retinues.Configuration.SettingsManager;

namespace Retinues.Configuration
{
    /// <summary>
    /// Definitions for all configuration options (no boilerplate here).
    /// </summary>
    [SafeClass]
    public static class Settings
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     User Interface                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section UserInterface = CreateSection(
            name: L.F("mcm_section_user_interface", "User Interface")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> EditorHotkey = CreateOption(
            section: UserInterface,
            name: L.F("mcm_option_editor_hotkey", "Editor Hotkey"),
            hint: L.F(
                "mcm_option_editor_hotkey_hint",
                "Enables the [R] hotkey to open the editor."
            ),
            @default: true
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Doctrines                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section Doctrines = CreateSection(
            name: L.F("mcm_section_doctrines", "Doctrines")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> EnableDoctrines = CreateOption(
            section: Doctrines,
            name: L.F("mcm_option_enable_doctrines", "Enable Doctrines"),
            hint: L.F(
                "mcm_option_enable_doctrines_hint",
                "Toggles the Doctrines feature on or off."
            ),
#if !DEBUG
            requiresRestart: true,
#endif
            @default: true
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Retinues                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section Retinues = CreateSection(
            name: L.F("mcm_section_retinues", "Retinues")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> EnableRetinues = CreateOption(
            section: Retinues,
            name: L.F("mcm_option_enable_retinues", "Enable Retinues"),
            hint: L.F("mcm_option_enable_retinues_hint", "Toggles the Retinues feature on or off."),
            @default: true,
            fires: UIEvent.Faction
        );

        public static readonly Option<float> MaxRetinueRatio = CreateOption(
            section: Retinues,
            name: L.F("mcm_option_max_retinue_ratio", "Max Retinue Ratio"),
            hint: L.F(
                "mcm_option_max_retinue_ratio_hint",
                "The maximum number of retinues as a ratio of the party size limit."
            ),
            minValue: 0,
            maxValue: 1,
            @default: 0.15f,
            @freeform: 1f,
            @realistic: 0.1f,
#if !DEBUG
            requiresRestart: true,
#endif
            dependsOn: EnableRetinues
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Equipment Unlocks                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section EquipmentUnlocks = CreateSection(
            name: L.F("mcm_section_equipment_unlocks", "Equipment Unlocks")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> AllEquipmentUnlocked = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_all_equipment_unlocked", "All Equipment Unlocked"),
            hint: L.F(
                "mcm_option_all_equipment_unlocked_hint",
                "Whether all equipment is immediately unlocked or needs to be unlocked through gameplay."
            ),
            @default: false,
            @freeform: true,
            fires: UIEvent.Page
        );

        public static readonly Option<bool> UnlockItemsThroughKills = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_unlock_items_through_kills", "Unlock Items Through Kills"),
            hint: L.F(
                "mcm_option_unlock_items_through_kills_hint",
                "Whether items are unlocked by defeating enemies in battles."
            ),
            @default: true,
            dependsOn: AllEquipmentUnlocked,
            dependsOnValue: false
        );

        public static readonly Option<int> RequiredKillsForUnlock = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_required_kills_for_unlock", "Required Kills For Unlock"),
            hint: L.F(
                "mcm_option_required_kills_for_unlock_hint",
                "The number of enemy troops wearing an item that must be defeated to unlock it."
            ),
            minValue: 1,
            maxValue: 1000,
            @default: 100,
            @realistic: 200,
            dependsOn: UnlockItemsThroughKills,
            dependsOnValue: true
        );

        public static readonly Option<bool> UnlockItemsThroughDiscards = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_unlock_items_through_discards", "Unlock Items Through Discards"),
            hint: L.F(
                "mcm_option_unlock_items_through_discards_hint",
                "Whether items are unlocked by discarding items."
            ),
            @default: false,
            dependsOn: AllEquipmentUnlocked,
            dependsOnValue: false
        );

        public static readonly Option<int> RequiredDiscardsForUnlock = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_required_discards_for_unlock", "Required Discards For Unlock"),
            hint: L.F(
                "mcm_option_required_discards_for_unlock_hint",
                "The number of times an item must be discarded to unlock it."
            ),
            minValue: 1,
            maxValue: 100,
            @default: 10,
            @realistic: 20,
            dependsOn: UnlockItemsThroughDiscards
        );

        public static readonly Option<int> DefaultUnlockedAmountPerSlot = CreateOption(
            section: EquipmentUnlocks,
            name: L.F(
                "mcm_option_default_unlocked_amount_per_slot",
                "Pre-Unlocked Amount Per Slot"
            ),
            hint: L.F(
                "mcm_option_default_unlocked_amount_per_slot_hint",
                "The number of items unlocked per equipment slot on game start."
            ),
            dependsOn: AllEquipmentUnlocked,
            dependsOnValue: false,
            minValue: 0,
            maxValue: 10,
            @default: 3
        );

        public static readonly Option<int> DefaultUnlockedItemMaxTier = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_default_unlocked_item_max_tier", "Pre-Unlocked Item Max Tier"),
            hint: L.F(
                "mcm_option_default_unlocked_item_max_tier_hint",
                "The maximum tier of items unlocked on game start."
            ),
            dependsOn: AllEquipmentUnlocked,
            dependsOnValue: false,
            minValue: 1,
            maxValue: 6,
            @default: 2,
            @realistic: 1
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Troop Unlocks                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section TroopUnlocks = CreateSection(
            name: L.F("mcm_section_troop_unlocks", "Troop Unlocks")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public enum EquipmentMode
        {
            SingleSet,
            AllSets,
            RandomSet,
            EmptySet,
        }

        public static readonly MultiChoiceOption<EquipmentMode> StarterEquipment =
            CreateMultiChoiceOption(
                section: TroopUnlocks,
                name: L.F("mcm_option_starter_equipment", "Starter Equipment"),
                hint: L.F(
                    "mcm_option_starter_equipment_hint",
                    "Sets the starter equipment for newly unlocked troops."
                ),
                @default: EquipmentMode.RandomSet,
                choices:
                [
                    EquipmentMode.SingleSet,
                    EquipmentMode.AllSets,
                    EquipmentMode.RandomSet,
                    EquipmentMode.EmptySet,
                ],
                choiceFormatter: v =>
                    v switch
                    {
                        EquipmentMode.SingleSet => L.S(
                            "starter_equipment_single_set",
                            "Copy One Set"
                        ),
                        EquipmentMode.AllSets => L.S("starter_equipment_all_sets", "Copy All Sets"),
                        EquipmentMode.RandomSet => L.S("starter_equipment_random_set", "Random"),
                        EquipmentMode.EmptySet => L.S("starter_equipment_empty_set", "Empty"),
                        _ => v.ToString(),
                    }
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equipment                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section Equipment = CreateSection(
            name: L.F("mcm_section_equipment", "Equipment")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> EquipmentCostsGold = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_equipment_costs_gold", "Equipment Costs Gold"),
            hint: L.F(
                "mcm_option_equipment_costs_gold_hint",
                "Whether equipping new items should cost gold."
            ),
            @default: true,
            @freeform: false,
            fires: UIEvent.Page
        );

        public static readonly Option<float> EquipmentCostMultiplier = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_equipment_cost_multiplier", "Equipment Cost Multiplier"),
            hint: L.F(
                "mcm_option_equipment_cost_multiplier_hint",
                "Multiplier applied to the base cost of items when calculating equip costs."
            ),
            minValue: 0.1f,
            maxValue: 10f,
            @default: 1f,
            @realistic: 2f,
            dependsOn: EquipmentCostsGold,
            fires: UIEvent.Page
        );

        public static readonly Option<bool> LimitEquipmentByWeight = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_limit_equipment_by_weight", "Tier-Based Equipment Weight Limits"),
            hint: L.F(
                "mcm_option_limit_equipment_by_weight_hint",
                "Whether to limit equippable items based on troop tier and total item weights."
            ),
            @default: true,
            fires: UIEvent.Equipment
        );

        public static readonly Option<float> EquipmentWeightLimitMultiplier = CreateOption(
            section: Equipment,
            name: L.F(
                "mcm_option_equipment_weight_limit_multiplier",
                "Equipment Weight Limit Multiplier"
            ),
            hint: L.F(
                "mcm_option_equipment_weight_limit_multiplier_hint",
                "Multiplier applied to the base weight limit when calculating equippable items."
            ),
            minValue: 0.5f,
            maxValue: 2f,
            @default: 1f,
            dependsOn: LimitEquipmentByWeight,
            fires: UIEvent.Equipment
        );

        public static readonly Option<bool> LimitEquipmentByValue = CreateOption(
            section: Equipment,
            name: L.F("mcm_option_limit_equipment_by_value", "Tier-Based Equipment Value Limits"),
            hint: L.F(
                "mcm_option_limit_equipment_by_value_hint",
                "Whether to limit equippable items based on troop tier and total item values."
            ),
            @default: true,
            fires: UIEvent.Equipment
        );

        public static readonly Option<float> EquipmentValueLimitMultiplier = CreateOption(
            section: Equipment,
            name: L.F(
                "mcm_option_equipment_value_limit_multiplier",
                "Equipment Value Limit Multiplier"
            ),
            hint: L.F(
                "mcm_option_equipment_value_limit_multiplier_hint",
                "Multiplier applied to the base value limit when calculating equippable items."
            ),
            minValue: 0.5f,
            maxValue: 2f,
            @default: 1f,
            dependsOn: LimitEquipmentByValue,
            fires: UIEvent.Equipment
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Skills                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section Skills = CreateSection(
            name: L.F("mcm_section_skills", "Skills")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> EnableSkillPointsSystem = CreateOption(
            section: Skills,
            name: L.F("mcm_option_enable_skill_points_system", "Enable Skill Points System"),
            hint: L.F(
                "mcm_option_enable_skill_points_system_hint",
                "Toggles the Skill Points System feature on or off. If disabled, troops will not need to earn skill points in battle."
            ),
            @default: true,
            @freeform: false,
            fires: UIEvent.Page
        );

        public static readonly Option<float> SkillPointGainMultiplier = CreateOption(
            section: Skills,
            name: L.F("mcm_option_skill_point_gain_multiplier", "Skill Point Gain Multiplier"),
            hint: L.F(
                "mcm_option_skill_point_gain_multiplier_hint",
                "Multiplier applied to the base skill point gain rate."
            ),
            minValue: 0.1f,
            maxValue: 5f,
            @default: 1f,
            @realistic: 0.5f,
            dependsOn: EnableSkillPointsSystem
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Skill Caps                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section SkillCaps = CreateSection(
            name: L.F("mcm_section_skill_caps", "Skill Caps")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<int> SkillCapT0 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER} Skill Cap", ("TIER", 0)),
            hint: L.F(
                "mcm_option_skill_cap_hint",
                "Maximum skill level for Tier {TIER} troops.",
                ("TIER", 0)
            ),
            minValue: 20,
            maxValue: 360,
            @default: 20,
            fires: UIEvent.Skill
        );

        public static readonly Option<int> SkillCapT1 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER} Skill Cap", ("TIER", 1)),
            hint: L.F(
                "mcm_option_skill_cap_hint",
                "Maximum skill level for Tier {TIER} troops.",
                ("TIER", 1)
            ),
            minValue: 20,
            maxValue: 360,
            @default: 20,
            fires: UIEvent.Skill
        );

        public static readonly Option<int> SkillCapT2 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER} Skill Cap", ("TIER", 2)),
            hint: L.F(
                "mcm_option_skill_cap_hint",
                "Maximum skill level for Tier {TIER} troops.",
                ("TIER", 2)
            ),
            minValue: 20,
            maxValue: 360,
            @default: 60,
            fires: UIEvent.Skill
        );

        public static readonly Option<int> SkillCapT3 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER} Skill Cap", ("TIER", 3)),
            hint: L.F(
                "mcm_option_skill_cap_hint",
                "Maximum skill level for Tier {TIER} troops.",
                ("TIER", 3)
            ),
            minValue: 20,
            maxValue: 360,
            @default: 80,
            fires: UIEvent.Skill
        );

        public static readonly Option<int> SkillCapT4 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER} Skill Cap", ("TIER", 4)),
            hint: L.F(
                "mcm_option_skill_cap_hint",
                "Maximum skill level for Tier {TIER} troops.",
                ("TIER", 4)
            ),
            minValue: 20,
            maxValue: 360,
            @default: 120,
            fires: UIEvent.Skill
        );

        public static readonly Option<int> SkillCapT5 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER} Skill Cap", ("TIER", 5)),
            hint: L.F(
                "mcm_option_skill_cap_hint",
                "Maximum skill level for Tier {TIER} troops.",
                ("TIER", 5)
            ),
            minValue: 20,
            maxValue: 360,
            @default: 160,
            fires: UIEvent.Skill
        );

        public static readonly Option<int> SkillCapT6 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER} Skill Cap", ("TIER", 6)),
            hint: L.F(
                "mcm_option_skill_cap_hint",
                "Maximum skill level for Tier {TIER} troops.",
                ("TIER", 6)
            ),
            minValue: 20,
            maxValue: 360,
            @default: 260,
            fires: UIEvent.Skill
        );

        public static readonly Option<int> SkillCapT7 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER} Skill Cap", ("TIER", 7)),
            hint: L.F(
                "mcm_option_skill_cap_hint",
                "Maximum skill level for Tier {TIER} troops.",
                ("TIER", 7)
            ),
            minValue: 20,
            maxValue: 360,
            @default: 360,
            disabled: !Mods.T7TroopUnlocker.IsLoaded,
            fires: UIEvent.Skill
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Skill Totals                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section SkillTotals = CreateSection(
            name: L.F("mcm_section_skill_totals", "Skill Totals")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<int> SkillTotalT0 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER} Skill Total", ("TIER", 0)),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Total available skill points for Tier {TIER} troops.",
                ("TIER", 0)
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 90,
            fires: UIEvent.Skill
        );

        public static readonly Option<int> SkillTotalT1 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER} Skill Total", ("TIER", 1)),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Total available skill points for Tier {TIER} troops.",
                ("TIER", 1)
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 90,
            fires: UIEvent.Skill
        );

        public static readonly Option<int> SkillTotalT2 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER} Skill Total", ("TIER", 2)),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Total available skill points for Tier {TIER} troops.",
                ("TIER", 2)
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 210,
            fires: UIEvent.Skill
        );

        public static readonly Option<int> SkillTotalT3 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER} Skill Total", ("TIER", 3)),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Total available skill points for Tier {TIER} troops.",
                ("TIER", 3)
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 360,
            fires: UIEvent.Skill
        );

        public static readonly Option<int> SkillTotalT4 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER} Skill Total", ("TIER", 4)),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Total available skill points for Tier {TIER} troops.",
                ("TIER", 4)
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 555,
            fires: UIEvent.Skill
        );

        public static readonly Option<int> SkillTotalT5 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER} Skill Total", ("TIER", 5)),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Total available skill points for Tier {TIER} troops.",
                ("TIER", 5)
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 780,
            fires: UIEvent.Skill
        );

        public static readonly Option<int> SkillTotalT6 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER} Skill Total", ("TIER", 6)),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Total available skill points for Tier {TIER} troops.",
                ("TIER", 6)
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 1015,
            fires: UIEvent.Skill
        );

        public static readonly Option<int> SkillTotalT7 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER} Skill Total", ("TIER", 7)),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Total available skill points for Tier {TIER} troops.",
                ("TIER", 7)
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 1600,
            disabled: !Mods.T7TroopUnlocker.IsLoaded,
            fires: UIEvent.Skill
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Debug                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Section Debug = CreateSection(
            name: L.F("mcm_section_debug", "Debug")
        );

        /* ━━━━━━━━ Options ━━━━━━━ */

        public static readonly Option<bool> DebugMode = CreateOption(
            section: Debug,
            name: L.F("mcm_option_debug_mode", "Debug Mode"),
            hint: L.F("mcm_option_debug_mode_hint", "Enables debug logging and additional checks."),
            @default: false
        );
    }
}
