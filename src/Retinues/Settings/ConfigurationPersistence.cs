using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Retinues.Utilities;

namespace Retinues.Settings
{
    /// <summary>
    /// Loads and saves configuration values to a simple INI file.
    /// </summary>
    internal static class ConfigurationPersistence
    {
        private const string FileName = "settings.ini";
        private static readonly object _lock = new();

        internal static string ConfigPath => FileSystem.GetPathInRetinuesDocuments(FileName);

        /// <summary>
        /// Loads settings from disk or initializes a new file with defaults.
        /// </summary>
        internal static void LoadOrInit(
            IReadOnlyList<Section> sections,
            IReadOnlyList<IOption> options
        )
        {
            lock (_lock)
            {
                try
                {
                    var path = ConfigPath;
                    if (!File.Exists(path))
                    {
                        SaveAll(sections, options);
                        return;
                    }

                    var map = ParseIni(path);
                    ApplyValues(map, options);

                    // Re-write to normalize formatting and add missing keys.
                    SaveAll(sections, options);
                }
                catch
                {
                    // Never crash the game on config IO.
                }
            }
        }

        /// <summary>
        /// Writes the full configuration file to disk.
        /// </summary>
        internal static void SaveAll(
            IReadOnlyList<Section> sections,
            IReadOnlyList<IOption> options
        )
        {
            lock (_lock)
            {
                try
                {
                    var path = ConfigPath;

                    var sb = new StringBuilder(8 * 1024);
                    sb.AppendLine("# Retinues settings");
                    sb.AppendLine("# This file is auto-saved when settings change.");
                    sb.AppendLine("# You can edit it manually; values are read on startup.");
                    sb.AppendLine();

                    // Group options by section name (as displayed).
                    var bySection = new Dictionary<string, List<IOption>>(
                        StringComparer.OrdinalIgnoreCase
                    );
                    for (int i = 0; i < options.Count; i++)
                    {
                        var opt = options[i];
                        if (opt == null)
                            continue;

                        var section = opt.Section ?? string.Empty;
                        if (!bySection.TryGetValue(section, out var list))
                        {
                            list = [];
                            bySection[section] = list;
                        }

                        list.Add(opt);
                    }

                    // Use the discovered sections order.
                    for (int s = 0; s < sections.Count; s++)
                    {
                        var section = sections[s];
                        var sectionName = section?.Name ?? string.Empty;

                        sb.Append('[').Append(sectionName).AppendLine("]");

                        var sectionDescription = section?.Description;
                        if (!string.IsNullOrWhiteSpace(sectionDescription))
                        {
                            foreach (var line in NormalizeNewlines(sectionDescription).Split('\n'))
                                sb.Append("# ").AppendLine(line);
                        }

                        if (bySection.TryGetValue(sectionName, out var list))
                        {
                            // Keep declaration order via ConfigurationManager ordinals.
                            list.Sort(
                                (a, b) =>
                                    ConfigurationManager
                                        .GetOrdinal(a?.Key)
                                        .CompareTo(ConfigurationManager.GetOrdinal(b?.Key))
                            );

                            for (int i = 0; i < list.Count; i++)
                            {
                                var opt = list[i];
                                if (opt == null)
                                    continue;

                                var desc = opt.Description;
                                if (!string.IsNullOrWhiteSpace(desc))
                                {
                                    foreach (var line in NormalizeNewlines(desc).Split('\n'))
                                        sb.Append("# ").AppendLine(line);
                                }

                                sb.Append(opt.Key).Append(" = ").AppendLine(FormatValue(opt));
                                sb.AppendLine();
                            }
                        }
                        else
                        {
                            sb.AppendLine();
                        }
                    }

                    // Write atomically.
                    var tmp = path + ".tmp";
                    File.WriteAllText(tmp, sb.ToString(), Encoding.UTF8);
                    File.Copy(tmp, path, overwrite: true);
                    File.Delete(tmp);
                }
                catch
                {
                    // ignore
                }
            }
        }

        /// <summary>
        /// Persists configuration in response to an option change.
        /// </summary>
        internal static void SaveOnChange(
            IReadOnlyList<Section> sections,
            IReadOnlyList<IOption> options
        )
        {
            // Keep it simple: write the whole file on each change.
            SaveAll(sections, options);
        }

