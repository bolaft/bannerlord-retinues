using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Retinues.Utilities;
using TaleWorlds.Localization;

namespace Retinues.Editor.MVC.Pages.Library.Services
{
    /// <summary>
    /// XML reading utilities for export files.
    /// </summary>
    public static class ExportXMLReader
    {
        /// <summary>
        /// Payload with optional model string ID.
        /// </summary>
        public readonly struct ModelPayload(string payload, string modelStringId)
        {
            public string Payload { get; } = payload;
            public string ModelStringId { get; } = modelStringId;
        }

        /// <summary>
        /// Reads troop names from an export file.
        /// </summary>
        public static bool TryReadTroopNames(ExportLibrary.Entry entry, out List<string> troopNames)
        {
            troopNames = [];

            try
            {
                if (entry == null)
                    return false;

                if (string.IsNullOrWhiteSpace(entry.FilePath) || !File.Exists(entry.FilePath))
                    return false;

                if (!TryLoadRoot(entry.FilePath, out var root))
                    return false;

                foreach (var el in root.Elements())
                {
                    if (!IsCharacterElement(el))
                        continue;

                    var name = ReadCharacterName(el);
                    if (!string.IsNullOrWhiteSpace(name))
                        troopNames.Add(name);

                    if (entry.Kind == ExportKind.Character)
                        break;
                }

                troopNames = [.. troopNames.Where(s => !string.IsNullOrWhiteSpace(s))];
                return troopNames.Count > 0;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "ExportXmlReader.TryReadTroopNames failed.");
                return false;
            }
        }

        /// <summary>
        /// Extracts model character payloads from an export file.
        /// </summary>
        public static bool TryExtractModelCharacterPayloads(
            ExportLibrary.Entry entry,
            out List<ModelPayload> payloads
        )
        {
            payloads = [];

            try
            {
                if (entry == null)
                    return false;

                if (string.IsNullOrWhiteSpace(entry.FilePath) || !File.Exists(entry.FilePath))
                    return false;

                if (!TryLoadRoot(entry.FilePath, out var root))
                    return false;

                foreach (var el in root.Elements())
                {
                    if (!IsCharacterElement(el))
                        continue;

                    var payload = ExtractPayload(el);
                    if (string.IsNullOrWhiteSpace(payload))
                        continue;

                    var modelStringId = TryGetModelStringId(el);
                    payloads.Add(new ModelPayload(payload, modelStringId));

                    if (entry.Kind == ExportKind.Character)
                        break;
                }

                return payloads.Count > 0;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "ExportXmlReader.TryExtractModelCharacterPayloads failed.");
                return false;
            }
        }

        /// <summary>
        /// Loads the XML root element from an export file.
        /// </summary>
        private static bool TryLoadRoot(string filePath, out XElement root)
        {
            root = null;

            try
            {
                var doc = XDocument.Load(filePath, LoadOptions.None);
                root = doc.Root;
                return root != null && root.Name.LocalName == "Retinues";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Determines if an XML element represents a character export.
        /// </summary>
        internal static bool IsCharacterElement(XElement el)
        {
            if (el == null)
                return false;

            var name = el.Name.LocalName ?? string.Empty;
            if (name.Contains("WCharacter"))
                return true;

            var type = (string)el.Attribute("type");
            if (!string.IsNullOrWhiteSpace(type) && type.Contains(".Characters.WCharacter"))
                return true;

            return false;
        }

        /// <summary>
        /// Extracts the payload from a character XML element.
        /// </summary>
        internal static string ExtractPayload(XElement el)
        {
            if (el == null)
                return string.Empty;

            // Legacy fallback: <Entry><![CDATA[...]]></Entry>
            if (el.Name.LocalName == "Entry")
                return el.Value ?? string.Empty;

            // Current format: wrapper element contains serialized XML; remove uid if present.
            var copy = new XElement(el);
            copy.SetAttributeValue("uid", null);

            return copy.ToString(SaveOptions.DisableFormatting);
        }

        /// <summary>
        /// Reads the character name from a character XML element.
        /// </summary>
        private static string ReadCharacterName(XElement el)
        {
            if (el == null)
                return string.Empty;

            var nameEl = el.Elements().FirstOrDefault(x => x.Name.LocalName == "NameAttribute");
            if (nameEl != null)
            {
                var raw = ResolveTextObjectString(nameEl.Value ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(raw))
                    return raw;
            }

            var attrName = ResolveTextObjectString((string)el.Attribute("name") ?? string.Empty);
            return attrName ?? string.Empty;
        }

        /// <summary>
        /// Resolves a TextObject string to plain text.
        /// </summary>
        internal static string ResolveTextObjectString(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            if (raw.StartsWith("{=", StringComparison.Ordinal) && raw.Contains("}"))
            {
                try
                {
                    return new TextObject(raw).ToString();
                }
                catch
                {
                    return raw;
                }
            }

            return raw;
        }

        /// <summary>
        /// Tries to get the model string ID from a character XML element.
        /// </summary>
        private static string TryGetModelStringId(XElement el)
        {
            if (el == null)
                return null;

            var id = (string)el.Attribute("stringId") ?? (string)el.Attribute("id");
            return string.IsNullOrWhiteSpace(id) ? null : id;
        }
    }
}
