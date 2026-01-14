using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Retinues.Utilities;

namespace Retinues.Configuration
{
    public static partial class SettingsManager
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Logging                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Log the current configuration values grouped by section.
        /// </summary>
        public static void LogSettings()
        {
            try
            {
                DiscoverOptions();

                Log.Debug("Retinues Config:");

                var grouped = new Dictionary<string, List<IOption>>();
                for (int i = 0; i < _all.Count; i++)
                {
                    IOption opt = _all[i];
                    if (!grouped.TryGetValue(opt.Section, out List<IOption> list))
                    {
                        list = [];
                        grouped[opt.Section] = list;
                    }

                    list.Add(opt);
                }

                // Preserve section declaration order using Section ordinals.
                foreach (var kv in grouped.OrderBy(g => GetSectionOrdinal(g.Key)))
                {
                    string sectionName = kv.Key;
                    List<IOption> options = kv.Value;

                    Log.Debug($"[{sectionName}]");

                    options.Sort(
                        (a, b) =>
                        {
                            int ia = GetOrdinal(a.Key);
                            int ib = GetOrdinal(b.Key);
                            return ia.CompareTo(ib);
                        }
                    );

                    foreach (var opt in options)
                    {
                        object current = opt.GetObject();
                        object def = opt.Default;

                        string currentText = FormatConfigValue(current);
                        string defaultText = FormatConfigValue(def);

                        bool changed = !Equals(current, def);
                        string marker = changed ? "*" : " ";

                        string label;
                        if (string.IsNullOrWhiteSpace(opt.Name))
                            label = opt.Key;
                        else
                            label = opt.Name + " [" + opt.Key + "]";

                        if (opt.IsDisabled)
                        {
                            Log.Debug($"{marker} {label} = {currentText} (DISABLED; override)");
                        }
                        else
                        {
                            Log.Debug($"{marker} {label} = {currentText} (default: {defaultText})");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "LogSettings failed.");
            }
        }

        /// <summary>
        /// Formats a config value for logging.
        /// </summary>
        private static string FormatConfigValue(object value)
        {
            if (value == null)
                return "null";
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }
}
