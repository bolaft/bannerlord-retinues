using System;
using System.Collections.Generic;
using Retinues.Domain.Factions.Wrappers;

namespace Retinues.Behaviors.Troops
{
    /// <summary>
    /// Partial class for troop naming utilities.
    /// </summary>
    public sealed partial class TroopUnlockerBehavior
    {
        /// <summary>
        /// Builds a faction-prefixed troop name while avoiding duplicate culture prefixes.
        /// </summary>
        private static string BuildFactionTroopName(
            string templateName,
            string factionName,
            WCulture culture
        )
        {
            var baseName = (templateName ?? string.Empty).Trim();
            var prefix = (factionName ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(prefix))
                return baseName;

            var stripped = StripCulturePrefix(baseName, culture);

            if (string.IsNullOrEmpty(stripped))
                return prefix;

            if (stripped.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase))
                return stripped;

            return $"{prefix} {stripped}";
        }

        /// <summary>
        /// Removes culture-derived prefixes from a troop name for cleaner faction naming.
        /// </summary>
        private static string StripCulturePrefix(string name, WCulture culture)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var s = name.Trim();
            if (culture?.Base == null)
                return s;

            var cultureName = (culture.Name ?? string.Empty).Trim();
            var prefixes = BuildCulturePrefixCandidates(cultureName, culture.StringId);

            for (int i = 0; i < prefixes.Count; i++)
            {
                var p = prefixes[i];
                if (string.IsNullOrEmpty(p))
                    continue;

                if (s.StartsWith(p + " ", StringComparison.OrdinalIgnoreCase))
                    return s.Substring(p.Length + 1).Trim();
            }

            return s;
        }

        /// <summary>
        /// Returns candidate culture prefix strings used when stripping culture prefixes.
        /// </summary>
        private static List<string> BuildCulturePrefixCandidates(
            string cultureName,
            string cultureId
        )
        {
            var list = new List<string>();

            if (!string.IsNullOrEmpty(cultureName))
                list.Add(cultureName);

            if (
                !string.IsNullOrEmpty(cultureName)
                && cultureName.EndsWith("a", StringComparison.OrdinalIgnoreCase)
            )
                list.Add(cultureName + "n");

            if (
                !string.IsNullOrEmpty(cultureName)
                && cultureName.EndsWith("ia", StringComparison.OrdinalIgnoreCase)
            )
                list.Add(cultureName.Substring(0, cultureName.Length - 2) + "ian");

            if (
                !string.IsNullOrEmpty(cultureId)
                && cultureId.Equals("empire", StringComparison.OrdinalIgnoreCase)
            )
            {
                list.Add("Imperial");
                list.Add("Empire");
            }

            list.Sort((a, b) => (b?.Length ?? 0).CompareTo(a?.Length ?? 0));
            return list;
        }
    }
}
