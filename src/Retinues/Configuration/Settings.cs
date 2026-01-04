using Retinues.Framework.Runtime;
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
                section: () => TroopUnlocks.Name,
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
            name: L.F("mcm_option_skill_cap", "Tier {TIER} Skill Cap"),
            hint: L.F("mcm_option_skill_cap_hint", "Maximum skill level for Tier {TIER} troops."),
            minValue: 20,
            maxValue: 330,
            @default: 100
        );

        public static readonly Option<int> SkillCapT1 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER} Skill Cap"),
            hint: L.F("mcm_option_skill_cap_hint", "Maximum skill level for Tier {TIER} troops."),
            minValue: 20,
            maxValue: 360,
            @default: 20
        );

        public static readonly Option<int> SkillCapT2 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER} Skill Cap"),
            hint: L.F("mcm_option_skill_cap_hint", "Maximum skill level for Tier {TIER} troops."),
            minValue: 20,
            maxValue: 360,
            @default: 50
        );

        public static readonly Option<int> SkillCapT3 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER} Skill Cap"),
            hint: L.F("mcm_option_skill_cap_hint", "Maximum skill level for Tier {TIER} troops."),
            minValue: 20,
            maxValue: 360,
            @default: 80
        );

        public static readonly Option<int> SkillCapT4 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER} Skill Cap"),
            hint: L.F("mcm_option_skill_cap_hint", "Maximum skill level for Tier {TIER} troops."),
            minValue: 20,
            maxValue: 360,
            @default: 120
        );

        public static readonly Option<int> SkillCapT5 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER} Skill Cap"),
            hint: L.F("mcm_option_skill_cap_hint", "Maximum skill level for Tier {TIER} troops."),
            minValue: 20,
            maxValue: 360,
            @default: 160
        );

        public static readonly Option<int> SkillCapT6 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER} Skill Cap"),
            hint: L.F("mcm_option_skill_cap_hint", "Maximum skill level for Tier {TIER} troops."),
            minValue: 20,
            maxValue: 360,
            @default: 260
        );

        public static readonly Option<int> SkillCapT7 = CreateOption(
            section: SkillCaps,
            name: L.F("mcm_option_skill_cap", "Tier {TIER} Skill Cap"),
            hint: L.F("mcm_option_skill_cap_hint", "Maximum skill level for Tier {TIER} troops."),
            minValue: 20,
            maxValue: 360,
            @default: 360
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
            name: L.F("mcm_option_skill_total", "Tier {TIER} Skill Total"),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Total available skill points for Tier {TIER} troops."
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 90
        );

        public static readonly Option<int> SkillTotalT1 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER} Skill Total"),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Total available skill points for Tier {TIER} troops."
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 90
        );

        public static readonly Option<int> SkillTotalT2 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER} Skill Total"),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Total available skill points for Tier {TIER} troops."
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 210
        );

        public static readonly Option<int> SkillTotalT3 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER} Skill Total"),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Total available skill points for Tier {TIER} troops."
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 360
        );

        public static readonly Option<int> SkillTotalT4 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER} Skill Total"),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Total available skill points for Tier {TIER} troops."
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 555
        );

        public static readonly Option<int> SkillTotalT5 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER} Skill Total"),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Total available skill points for Tier {TIER} troops."
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 780
        );

        public static readonly Option<int> SkillTotalT6 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER} Skill Total"),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Total available skill points for Tier {TIER} troops."
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 1015
        );

        public static readonly Option<int> SkillTotalT7 = CreateOption(
            section: SkillTotals,
            name: L.F("mcm_option_skill_total", "Tier {TIER} Skill Total"),
            hint: L.F(
                "mcm_option_skill_total_hint",
                "Total available skill points for Tier {TIER} troops."
            ),
            minValue: 90,
            maxValue: 1600,
            @default: 1600
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
