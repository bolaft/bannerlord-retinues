using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Retinues.Utilities;

namespace Retinues.Framework.Model.Exports
{
    public static partial class MImportExport
    {
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
                Log.Exception(ex, "TryParseCharacterExport failed.");
                error = ex.Message ?? "unknown error.";
                return false;
            }
        }

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
                Log.Exception(ex, "TryParseFactionExport failed.");
                error = ex.Message ?? "unknown error.";
                return false;
            }
        }

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

        private static XElement BuildRoot(string kind, string sourceId)
        {
            var root = new XElement(RootName);
            root.SetAttributeValue("v", RootVersion);
            root.SetAttributeValue("kind", kind ?? string.Empty);
            root.SetAttributeValue("source", sourceId ?? string.Empty);
            root.SetAttributeValue("createdUtc", DateTime.UtcNow.ToString("o"));
            return root;
        }

        private static XElement AddSerialized(XElement root, string uid, string serialized)
        {
            if (root == null || string.IsNullOrWhiteSpace(serialized))
                return null;

            var trimmed = serialized.TrimStart();

            if (trimmed.StartsWith("<"))
            {
                try
                {
                    var el = XElement.Parse(serialized, LoadOptions.None);

                    if (!string.IsNullOrWhiteSpace(uid))
                        el.SetAttributeValue("uid", uid);

                    root.Add(el);
                    return el;
                }
                catch
                {
                    // fall through
                }
            }

            var entry = new XElement(
                "Entry",
                new XAttribute("uid", uid ?? string.Empty),
                new XCData(serialized)
            );
            root.Add(entry);
            return entry;
        }

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

        internal static string TryGetStringIdFromUid(string uid)
        {
            if (string.IsNullOrWhiteSpace(uid))
                return null;

            var sep = uid.IndexOf(':');
            if (sep <= 0 || sep >= uid.Length - 1)
                return null;

            return uid.Substring(sep + 1);
        }

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
