using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Retinues.Utilities;

namespace Retinues.Exports
{
    /// <summary>
    /// Parses exported Retinues XML files (format v1) and extracts character/faction entries.
    /// </summary>
    public static class ExportFileParser
    {
        internal const string RootName = "Retinues";
        internal const string RootVersion = "1";

        /// <summary>
        /// Attempts to parse a single character export from the given file.
        /// </summary>
        public static bool TryParseCharacterExport(
            string filepath,
            out CharacterExportEntry entry,
            out string error
        )
        {
            entry = null;
            error = null;

            try
            {
                if (!TryReadRoot(filepath, out var root, out error))
                    return false;

                var el =
                    root.Elements().FirstOrDefault(x => IsCharacterElement(x, loose: true))
                    ?? root.Elements().FirstOrDefault();

                if (el == null)
                {
                    error = "missing character element.";
                    return false;
                }

                var sourceId =
                    (string)el.Attribute("stringId")
                    ?? TryGetStringIdFromUid((string)el.Attribute("uid"))
                    ?? (string)root.Attribute("source");

                entry = new CharacterExportEntry
                {
                    SourceId = sourceId,
                    RosterKey = (string)el.Attribute("r") ?? string.Empty,
                    PayloadXml = ExtractPayload(el),
                };

                return true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "RetinuesExportParser.TryParseCharacterExport failed.");
                error = ex.Message ?? "unknown error.";
                return false;
            }
        }

        /// <summary>
        /// Attempts to parse a faction export (with multiple character entries) from the given file.
        /// </summary>
        public static bool TryParseFactionExport(
            string filepath,
            out FactionExportData data,
            out string error
        )
        {
            data = null;
            error = null;

            try
            {
                if (!TryReadRoot(filepath, out var root, out error))
                    return false;

                var elements = root.Elements().ToList();
                if (elements.Count == 0)
                {
                    error = "file has no elements.";
                    return false;
                }

                var sourceId = (string)root.Attribute("source");

                // Find the faction wrapper element (first non-character element).
                var fEl = elements.FirstOrDefault(IsFactionElement);

                var factionId =
                    sourceId
                    ?? (string)fEl?.Attribute("stringId")
                    ?? TryGetStringIdFromUid((string)fEl?.Attribute("uid"));

                var troopElements = elements
                    .Where(x => IsCharacterElement(x, loose: true))
                    .ToList();

                var troops = new List<CharacterExportEntry>(troopElements.Count);

                for (int i = 0; i < troopElements.Count; i++)
                {
                    var el = troopElements[i];

                    var id =
                        (string)el.Attribute("stringId")
                        ?? TryGetStringIdFromUid((string)el.Attribute("uid"))
                        ?? string.Empty;

                    troops.Add(
                        new CharacterExportEntry
                        {
                            SourceId = id,
                            RosterKey = (string)el.Attribute("r") ?? string.Empty,
                            PayloadXml = ExtractPayload(el),
                        }
                    );
                }

                data = new FactionExportData
                {
                    SourceFactionId = factionId ?? string.Empty,
                    FactionPayloadXml = fEl != null ? ExtractPayload(fEl) : string.Empty,
                    Troops = troops,
                };

                return true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "RetinuesExportParser.TryParseFactionExport failed.");
                error = ex.Message ?? "unknown error.";
                return false;
            }
        }

        /// <summary>
        /// Reads and validates the export root element from a file.
        /// </summary>
        private static bool TryReadRoot(string filepath, out XElement root, out string error)
        {
            root = null;
            error = null;

            if (string.IsNullOrWhiteSpace(filepath) || !File.Exists(filepath))
            {
                error = "file not found.";
                return false;
            }

            var xml = File.ReadAllText(filepath, Encoding.UTF8);
            if (!XML.TryParseRoot(xml, out root))
            {
                error = "invalid XML file.";
                return false;
            }

            if (root.Name.LocalName != RootName || (string)root.Attribute("v") != RootVersion)
            {
                error = "unknown export format.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Extracts the serialized payload string from an export entry element.
        /// </summary>
        internal static string ExtractPayload(XElement el)
        {
            if (el == null)
                return string.Empty;

            if (el.Name.LocalName == "Entry")
                return el.Value ?? string.Empty;

            var copy = new XElement(el);
            copy.SetAttributeValue("uid", null);

            return copy.ToString(SaveOptions.DisableFormatting);
        }

        /// <summary>
        /// Attempts to derive a string id from a uid of the form "prefix:id".
        /// </summary>
        internal static string TryGetStringIdFromUid(string uid)
        {
            if (string.IsNullOrWhiteSpace(uid))
                return null;

            var sep = uid.IndexOf(':');
            if (sep <= 0 || sep >= uid.Length - 1)
                return null;

            return uid.Substring(sep + 1);
        }

        /// <summary>
        /// Returns true if the element represents a character entry (loose matching optional).
        /// </summary>
        internal static bool IsCharacterElement(XElement el, bool loose = false)
        {
            if (el == null)
                return false;

            if (!loose)
                return el.Name.LocalName.Contains("WCharacter");

            var name = el.Name.LocalName ?? string.Empty;
            if (string.Equals(name, "WCharacter", StringComparison.OrdinalIgnoreCase))
                return true;

            var t = (string)el.Attribute("type") ?? string.Empty;
            return t.IndexOf("WCharacter", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Returns true if the element likely represents a faction element (clan/kingdom/culture).
        /// </summary>
        private static bool IsFactionElement(XElement el)
        {
            if (el == null)
                return false;

            if (IsCharacterElement(el, loose: true))
                return false;

            var type = (string)el.Attribute("type");
            if (!string.IsNullOrWhiteSpace(type) && type.Contains(".Factions."))
                return true;

            var n = el.Name.LocalName ?? string.Empty;

            return n.Contains("WClan")
                || n.Contains("WKingdom")
                || n.Contains("WCulture")
                || n.Contains("WFaction");
        }
    }
}
