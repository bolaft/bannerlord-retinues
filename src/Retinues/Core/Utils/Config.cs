using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Retinues.Core.Utils
{
    public static class Config
    {
        // =========================
        // Model & storage
        // =========================
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
        private static readonly Dictionary<string, ConfigOption> _byKey = new(StringComparer.OrdinalIgnoreCase);

        private static string ConfigFile
        {
            get
            {
                var asmDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
                var moduleRoot = Directory.GetParent(asmDir)!.Parent!.FullName;
                return Path.Combine(moduleRoot, "config.ini");
            }
        }

        static Config()
        {
            // Retinues

            AddOption(
                section: "Retinues",
                name: "Max Elite Retinue Ratio",
                key: "MaxEliteRetinueRatio",
                hint: "Maximum proportion of elite retinue troops in player party.",
                @default: 0.1,
                type: typeof(float),
                minValue: 0, maxValue: 1);

            AddOption(
                section: "Retinues",
                name: "Max Basic Retinue Ratio",
                key: "MaxBasicRetinueRatio",
                hint: "Maximum proportion of basic retinue troops in player party.",
                @default: 0.2,
                type: typeof(float),
                minValue: 0, maxValue: 1);

            AddOption(
                section: "Retinues",
                name: "Retinue Conversion Cost Per Tier",
                key: "RetinueConversionCostPerTier",
                hint: "Conversion cost for retinue troops per tier.",
                @default: 50,
                type: typeof(int),
                minValue: 0, maxValue: 200);

            AddOption(
                section: "Retinues",
                name: "Retinue Rank Up Cost Per Tier",
                key: "RetinueRankUpCostPerTier",
                hint: "Rank up cost for retinue troops per tier.",
                @default: 1000,
                type: typeof(int),
                minValue: 0, maxValue: 5000);

            // Recruitment

            AddOption(
                section: "Recruitment",
                name: "Recruit Clan Troops Anywhere",
                key: "RecruitAnywhere",
                hint: "Player can recruit clan troops in any settlement.",
                @default: false,
                type: typeof(bool));

            // Equipment

            AddOption(
                section: "Equipment",
                name: "Pay For Troop Equipment",
                key: "PayForEquipment",
                hint: "Upgrading troop equipment costs money.",
                @default: true,
                type: typeof(bool));

            AddOption(
                section: "Equipment",
                name: "Allowed Tier Difference",
                key: "AllowedTierDifference",
                hint: "Maximum allowed tier difference between troops and equipment.",
                @default: 3,
                type: typeof(int),
                minValue: 0, maxValue: 6);

            AddOption(
                section: "Equipment",
                name: "Disallow Mounts For Tier 1",
                key: "NoMountForTier1",
                hint: "Tier 1 troops cannot have mounts.",
                @default: true,
                type: typeof(bool));

            // Unlocks

            AddOption(
                section: "Unlocks",
                name: "Unlock From Kills",
                key: "UnlockFromKills",
                hint: "Unlock equipment by defeating enemies wearing it.",
                @default: true,
                type: typeof(bool));

            AddOption(
                section: "Unlocks",
                name: "Required Kills For Unlock",
                key: "KillsForUnlock",
                hint: "How many enemies wearing an item must be defeated to unlock it.",
                @default: 100,
                type: typeof(int),
                minValue: 1, maxValue: 1000);

            AddOption(
                section: "Unlocks",
                name: "Unlock From Culture",
                key: "UnlockFromCulture",
                hint: "Player culture and player-led kingdom culture equipment is always available.",
                @default: false,
                type: typeof(bool));

            AddOption(
                section: "Unlocks",
                name: "All Equipment Unlocked",
                key: "AllEquipmentUnlocked",
                hint: "All equipment unlocked on game start.",
                @default: false,
                type: typeof(bool));

            Load();

            try
            {
                Log.Debug("Config loaded:");
                foreach (var o in _options)
                    Log.Debug($"  [{o.Section}] {o.Key} = {FormatValue(o.Value)}");
            }
            catch { }
        }

        private static void AddOption(string section, string name, string key, string hint, object @default, Type type, int minValue = 0, int maxValue = 1000)
        {
            var opt = new ConfigOption
            {
                Section = section ?? "General",
                Name = name,
                Key = key,
                Hint = hint,
                Default = @default,
                Type = type,
                Value = @default,
                MinValue = minValue,
                MaxValue = maxValue
            };
            _options.Add(opt);
            _byKey[key] = opt;
        }

        // =========================
        // Public API
        // =========================

        public static IReadOnlyList<ConfigOption> Options => _options;

        public static T GetOption<T>(string key, T fallback = default)
        {
            if (!_byKey.TryGetValue(key, out var opt)) return fallback;
            try
            {
                if (opt.Value is T t) return t;
                var converted = ConvertTo(opt.Value, typeof(T));
                return converted is T tt ? tt : fallback;
            }
            catch { return fallback; }
        }

        public static bool SetOption<T>(string key, T value, bool save = false)
        {
            if (!_byKey.TryGetValue(key, out var opt)) return false;
            try
            {
                var converted = ConvertTo(value, opt.Type);
                opt.Value = converted;
                if (save) Save();
                return true;
            }
            catch { return false; }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(ConfigFile))
                    return;

                foreach (var line in File.ReadAllLines(ConfigFile))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || !trimmed.Contains("="))
                        continue;

                    var parts = trimmed.Split(new[] { '=' }, 2);
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

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigFile)!);
                using var w = new StreamWriter(ConfigFile, false);
                w.WriteLine("# Custom Clan Troops Configuration");

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

        // =========================
        // Helpers
        // =========================

        private static object ConvertTo(object value, Type targetType)
        {
            if (value == null) return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            var vt = value.GetType();
            if (targetType.IsAssignableFrom(vt)) return value;

            if (targetType == typeof(bool))
            {
                if (value is string s) return ParseBool(s, false);
                if (value is int i) return i != 0;
            }

            if (targetType == typeof(int))
            {
                if (value is string s && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ii)) return ii;
                if (value is bool b) return b ? 1 : 0;
            }

            if (targetType == typeof(string))
                return value.ToString();

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        private static object ParseFromString(string raw, Type type, object fallback)
        {
            if (type == typeof(bool)) return ParseBool(raw, (bool)fallback);
            if (type == typeof(int)) return ParseInt(raw, (int)fallback);
            if (type == typeof(string)) return raw;
            return fallback;
        }

        private static bool ParseBool(string value, bool fallback)
        {
            if (bool.TryParse(value, out var b)) return b;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) return i != 0;
            return fallback;
        }

        private static int ParseInt(string value, int fallback)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) return i;
            return fallback;
        }

        private static string FormatValue(object value)
        {
            return value switch
            {
                bool b => b ? "true" : "false",
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                null => "",
                _ => value.ToString()
            };
        }
    }
}
