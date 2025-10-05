using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Utils
{
    /// <summary>
    /// Central configuration manager for mod options.
    /// Handles loading, saving, and type conversion for all runtime and MCM options.
    /// </summary>
    [SafeClass]
    public static class Config
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Option Model                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Represents a single configuration option, including metadata for UI and serialization.
        /// </summary>
        public sealed class ConfigOption
        {
            public string Section { get; set; } // e.g., "Recruitment"
            public string Name { get; set; } // Label
            public string Key { get; set; } // INI key
            public string Hint { get; set; } // Tooltip
            public Type Type { get; set; } // typeof(bool/int/string)
            public object Default { get; set; } // Default value
            public object Value { get; set; } // Current value
            public int MinValue { get; set; } // For int type
            public int MaxValue { get; set; } // For int type
        }

        private static readonly List<ConfigOption> _options = [];

        private static readonly Dictionary<string, ConfigOption> _byKey = new(
            StringComparer.OrdinalIgnoreCase
        );

        private static string ConfigFile
        {
            get
            {
                var asmDir = Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location
                )!;
                var moduleRoot = Directory.GetParent(asmDir)!.Parent!.FullName;
                return Path.Combine(moduleRoot, "config.ini");
            }
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
                @default: 0.1,
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
                @default: 0.2,
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
                @default: false,
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

            Load();

            try
            {
                Log.Info("Config loaded:");
                foreach (var o in _options)
                    Log.Info($"  [{o.Section}] {o.Key} = {FormatValue(o.Value)}");
            }
            catch { }

            /* ━━━━━━ Skill Caps ━━━━━━ */

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
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// List of all config options, grouped by section for UI and serialization.
        /// </summary>
        public static IReadOnlyList<ConfigOption> Options => _options;

        /// <summary>
        /// Gets the value of a config option by key, with type conversion and fallback.
        /// Returns the fallback if the key is missing or conversion fails.
        /// </summary>
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

        /// <summary>
        /// Sets the value of a config option by key, with type conversion.
        /// Optionally saves the config file after setting.
        /// Returns true if successful.
        /// </summary>
        public static bool SetOption<T>(string key, T value, bool save = false)
        {
            if (!_byKey.TryGetValue(key, out var opt))
                return false;
            try
            {
                var converted = ConvertTo(value, opt.Type);
                opt.Value = converted;
                if (save)
                    Save();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Loads config values from the config.ini file, applying type conversion and falling back to defaults on error.
        /// </summary>
        public static void Load()
        {
            try
            {
                if (!File.Exists(ConfigFile))
                    return;

                foreach (var line in File.ReadAllLines(ConfigFile))
                {
                    var trimmed = line.Trim();
                    if (
                        string.IsNullOrEmpty(trimmed)
                        || trimmed.StartsWith("#")
                        || !trimmed.Contains("=")
                    )
                        continue;

                    var parts = trimmed.Split(['='], 2);
                    var key = parts[0].Trim();
                    var raw = parts[1].Trim();

                    if (!_byKey.TryGetValue(key, out var opt))
                        continue;

                    try
                    {
                        object parsed = ParseFromString(raw, opt.Type, opt.Default);
                        opt.Value = parsed;
                    }
                    catch
                    {
                        opt.Value = opt.Default;
                    }
                }
            }
            catch
            {
                // keep defaults on failure
            }
        }

        /// <summary>
        /// Saves all config options to config.ini, grouped by section and with hints as comments.
        /// </summary>
        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigFile)!);
                using var w = new StreamWriter(ConfigFile, false);
                w.WriteLine("# Retinues Configuration");

                foreach (var grp in _options.GroupBy(o => o.Section))
                {
                    foreach (var o in grp)
                    {
                        w.WriteLine();
                        var defaultVal = FormatValue(o.Default);
                        var hint = o.Hint?.Replace("\n", " ") ?? "";
                        w.WriteLine($"# {hint} Default: {defaultVal}.");
                        w.WriteLine($"{o.Key}={FormatValue(o.Value)}");
                    }
                }
            }
            catch
            {
                // Non-fatal
            }
        }

        /// <summary>
        /// Enumerates all config option keys and their default values.
        /// Used for MCM preset generation.
        /// </summary>
        public static IEnumerable<(string Id, object Default)> Defaults()
        {
            foreach (var opt in _options)
                yield return (opt.Key, opt.Default);
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

        /* ━━━━━━ Conversion ━━━━━━ */

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
            }

            if (targetType == typeof(float))
            {
                if (value is float f)
                    return f;
                if (value is double d)
                    return (float)d;
                if (value is int i)
                    return (float)i;
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

        /* ━━━━━━━━ Parsers ━━━━━━━ */

        private static object ParseFromString(string raw, Type type, object fallback)
        {
            if (type == typeof(bool))
                return ParseBool(raw, (bool)fallback);
            if (type == typeof(int))
                return ParseInt(raw, (int)fallback);
            if (type == typeof(float))
                return ParseFloat(raw, (float)fallback);
            if (type == typeof(double))
                return ParseDouble(raw, (double)fallback);
            if (type == typeof(string))
                return raw;
            return fallback;
        }

        private static bool ParseBool(string value, bool fallback)
        {
            if (bool.TryParse(value, out var b))
                return b;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                return i != 0;
            return fallback;
        }

        private static int ParseInt(string value, int fallback)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                return i;
            return fallback;
        }

        private static float ParseFloat(string value, float fallback)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                return f;
            return fallback;
        }

        private static double ParseDouble(string value, double fallback)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return d;
            return fallback;
        }

        /* ━━━━━━━━ Format ━━━━━━━━ */

        private static string FormatValue(object value)
        {
            return value switch
            {
                bool b => b ? "true" : "false",
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                null => "",
                _ => value.ToString(),
            };
        }
    }
}