        /// <summary>
        /// Formats an option value into its INI string representation.
        /// </summary>
        private static string FormatValue(IOption opt)
        {
            object value;
            try
            {
                value = opt.GetObject();
            }
            catch
            {
                value = opt.Default;
            }

            var t = opt.Type;

            try
            {
                if (t == typeof(bool))
                    return (
                        (bool)Convert.ChangeType(value, typeof(bool), CultureInfo.InvariantCulture)
                    )
                        ? "true"
                        : "false";

                if (t == typeof(int))
                    return Convert
                        .ToInt32(value, CultureInfo.InvariantCulture)
                        .ToString(CultureInfo.InvariantCulture);

                if (t == typeof(float))
                    return Convert
                        .ToSingle(value, CultureInfo.InvariantCulture)
                        .ToString(CultureInfo.InvariantCulture);

                if (t == typeof(double))
                    return Convert
                        .ToDouble(value, CultureInfo.InvariantCulture)
                        .ToString(CultureInfo.InvariantCulture);

                if (t != null && t.IsEnum)
                    return value?.ToString() ?? string.Empty;
            }
            catch
            {
                // fall through
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        /// <summary>
        /// Parses an INI file into a flat map of key/value pairs.
        /// </summary>
        private static Dictionary<string, string> ParseIni(string path)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw?.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                if (line.StartsWith("#") || line.StartsWith(";"))
                    continue;

                // Ignore sections; keys are global (option keys are unique).
                if (line.StartsWith("[") && line.EndsWith("]"))
                    continue;

                int eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;

                var key = line.Substring(0, eq).Trim();
                var val = line.Substring(eq + 1).Trim();

                if (string.IsNullOrEmpty(key))
                    continue;

                dict[key] = val;
            }

            return dict;
        }

        /// <summary>
        /// Applies parsed values to the provided option list.
        /// </summary>
        private static void ApplyValues(
            Dictionary<string, string> valuesByKey,
            IReadOnlyList<IOption> options
        )
        {
            if (valuesByKey == null || valuesByKey.Count == 0)
                return;

            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                if (opt == null)
                    continue;

                if (string.IsNullOrWhiteSpace(opt.Key))
                    continue;

                if (!valuesByKey.TryGetValue(opt.Key, out var raw))
                    continue;

                if (TryParseValue(raw, opt.Type, out var parsed))
                {
                    try
                    {
                        opt.SetObject(parsed);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to parse a raw string into a typed option value.
        /// </summary>
        private static bool TryParseValue(string raw, Type type, out object value)
        {
            value = null;
            raw ??= string.Empty;

            try
            {
                if (type == typeof(bool))
                {
                    if (bool.TryParse(raw, out var b))
                    {
                        value = b;
                        return true;
                    }

                    if (
                        raw == "0"
                        || raw.Equals("off", StringComparison.OrdinalIgnoreCase)
                        || raw.Equals("no", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        value = false;
                        return true;
                    }

                    if (
                        raw == "1"
                        || raw.Equals("on", StringComparison.OrdinalIgnoreCase)
                        || raw.Equals("yes", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        value = true;
                        return true;
                    }

                    return false;
                }

                if (type == typeof(int))
                {
                    if (
                        int.TryParse(
                            raw,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out var i
                        )
                    )
                    {
                        value = i;
                        return true;
                    }

                    return false;
                }

                if (type == typeof(float))
                {
                    if (
                        float.TryParse(
                            raw,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out var f
                        )
                    )
                    {
                        value = f;
                        return true;
                    }

                    return false;
                }

                if (type == typeof(double))
                {
                    if (
                        double.TryParse(
                            raw,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out var d
                        )
                    )
                    {
                        value = d;
                        return true;
                    }

                    return false;
                }

                if (type != null && type.IsEnum)
                {
                    try
                    {
                        value = Enum.Parse(type, raw, ignoreCase: true);
                        return true;
                    }
                    catch
                    {
                        // allow numeric
                        if (
                            int.TryParse(
                                raw,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out var i
                            )
                        )
                        {
                            value = Enum.ToObject(type, i);
                            return true;
                        }

                        return false;
                    }
                }

                // Fallback: store as string.
                value = raw;
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Normalizes newlines to '\n' for consistent serialization.
        /// </summary>
        private static string NormalizeNewlines(string text)
        {
            return (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
        }
    }
}
