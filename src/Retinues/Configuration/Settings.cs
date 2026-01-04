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
            minValue: 0,
            maxValue: 9,
            @default: 3
        );

        public static readonly Option<int> DefaultUnlockedItemMaxTier = CreateOption(
            section: EquipmentUnlocks,
            name: L.F("mcm_option_default_unlocked_item_max_tier", "Pre-Unlocked Item Max Tier"),
            hint: L.F(
                "mcm_option_default_unlocked_item_max_tier_hint",
                "The maximum tier of items unlocked on game start."
            ),
            minValue: 1,
            maxValue: 6,
            @default: 2
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
            dependsOn: EquipmentCostsGold,
            fires: UIEvent.Page
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
                @default: EquipmentMode.SingleSet,
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
