using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MCM.Abstractions.Base;
using MCM.Abstractions.FluentBuilder;
using MCM.Common;
using Retinues.Troops;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Utils
{
    /// <summary>
    /// Central configuration: defines options, provides typed accessors, and registers the MCM menu.
    /// </summary>
    [SafeClass]
    public static class Config
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Option Model                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class ConfigOption
        {
            public string Section { get; set; } // UI group (e.g., "Recruitment")
            public string Name { get; set; } // UI label
            public string Key { get; set; } // stable ID
            public string Hint { get; set; } // tooltip
            public Type Type { get; set; } // typeof(bool/int/float/string)
            public object Default { get; set; } // default value
            public object Value { get; set; } // current in-memory value
            public int MinValue { get; set; } // numeric ranges (int/float)
            public int MaxValue { get; set; }
        }

        private static readonly List<ConfigOption> _options = [];
        private static readonly Dictionary<string, ConfigOption> _byKey = new(
            StringComparer.OrdinalIgnoreCase
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Import/Export UI                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static string _exportName = SuggestDefaultExportName();
        private static Dropdown<string> _importDropdown = BuildImportDropdown();

        private static bool InCampaign() => Campaign.Current != null;

        private static void ConfirmTroopReplace(string title, string body, Action onConfirm)
        {
            var inquiry = new InquiryData(
                title,
                body,
                true,
                true,
                L.S("continue", "Continue"),
                L.S("cancel", "Cancel"),
                onConfirm,
                () => { }
            );
            InformationManager.ShowInquiry(inquiry);
        }

        private static string SuggestDefaultExportName()
        {
            // troops_YYYY-MM-DD_HH-mm
            var now = DateTime.Now;
            return $"troops_{now:yyyy-MM-dd_HH-mm}";
        }

        private static Dropdown<string> BuildImportDropdown()
        {
            try
            {
                var dir = TroopImportExport.DefaultDir;
                Directory.CreateDirectory(dir);
                var files = Directory
                    .EnumerateFiles(dir, "*.xml", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .OrderByDescending(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (files.Count == 0)
                    files.Add("<no .xml files found>");

                return new Dropdown<string>(files, 0);
            }
            catch
            {
                return new Dropdown<string>(new List<string> { "<error>" }, 0);
            }
        }

        private static void RefreshImportDropdown()
        {
            _importDropdown = BuildImportDropdown();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Option List                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        static Config()
        {
            /* ━━━━━━━ Retinues ━━━━━━━ */

            AddOption(
                section: L.S("mcm_section_retinues", "Retinues"),
                name: L.S("mcm_option_max_elite_retinue_ratio", "Max Elite Retinue Ratio"),
                key: "MaxEliteRetinueRatio",
                hint: L.S(
                    "mcm_option_max_elite_retinue_ratio_hint",
                    "Maximum proportion of elite retinue troops in player party."
                ),
                @default: 0.1f,
                type: typeof(float),
                minValue: 0,
                maxValue: 1
            );

            AddOption(
                section: L.S("mcm_section_retinues", "Retinues"),
                name: L.S("mcm_option_max_basic_retinue_ratio", "Max Basic Retinue Ratio"),
                key: "MaxBasicRetinueRatio",
                hint: L.S(
                    "mcm_option_max_basic_retinue_ratio_hint",
                    "Maximum proportion of basic retinue troops in player party."
                ),
                @default: 0.2f,
                type: typeof(float),
                minValue: 0,
                maxValue: 1
            );

            AddOption(
                section: L.S("mcm_section_retinues", "Retinues"),
                name: L.S(
                    "mcm_option_retinue_conversion_cost_per_tier",
                    "Retinue Conversion Cost Per Tier"
                ),
                key: "RetinueConversionCostPerTier",
                hint: L.S(
                    "mcm_option_retinue_conversion_cost_per_tier_hint",
                    "Conversion cost for retinue troops per tier."
                ),
                @default: 50,
                type: typeof(int),
                minValue: 0,
                maxValue: 200
            );

            AddOption(
                section: L.S("mcm_section_retinues", "Retinues"),
                name: L.S(
                    "mcm_option_retinue_rank_up_cost_per_tier",
                    "Retinue Rank Up Cost Per Tier"
                ),
                key: "RetinueRankUpCostPerTier",
                hint: L.S(
                    "mcm_option_retinue_rank_up_cost_per_tier_hint",
                    "Rank up cost for retinue troops per tier."
                ),
                @default: 1000,
                type: typeof(int),
                minValue: 0,
                maxValue: 5000
            );

            AddOption(
                section: L.S("mcm_section_retinues", "Retinues"),
                name: L.S(
                    "mcm_option_restrict_conversion_to_fiefs",
                    "Restrict Retinue Conversion To Fiefs"
                ),
                key: "RestrictConversionToFiefs",
                hint: L.S(
                    "mcm_option_restrict_conversion_to_fiefs_hint",
                    "Player can only convert retinues when in a fief (clan retinues can be converted in any settlement)."
                ),
                @default: true,
                type: typeof(bool)
            );

            /* ━━━━━━ Recruitment ━━━━━ */

            AddOption(
                section: L.S("mcm_section_recruitment", "Recruitment"),
                name: L.S("mcm_option_recruit_anywhere", "Recruit Clan Troops Anywhere"),
                key: "RecruitAnywhere",
                hint: L.S(
                    "mcm_option_recruit_anywhere_hint",
                    "Player can recruit clan troops in any settlement."
                ),
                @default: false,
                type: typeof(bool)
            );

            AddOption(
                section: L.S("mcm_section_recruitment", "Recruitment"),
                name: L.S(
                    "mcm_option_swap_only_for_correct_culture",
                    "Swap Volunteers Only For Correct Culture"
                ),
                key: "SwapOnlyForCorrectCulture",
                hint: L.S(
                    "mcm_option_swap_only_for_correct_culture_hint",
                    "Volunteers in settlements of a different culture will not be replaced by custom troops."
                ),
                @default: false,
                type: typeof(bool)
            );

            AddOption(
                section: L.S("mcm_section_recruitment", "Recruitment"),
                name: L.S(
                    "mcm_option_clan_troops_over_kingdom_troops",
                    "Clan Troops Over Kingdom Troops"
                ),
                key: "ClanTroopsOverKingdomTroops",
                hint: L.S(
                    "mcm_option_clan_troops_over_kingdom_troops_hint",
                    "If a fief is both a clan fief and a kingdom fief, clan troops will be prioritized over kingdom troops."
                ),
                @default: true,
                type: typeof(bool)
            );

            AddOption(
                section: L.S("mcm_section_recruitment", "Recruitment"),
                name: L.S(
                    "mcm_option_vassal_lords_recruit_custom_troops",
                    "Vassal Lords Recruit Custom Troops"
                ),
                key: "VassalLordsCanRecruitCustomTroops",
                hint: L.S(
                    "mcm_option_vassal_lords_recruit_custom_troops_hint",
                    "Lords of the player's clan or kingdom can recruit custom troops in their fiefs."
                ),
                @default: true,
                type: typeof(bool)
            );

            AddOption(
                section: L.S("mcm_section_recruitment", "Recruitment"),
                name: L.S(
                    "mcm_option_all_lords_recruit_custom_troops",
                    "All Lords Recruit Custom Troops"
                ),
                key: "AllLordsCanRecruitCustomTroops",
                hint: L.S(
                    "mcm_option_all_lords_recruit_custom_troops_hint",
                    "Any lord can recruit custom troops in the player's fiefs."
                ),
                @default: false,
                type: typeof(bool)
            );

            /* ━━━━━━━━ Editing ━━━━━━━ */

            AddOption(
                section: L.S("mcm_section_troop_customization", "Troop Customization"),
                name: L.S("mcm_option_restrict_editing_to_fiefs", "Restrict Editing To Fiefs"),
                key: "RestrictEditingToFiefs",
                hint: L.S(
                    "mcm_option_restrict_editing_to_fiefs_hint",
                    "Player can only edit troops when in a fief owned by their clan or kingdom (clan retinues can be edited in any settlement)."
                ),
                @default: true,
                type: typeof(bool)
            );

            /* ━━━━━━━ Doctrines ━━━━━━ */

            AddOption(
                section: L.S("mcm_section_doctrines", "Doctrines"),
                name: L.S("mcm_option_enable_doctrines", "Enable Doctrines"),
                key: "EnableDoctrines",
                hint: L.S(
                    "mcm_option_enable_doctrines_hint",
                    "Enable the Doctrines system and its features."
                ),
                @default: true,
                type: typeof(bool)
            );

            /* ━━━━━━━ Equipment ━━━━━━ */

            AddOption(
                section: L.S("mcm_section_equipment", "Equipment"),
                name: L.S("mcm_option_pay_for_equipment", "Pay For Troop Equipment"),
                key: "PayForEquipment",
                hint: L.S(
                    "mcm_option_pay_for_equipment_hint",
                    "Upgrading troop equipment costs money."
                ),
                @default: true,
                type: typeof(bool)
            );

            AddOption(
                section: L.S("mcm_section_equipment", "Equipment"),
                name: L.S("mcm_option_equipment_price_modifier", "Equipment Price Modifier"),
                key: "EquipmentPriceModifier",
                hint: L.S(
                    "mcm_option_equipment_price_modifier_hint",
                    "Modifier for equipment price compared to base game prices."
                ),
                @default: 2.0f,
                type: typeof(float),
                minValue: 0,
                maxValue: 5
            );

            AddOption(
                section: L.S("mcm_section_equipment", "Equipment"),
                name: L.S(
                    "mcm_option_equipment_change_takes_time",
                    "Changing Troop Equipment Takes Time"
                ),
                key: "EquipmentChangeTakesTime",
                hint: L.S(
                    "mcm_option_equipment_change_takes_time_hint",
                    "Changing a troop's equipment takes time."
                ),
                @default: true,
                type: typeof(bool)
            );

            AddOption(
                section: L.S("mcm_section_equipment", "Equipment"),
                name: L.S(
                    "mcm_option_equipment_change_time_modifier",
                    "Equipment Change Time Modifier"
                ),
                key: "EquipmentChangeTimeModifier",
                hint: L.S(
                    "mcm_option_equipment_change_time_modifier_hint",
                    "Modifier for equipment change time."
                ),
                @default: 2,
                type: typeof(int),
                minValue: 1,
                maxValue: 5
            );

            AddOption(
                section: L.S("mcm_section_equipment", "Equipment"),
                name: L.S(
                    "mcm_option_restrict_items_to_town_inventory",
                    "Restrict Items To Town Inventory"
                ),
                key: "RestrictItemsToTownInventory",
                hint: L.S(
                    "mcm_option_restrict_items_to_town_inventory_hint",
                    "Player can only purchase items available in the town inventory."
                ),
                @default: false,
                type: typeof(bool)
            );

            AddOption(
                section: L.S("mcm_section_equipment", "Equipment"),
                name: L.S("mcm_option_allowed_tier_difference", "Allowed Tier Difference"),
                key: "AllowedTierDifference",
                hint: L.S(
                    "mcm_option_allowed_tier_difference_hint",
                    "Maximum allowed tier difference between troops and equipment."
                ),
                @default: 3,
                type: typeof(int),
                minValue: 0,
                maxValue: 6
            );

            AddOption(
                section: L.S("mcm_section_equipment", "Equipment"),
                name: L.S("mcm_option_disallow_mounts_for_tier_1", "Disallow Mounts For Tier 1"),
                key: "NoMountForTier1",
                hint: L.S(
                    "mcm_option_disallow_mounts_for_tier_1_hint",
                    "Tier 1 troops cannot have mounts."
                ),
                @default: true,
                type: typeof(bool)
            );

            /* ━━━━━━━━ Skills ━━━━━━━━ */

            AddOption(
                section: L.S("mcm_section_skills", "Skills"),
                name: L.S("mcm_option_training_takes_time", "Troop Training Takes Time"),
                key: "TrainingTakesTime",
                hint: L.S("mcm_option_training_takes_time_hint", "Troop training takes time."),
                @default: true,
                type: typeof(bool)
            );

            AddOption(
                section: L.S("mcm_section_skills", "Skills"),
                name: L.S("mcm_option_training_time_modifier", "Training Time Modifier"),
                key: "TrainingTimeModifier",
                hint: L.S(
                    "mcm_option_training_time_modifier_hint",
                    "Modifier for troop training time."
                ),
                @default: 2,
                type: typeof(int),
                minValue: 1,
                maxValue: 5
            );

            AddOption(
                section: L.S("mcm_section_skills", "Skills"),
                name: L.S("mcm_option_base_skill_xp_cost", "Base Skill XP Cost"),
                key: "BaseSkillXpCost",
                hint: L.S(
                    "mcm_option_base_skill_xp_cost_hint",
                    "Base XP cost for increasing a skill."
                ),
                @default: 100,
                type: typeof(int),
                minValue: 0,
                maxValue: 1000
            );

            AddOption(
                section: L.S("mcm_section_skills", "Skills"),
                name: L.S("mcm_option_skill_xp_cost_per_point", "Skill XP Cost Per Point"),
                key: "SkillXpCostPerPoint",
                hint: L.S(
                    "mcm_option_skill_xp_cost_per_point_hint",
                    "Scalable XP cost for each point of skill increase."
                ),
                @default: 1,
                type: typeof(int),
                minValue: 0,
                maxValue: 10
            );

            AddOption(
                section: L.S("mcm_section_skills", "Skills"),
                name: L.S("mcm_option_shared_xp_pool", "Shared XP Pool"),
                key: "SharedXpPool",
                hint: L.S("mcm_option_shared_xp_pool_hint", "All troops share the same XP pool."),
                @default: false,
                type: typeof(bool)
            );

            /* ━━━━━━━━ Unlocks ━━━━━━━ */

            AddOption(
                section: L.S("mcm_section_unlocks", "Unlocks"),
                name: L.S("mcm_option_unlock_from_kills", "Unlock From Kills"),
                key: "UnlockFromKills",
                hint: L.S(
                    "mcm_option_unlock_from_kills_hint",
                    "Unlock equipment by defeating enemies wearing it."
                ),
                @default: true,
                type: typeof(bool)
            );

            AddOption(
                section: L.S("mcm_section_unlocks", "Unlocks"),
                name: L.S("mcm_option_required_kills_for_unlock", "Required Kills For Unlock"),
                key: "KillsForUnlock",
                hint: L.S(
                    "mcm_option_required_kills_for_unlock_hint",
                    "How many enemies wearing an item must be defeated to unlock it."
                ),
                @default: 100,
                type: typeof(int),
                minValue: 1,
                maxValue: 1000
            );

            AddOption(
                section: L.S("mcm_section_unlocks", "Unlocks"),
                name: L.S("mcm_option_own_culture_unlock_bonuses", "Own Culture Unlock Bonuses"),
                key: "OwnCultureUnlockBonuses",
                hint: L.S(
                    "mcm_option_own_culture_unlock_bonuses_hint",
                    "Whether kills also unlock items from the custom troop's culture."
                ),
                @default: true,
                type: typeof(bool)
            );

            AddOption(
                section: L.S("mcm_section_unlocks", "Unlocks"),
                name: L.S("mcm_option_unlock_from_culture", "Unlock From Culture"),
                key: "UnlockFromCulture",
                hint: L.S(
                    "mcm_option_unlock_from_culture_hint",
                    "Player culture and player-led kingdom culture equipment is always available."
                ),
                @default: false,
                type: typeof(bool)
            );

            AddOption(
                section: L.S("mcm_section_unlocks", "Unlocks"),
                name: L.S("mcm_option_all_equipment_unlocked", "All Equipment Unlocked"),
                key: "AllEquipmentUnlocked",
                hint: L.S(
                    "mcm_option_all_equipment_unlocked_hint",
                    "All equipment unlocked on game start."
                ),
                @default: false,
                type: typeof(bool)
            );

            /* ━━━━━━━━━ Debug ━━━━━━━━ */

            AddOption(
                section: L.S("mcm_section_debug", "Debug"),
                name: L.S("mcm_option_debug_mode", "Debug Mode"),
                key: "DebugMode",
                hint: L.S(
                    "mcm_option_debug_mode_hint",
                    "Outputs many more logs (may impact performance)."
                ),
                @default: false,
                type: typeof(bool)
            );

            /* ━━━━━ Skill Caps ━━━━━ */

            AddOption(
                section: L.S("mcm_section_skill_caps", "Skill Caps"),
                key: "RetinueSkillCapBonus",
                name: L.S("mcm_option_retinue_skill_cap_bonus", "Retinue Skill Cap Bonus"),
                hint: L.S(
                    "mcm_option_retinue_skill_cap_bonus_hint",
                    "Additional skill cap for retinue troops."
                ),
                @default: 5,
                type: typeof(int),
                minValue: 0,
                maxValue: 50
            );

            AddOption(
                section: L.S("mcm_section_skill_caps", "Skill Caps"),
                key: "SkillCapTier0",
                name: L.T("mcm_option_skill_cap", "Tier {TIER} Cap")
                    .SetTextVariable("TIER", "0")
                    .ToString(),
                hint: L.T(
                        "mcm_option_skill_cap_hint",
                        "The maximum skill level for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "0")
                    .ToString(),
                @default: 20,
                type: typeof(int),
                minValue: 20,
                maxValue: 360
            );
            AddOption(
                section: L.S("mcm_section_skill_caps", "Skill Caps"),
                key: "SkillCapTier1",
                name: L.T("mcm_option_skill_cap", "Tier {TIER} Cap")
                    .SetTextVariable("TIER", "1")
                    .ToString(),
                hint: L.T(
                        "mcm_option_skill_cap_hint",
                        "The maximum skill level for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "1")
                    .ToString(),
                @default: 20,
                type: typeof(int),
                minValue: 20,
                maxValue: 360
            );
            AddOption(
                section: L.S("mcm_section_skill_caps", "Skill Caps"),
                key: "SkillCapTier2",
                name: L.T("mcm_option_skill_cap", "Tier {TIER} Cap")
                    .SetTextVariable("TIER", "2")
                    .ToString(),
                hint: L.T(
                        "mcm_option_skill_cap_hint",
                        "The maximum skill level for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "2")
                    .ToString(),
                @default: 50,
                type: typeof(int),
                minValue: 20,
                maxValue: 360
            );
            AddOption(
                section: L.S("mcm_section_skill_caps", "Skill Caps"),
                key: "SkillCapTier3",
                name: L.T("mcm_option_skill_cap", "Tier {TIER} Cap")
                    .SetTextVariable("TIER", "3")
                    .ToString(),
                hint: L.T(
                        "mcm_option_skill_cap_hint",
                        "The maximum skill level for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "3")
                    .ToString(),
                @default: 80,
                type: typeof(int),
                minValue: 20,
                maxValue: 360
            );
            AddOption(
                section: L.S("mcm_section_skill_caps", "Skill Caps"),
                key: "SkillCapTier4",
                name: L.T("mcm_option_skill_cap", "Tier {TIER} Cap")
                    .SetTextVariable("TIER", "4")
                    .ToString(),
                hint: L.T(
                        "mcm_option_skill_cap_hint",
                        "The maximum skill level for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "4")
                    .ToString(),
                @default: 120,
                type: typeof(int),
                minValue: 20,
                maxValue: 360
            );
            AddOption(
                section: L.S("mcm_section_skill_caps", "Skill Caps"),
                key: "SkillCapTier5",
                name: L.T("mcm_option_skill_cap", "Tier {TIER} Cap")
                    .SetTextVariable("TIER", "5")
                    .ToString(),
                hint: L.T(
                        "mcm_option_skill_cap_hint",
                        "The maximum skill level for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "5")
                    .ToString(),
                @default: 160,
                type: typeof(int),
                minValue: 20,
                maxValue: 360
            );
            AddOption(
                section: L.S("mcm_section_skill_caps", "Skill Caps"),
                key: "SkillCapTier6",
                name: L.T("mcm_option_skill_cap", "Tier {TIER} Cap")
                    .SetTextVariable("TIER", "6")
                    .ToString(),
                hint: L.T(
                        "mcm_option_skill_cap_hint",
                        "The maximum skill level for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "6")
                    .ToString(),
                @default: 260,
                type: typeof(int),
                minValue: 20,
                maxValue: 360
            );
            AddOption(
                section: L.S("mcm_section_skill_caps", "Skill Caps"),
                key: "SkillCapTier7Plus",
                name: L.T("mcm_option_skill_cap", "Tier {TIER} Cap")
                    .SetTextVariable("TIER", "7+")
                    .ToString(),
                hint: L.T(
                        "mcm_option_skill_cap_hint",
                        "The maximum skill level for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "7+")
                    .ToString(),
                @default: 360,
                type: typeof(int),
                minValue: 20,
                maxValue: 360
            );

            /* ━━━━━ Skill Totals ━━━━━ */

            AddOption(
                section: L.S("mcm_section_skill_totals", "Skill Totals"),
                key: "RetinueSkillTotalBonus",
                name: L.S("mcm_option_retinue_skill_total_bonus", "Retinue Skill Total Bonus"),
                hint: L.S(
                    "mcm_option_retinue_skill_total_bonus_hint",
                    "Additional skill total for retinue troops."
                ),
                @default: 10,
                type: typeof(int),
                minValue: 0,
                maxValue: 100
            );

            AddOption(
                section: L.S("mcm_section_skill_totals", "Skill Totals"),
                key: "SkillTotalTier0",
                name: L.T("mcm_option_skill_total", "Tier {TIER} Skill Total")
                    .SetTextVariable("TIER", "0")
                    .ToString(),
                hint: L.T(
                        "mcm_option_skill_total_hint",
                        "The total available skill points for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "0")
                    .ToString(),
                @default: 90,
                type: typeof(int),
                minValue: 90,
                maxValue: 1600
            );
            AddOption(
                section: L.S("mcm_section_skill_totals", "Skill Totals"),
                key: "SkillTotalTier1",
                name: L.T("mcm_option_skill_total", "Tier {TIER} Skill Total")
                    .SetTextVariable("TIER", "1")
                    .ToString(),
                hint: L.T(
                        "mcm_option_skill_total_hint",
                        "The total available skill points for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "1")
                    .ToString(),
                @default: 90,
                type: typeof(int),
                minValue: 90,
                maxValue: 1600
            );
            AddOption(
                section: L.S("mcm_section_skill_totals", "Skill Totals"),
                key: "SkillTotalTier2",
                name: L.T("mcm_option_skill_total", "Tier {TIER} Skill Total")
                    .SetTextVariable("TIER", "2")
                    .ToString(),
                hint: L.T(
                        "mcm_option_skill_total_hint",
                        "The total available skill points for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "2")
                    .ToString(),
                @default: 210,
                type: typeof(int),
                minValue: 90,
                maxValue: 1600
            );
            AddOption(
                section: L.S("mcm_section_skill_totals", "Skill Totals"),
                key: "SkillTotalTier3",
                name: L.T("mcm_option_skill_total", "Tier {TIER} Skill Total")
                    .SetTextVariable("TIER", "3")
                    .ToString(),
                hint: L.T(
                        "mcm_option_skill_total_hint",
                        "The total available skill points for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "3")
                    .ToString(),
                @default: 360,
                type: typeof(int),
                minValue: 90,
                maxValue: 1600
            );
            AddOption(
                section: L.S("mcm_section_skill_totals", "Skill Totals"),
                key: "SkillTotalTier4",
                name: L.T("mcm_option_skill_total", "Tier {TIER} Skill Total")
                    .SetTextVariable("TIER", "4")
                    .ToString(),
                hint: L.T(
                        "mcm_option_skill_total_hint",
                        "The total available skill points for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "4")
                    .ToString(),
                @default: 535,
                type: typeof(int),
                minValue: 90,
                maxValue: 1600
            );
            AddOption(
                section: L.S("mcm_section_skill_totals", "Skill Totals"),
                key: "SkillTotalTier5",
                name: L.T("mcm_option_skill_total", "Tier {TIER} Skill Total")
                    .SetTextVariable("TIER", "5")
                    .ToString(),
                hint: L.T(
                        "mcm_option_skill_total_hint",
                        "The total available skill points for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "5")
                    .ToString(),
                @default: 710,
                type: typeof(int),
                minValue: 90,
                maxValue: 1600
            );
            AddOption(
                section: L.S("mcm_section_skill_totals", "Skill Totals"),
                key: "SkillTotalTier6",
                name: L.T("mcm_option_skill_total", "Tier {TIER} Skill Total")
                    .SetTextVariable("TIER", "6")
                    .ToString(),
                hint: L.T(
                        "mcm_option_skill_total_hint",
                        "The total available skill points for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "6")
                    .ToString(),
                @default: 915,
                type: typeof(int),
                minValue: 90,
                maxValue: 1600
            );
            AddOption(
                section: L.S("mcm_section_skill_totals", "Skill Totals"),
                key: "SkillTotalTier7Plus",
                name: L.T("mcm_option_skill_total", "Tier {TIER} Skill Total")
                    .SetTextVariable("TIER", "7+")
                    .ToString(),
                hint: L.T(
                        "mcm_option_skill_total_hint",
                        "The total available skill points for tier {TIER} troops."
                    )
                    .SetTextVariable("TIER", "7+")
                    .ToString(),
                @default: 1600,
                type: typeof(int),
                minValue: 90,
                maxValue: 1600
            );

            // Initial log dump
            try
            {
                Log.Info("Config initialized (defaults):");
                foreach (var o in _options)
                    Log.Info($"  [{o.Section}] {o.Key} = {FormatValue(o.Value)}");
            }
            catch { }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static IReadOnlyList<ConfigOption> Options => _options;

        public static T GetOption<T>(string key, T fallback = default)
        {
            if (!_byKey.TryGetValue(key, out var opt))
                return fallback;
            try
            {
                if (opt.Value is T t)
                    return t;
                var converted = ConvertTo(opt.Value, typeof(T));
                return converted is T tt ? tt : fallback;
            }
            catch
            {
                return fallback;
            }
        }

        public static bool SetOption<T>(string key, T value)
        {
            if (!_byKey.TryGetValue(key, out var opt))
                return false;
            try
            {
                opt.Value = ConvertTo(value, opt.Type);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns (Id, Default) pairs for preset generation.
        /// </summary>
        public static IEnumerable<(string Id, object Default)> Defaults()
        {
            foreach (var opt in _options)
                yield return (opt.Key, opt.Default);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    MCM Registration                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const string McmId = "Retinues.Settings";
        private const string McmDisplay = "Retinues";
        private const string McmFolder = "Retinues";
        private const string McmFormat = "xml";

        /// <summary>
        /// Build and register the MCM UI.
        /// </summary>
        public static bool RegisterWithMCM()
        {
            try
            {
                Log.Info("Registering MCM menu...");
                var create = BaseSettingsBuilder.Create(McmId, McmDisplay);
                if (create is null)
                    return false;

                var builder = create.SetFolderName(McmFolder).SetFormat(McmFormat);
                if (builder is null)
                    return false;

                // Pin an Import/Export utility group at the very top.
                builder.CreateGroup(
                    L.S("mcm_section_import_export", "Import & Export"),
                    group =>
                    {
                        group.SetGroupOrder(0);
                        int order = 0;

                        // Export filename entry — editable text bound to _exportName
                        group.AddText(
                            "ExportFileName",
                            L.S("mcm_ie_export_name", "File name"),
                            new ProxyRef<string>(
                                () => _exportName,
                                v =>
                                    _exportName = string.IsNullOrWhiteSpace(v)
                                        ? SuggestDefaultExportName()
                                        : v
                            ),
                            b =>
                                b.SetOrder(order++)
                                    .SetHintText(
                                        L.S("mcm_ie_export_hint", "Example: troops_myCampaignRun")
                                    )
                        );

                        // Export button — action via ProxyRef<Action>
                        group.AddButton(
                            "ExportButton",
                            L.S("mcm_ie_export_btn_text", "Export Troops to XML"),
                            new ProxyRef<Action>(
                                () =>
                                {
                                    return delegate
                                    {
                                        if (!InCampaign())
                                        {
                                            Log.Message(
                                                "Export aborted: not in a running campaign. Load a save first."
                                            );
                                            return;
                                        }

                                        var used = TroopImportExport.ExportAllToXml(_exportName);
                                        if (!string.IsNullOrWhiteSpace(used))
                                            Log.Info($"Exported custom troops to: {used}");
                                        else
                                            Log.Warn("Export failed. See log for details.");

                                        RefreshImportDropdown(); // newly exported file becomes selectable for import
                                    };
                                },
                                _ => { }
                            ),
                            L.S("mcm_ie_export_btn", "Export"),
                            b => b.SetOrder(order++).SetRequireRestart(false)
                        );

                        // Import dropdown — bound to _importDropdown
                        group.AddDropdown(
                            "ImportFileDropdown",
                            L.S("mcm_ie_import_dropdown", "Available files"),
                            0,
                            new ProxyRef<Dropdown<string>>(
                                () => _importDropdown,
                                v => _importDropdown = v ?? _importDropdown
                            ),
                            b => b.SetOrder(order++)
                        );
                        group.AddButton(
                            "ImportButton",
                            L.S("mcm_ie_import_btn_text", "Import Troops from XML"),
                            new ProxyRef<Action>(
                                () =>
                                {
                                    return () =>
                                    {
                                        if (!InCampaign())
                                        {
                                            Log.Message(
                                                "Import aborted: not in a running campaign. Load a save first."
                                            );
                                            return;
                                        }

                                        var choice = _importDropdown?.SelectedValue;
                                        if (
                                            string.IsNullOrWhiteSpace(choice)
                                            || choice.StartsWith("<")
                                        )
                                        {
                                            Log.Message("No valid file selected for import.");
                                            return;
                                        }

                                        ConfirmTroopReplace(
                                            L.S("mcm_ie_import_confirm_title", "Import Troops"),
                                            L.S(
                                                "mcm_ie_import_confirm_body",
                                                "Importing new troop definitions will replace the existing ones. Are you sure?"
                                            ),
                                            () =>
                                            {
                                                try
                                                {
                                                    // 1) Safety backup
                                                    var backupName =
                                                        "backup_" + SuggestDefaultExportName();
                                                    var backupPath =
                                                        TroopImportExport.ExportAllToXml(
                                                            backupName
                                                        );
                                                    if (!string.IsNullOrWhiteSpace(backupPath))
                                                        Log.Info($"Backup created: {backupPath}");
                                                    else
                                                        Log.Warn(
                                                            "Backup export failed (continuing with import)."
                                                        );

                                                    // 2) Import
                                                    var count = TroopImportExport.ImportFromXml(
                                                        choice
                                                    );
                                                    Log.Message(
                                                        count > 0
                                                            ? $"Imported {count} root troop definitions from '{choice}'."
                                                            : $"No troops were imported from '{choice}'."
                                                    );
                                                }
                                                catch (Exception ex)
                                                {
                                                    Log.Error($"Import failed: {ex}");
                                                }
                                            }
                                        );
                                    };
                                },
                                _ => { }
                            ),
                            L.S("mcm_ie_import_btn", "Import"),
                            b => b.SetOrder(order++).SetRequireRestart(false)
                        );
                    }
                );

                if (_options.Count == 0)
                    return false;

                foreach (
                    var section in _options.GroupBy(o =>
                        string.IsNullOrWhiteSpace(o.Section) ? "General" : o.Section
                    )
                )
                {
                    var sectionName = section.Key;

                    builder.CreateGroup(
                        sectionName,
                        group =>
                        {
                            int order = 0;
                            foreach (var opt in section)
                            {
                                var id = string.IsNullOrWhiteSpace(opt.Key)
                                    ? Guid.NewGuid().ToString("N")
                                    : opt.Key;
                                var name = string.IsNullOrWhiteSpace(opt.Name) ? id : opt.Name;
                                var hint = opt.Hint ?? string.Empty;
                                var def = opt.Default;
                                var min = opt.MinValue;
                                var max = opt.MaxValue;

                                try
                                {
                                    if (opt.Type == typeof(bool))
                                    {
                                        group.AddBool(
                                            id,
                                            name,
                                            new ProxyRef<bool>(
                                                () => GetOption(id, def is bool b && b),
                                                v => SetOption(id, v)
                                            ),
                                            b =>
                                                b.SetOrder(order++)
                                                    .SetHintText(hint)
                                                    .SetRequireRestart(false)
                                        );
                                    }
                                    else if (opt.Type == typeof(int))
                                    {
                                        group.AddInteger(
                                            id,
                                            name,
                                            min,
                                            max,
                                            new ProxyRef<int>(
                                                () => GetOption(id, def is int i ? i : 0),
                                                v => SetOption(id, v)
                                            ),
                                            b =>
                                                b.SetOrder(order++)
                                                    .SetHintText(hint)
                                                    .SetRequireRestart(false)
                                        );
                                    }
                                    else if (opt.Type == typeof(float))
                                    {
                                        group.AddFloatingInteger(
                                            id,
                                            name,
                                            minValue: min,
                                            maxValue: max,
                                            new ProxyRef<float>(
                                                () => GetOption(id, def is float f ? f : 0f),
                                                v => SetOption(id, v)
                                            ),
                                            b =>
                                                b.SetOrder(order++)
                                                    .SetHintText(hint)
                                                    .SetRequireRestart(false)
                                        );
                                    }
                                    else // string and others
                                    {
                                        group.AddText(
                                            id,
                                            name,
                                            new ProxyRef<string>(
                                                () => GetOption(id, def as string ?? string.Empty),
                                                v => SetOption(id, v)
                                            ),
                                            b =>
                                                b.SetOrder(order++)
                                                    .SetHintText(hint)
                                                    .SetRequireRestart(false)
                                        );
                                    }
                                }
                                catch
                                {
                                    // skip bad entry
                                }
                            }
                        }
                    );
                }

                // Default preset mirrors current Defaults()
                builder.CreatePreset(
                    BaseSettings.DefaultPresetId,
                    BaseSettings.DefaultPresetName,
                    p =>
                    {
                        foreach (var (id, d) in Defaults())
                        {
                            if (d is float f)
                                p.SetPropertyValue(
                                    id,
                                    float.Parse(f.ToString("0.00", CultureInfo.InvariantCulture))
                                );
                            else if (d is double dd)
                                p.SetPropertyValue(
                                    id,
                                    float.Parse(dd.ToString("0.00", CultureInfo.InvariantCulture))
                                );
                            else
                                p.SetPropertyValue(id, d);
                        }
                    }
                );

                var settings = builder.BuildAsGlobal();
                if (settings is null)
                    return false;

                settings.Register();
                Log.Info("MCM registered: Retinues settings are available.");
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void AddOption(
            string section,
            string name,
            string key,
            string hint,
            object @default,
            Type type,
            int minValue = 0,
            int maxValue = 1000
        )
        {
            var opt = new ConfigOption
            {
                Section = section ?? L.S("mcm_section_general", "General"),
                Name = name,
                Key = key,
                Hint = hint,
                Default = @default,
                Type = type,
                Value = @default,
                MinValue = minValue,
                MaxValue = maxValue,
            };
            _options.Add(opt);
            _byKey[key] = opt;
        }

        private static object ConvertTo(object value, Type targetType)
        {
            if (value == null)
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            var vt = value.GetType();
            if (targetType.IsAssignableFrom(vt))
                return value;

            if (targetType == typeof(bool))
            {
                if (value is string s)
                    return ParseBool(s, false);
                if (value is int i)
                    return i != 0;
            }
            if (targetType == typeof(int))
            {
                if (
                    value is string s
                    && int.TryParse(
                        s,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var ii
                    )
                )
                    return ii;
                if (value is bool b)
                    return b ? 1 : 0;
                if (value is float f)
                    return (int)f;
                if (value is double d)
                    return (int)d;
            }
            if (targetType == typeof(float))
            {
                if (value is float f)
                    return f;
                if (value is double d)
                    return (float)d;
                if (value is int i)
                    return i;
                if (
                    value is string s
                    && float.TryParse(
                        s,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var ff
                    )
                )
                    return ff;
            }
            if (targetType == typeof(double))
            {
                if (value is double d)
                    return d;
                if (value is float f)
                    return (double)f;
                if (value is int i)
                    return (double)i;
                if (
                    value is string s
                    && double.TryParse(
                        s,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var dd
                    )
                )
                    return dd;
            }
            if (targetType == typeof(string))
                return value.ToString();

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        private static bool ParseBool(string value, bool fallback)
        {
            if (bool.TryParse(value, out var b))
                return b;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                return i != 0;
            return fallback;
        }

        private static string FormatValue(object value) =>
            value switch
            {
                bool b => b ? "true" : "false",
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                null => "",
                _ => value.ToString(),
            };
    }
}
