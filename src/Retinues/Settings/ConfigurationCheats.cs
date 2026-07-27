using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TaleWorlds.Library;

namespace Retinues.Settings
{
    /// <summary>
    /// Console commands for diagnosing and repairing settings.
    ///
    /// Settings persist globally in Documents/Retinues/settings.ini, across campaigns and mod
    /// reinstalls. That is by design (the new-campaign prompt offers "keep current settings"),
    /// but it means a value set in an old version silently carries into every "fresh" game — a
    /// player can be convinced they are on defaults when they are not, and the community
    /// workaround becomes "delete the Retinues folder". These commands make that unnecessary:
    /// print_settings shows exactly what differs from defaults (pasteable into a bug report),
    /// and reset_settings restores defaults without touching logs or backups.
    /// </summary>
    internal static class ConfigurationCheats
    {
        /// <summary>
        /// Prints every setting that differs from its default, plus the settings file path.
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("print_settings", "retinues")]
        public static string PrintSettings(List<string> args)
        {
            ConfigurationManager.DiscoverOptions();

            var sb = new StringBuilder();
            sb.AppendLine($"Settings file: {ConfigurationPersistence.ConfigPath}");

            int changed = 0;

            var options = ConfigurationManager.Options;
            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                if (opt?.Key == null)
                    continue;

                object current = opt.GetObject();
                object def = opt.Default;

                if (Equals(current, def))
                    continue;

                changed++;
                sb.AppendLine(
                    $"  {opt.Key} = {Convert.ToString(current, CultureInfo.InvariantCulture)} "
                        + $"(default: {Convert.ToString(def, CultureInfo.InvariantCulture)})"
                );
            }

            sb.AppendLine(
                changed == 0
                    ? "All settings are at their default values."
                    : $"{changed} setting(s) differ from defaults."
            );

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Resets every setting to its default value and saves. Equivalent to the settings page's
        /// Default preset — no need to delete the Documents/Retinues folder.
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("reset_settings", "retinues")]
        public static string ResetSettings(List<string> args)
        {
            ConfigurationManager.ApplyPreset(SettingsPreset.Default);
            return "All settings reset to defaults and saved. (Logs and backups are untouched.)";
        }
    }
}
