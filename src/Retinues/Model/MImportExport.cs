using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Retinues.Helpers;
using Retinues.Model.Characters;
using Retinues.Model.Factions;
using Retinues.Utilities;

namespace Retinues.Model
{
    [SafeClass(IncludeDerived = true)]
    public static partial class MImportExport
    {
        const string ExportFolderName = "Exports";

        const string RootName = "Retinues";
        const string RootVersion = "1";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Export                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void ExportCharacter(string characterStringId)
        {
            if (string.IsNullOrWhiteSpace(characterStringId))
            {
                Notifications.Message("Export failed: empty character id.");
                return;
            }

            var c = WCharacter.Get(characterStringId);
            if (c == null)
            {
                Notifications.Message(
                    $"Export failed: character not found: '{characterStringId}'."
                );
                return;
            }

            PromptFileName(
                kind: "character",
                source: c,
                onChosen: fileName =>
                {
                    try
                    {
                        var path = BuildExportPath(fileName);

                        var root = BuildRoot(kind: "character", sourceId: c.StringId);

                        AddSerialized(root, c.UniqueId, c.SerializeAll());

                        WriteXml(path, root);

                        Notifications.Message(
                            $"Exported '{c.StringId}' to '{Path.GetFileName(path)}'."
                        );
                        Log.Info($"Exported character '{c.StringId}' to '{path}'.");
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex, "ExportCharacter failed.");
                        Notifications.Message($"Export failed: {ex.Message}");
                    }
                }
            );
        }

        public static void ExportFaction(string factionStringId)
        {
            if (string.IsNullOrWhiteSpace(factionStringId))
            {
                Notifications.Message("Export failed: empty faction id.");
                return;
            }

            var f = ResolveFaction(factionStringId);
            if (f == null)
            {
                Notifications.Message($"Export failed: faction not found: '{factionStringId}'.");
                return;
            }

            PromptFileName(
                kind: "faction",
                source: f,
                onChosen: fileName =>
                {
                    try
                    {
                        var path = BuildExportPath(fileName);

                        var root = BuildRoot(kind: "faction", sourceId: f.StringId);

                        AddSerialized(root, null, f.SerializeAll());

                        var troops = f
                            .Troops.Where(t => t != null && !string.IsNullOrWhiteSpace(t.StringId))
                            .GroupBy(t => t.StringId)
                            .Select(g => g.First())
                            .ToList();

                        for (int i = 0; i < troops.Count; i++)
                        {
                            var t = troops[i];
                            AddSerialized(root, t.UniqueId, t.SerializeAll());
                        }

                        WriteXml(path, root);

                        Notifications.Message(
                            $"Exported '{f.StringId}' ({troops.Count} troops) to '{Path.GetFileName(path)}'."
                        );
                        Log.Info(
                            $"Exported faction '{f.StringId}' to '{path}' with {troops.Count + 1} elements."
                        );
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex, "ExportFaction failed.");
                        Notifications.Message($"Export failed: {ex.Message}");
                    }
                }
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Import                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void ImportCharacter(string filepath)
        {
            if (!TryImportCharacter(filepath, out var err))
                Notifications.Message($"Import failed: {err}");
        }

        public static void ImportFaction(string filepath)
        {
            if (!TryImportFaction(filepath, out var err))
                Notifications.Message($"Import failed: {err}");
        }

        public static bool TryImportCharacter(string filepath, out string error)
        {
            error = null;

            try
            {
                if (string.IsNullOrWhiteSpace(filepath) || !System.IO.File.Exists(filepath))
                {
                    error = "file not found.";
                    return false;
                }

                var xml = System.IO.File.ReadAllText(filepath, Encoding.UTF8);
                if (!TryParseXmlRoot(xml, out var root))
                {
                    error = "invalid XML file.";
                    return false;
                }

                if (root.Name.LocalName != RootName || (string)root.Attribute("v") != RootVersion)
                {
                    error = "unknown export format.";
                    return false;
                }

                var el =
                    root.Elements().FirstOrDefault(IsCharacterElement) ?? root.Elements()
                        .FirstOrDefault();
                if (el == null)
                {
                    error = "missing character element.";
                    return false;
                }

                // Prefer element id, but fallback to root source.
                var stringId =
                    (string)el.Attribute("stringId")
                    ?? TryGetStringIdFromUid((string)el.Attribute("uid"))
                    ?? (string)root.Attribute("source");

                if (string.IsNullOrWhiteSpace(stringId))
                {
                    error = "could not resolve character StringId.";
                    return false;
                }

                var c = WCharacter.Get(stringId);
                if (c == null)
                {
                    error = $"character not found: '{stringId}'.";
                    return false;
                }

                c.Deserialize(ExtractPayload(el));

                Log.Info($"Imported character '{stringId}' from '{filepath}'.");
                return true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "TryImportCharacter failed.");
                error = ex.Message ?? "unknown error.";
                return false;
            }
        }

        public static bool TryImportFaction(string filepath, out string error)
        {
            error = null;

            try
            {
                if (string.IsNullOrWhiteSpace(filepath) || !System.IO.File.Exists(filepath))
                {
                    error = "file not found.";
                    return false;
                }

                var xml = System.IO.File.ReadAllText(filepath, Encoding.UTF8);
                if (!TryParseXmlRoot(xml, out var root))
                {
                    error = "invalid XML file.";
                    return false;
                }

                if (root.Name.LocalName != RootName || (string)root.Attribute("v") != RootVersion)
                {
                    error = "unknown export format.";
                    return false;
                }

                var elements = root.Elements().ToList();
                if (elements.Count == 0)
                {
                    error = "file has no elements.";
                    return false;
                }

                // Prefer root source, then the wrapper element's stringId.
                var sourceId = (string)root.Attribute("source");

                // Find the faction wrapper element rather than assuming elements[0].
                var fEl = elements.FirstOrDefault(IsFactionElement);

                var factionId =
                    sourceId
                    ?? (string)fEl?.Attribute("stringId")
                    ?? TryGetStringIdFromUid((string)fEl?.Attribute("uid"));

                if (string.IsNullOrWhiteSpace(factionId))
                {
                    error = "could not resolve faction StringId.";
                    return false;
                }

                var f = ResolveFaction(factionId);
                if (f == null)
                {
                    error = $"faction not found: '{factionId}'.";
                    return false;
                }

                // Apply faction payload if present.
                if (fEl != null)
                    f.Deserialize(ExtractPayload(fEl));

                int imported = 0;
                int skipped = 0;

                for (int i = 0; i < elements.Count; i++)
                {
                    var el = elements[i];

                    // Skip faction wrapper and any non-character elements.
                    if (!IsCharacterElement(el))
                        continue;

                    var id =
                        (string)el.Attribute("stringId")
                        ?? TryGetStringIdFromUid((string)el.Attribute("uid"));

                    if (string.IsNullOrWhiteSpace(id))
                        continue;

                    var c = WCharacter.Get(id);
                    if (c == null)
                    {
                        skipped++;
                        continue;
                    }

                    c.Deserialize(ExtractPayload(el));
                    imported++;
                }

                Log.Info(
                    $"Imported faction '{factionId}' from '{filepath}'. Troops imported={imported}, skipped={skipped}."
                );

                // Treat "faction resolved + applied (or not) + troops loop executed" as success.
                // If you want "must import at least 1 troop" semantics, say so and I will tighten it.
                return true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "TryImportFaction failed.");
                error = ex.Message ?? "unknown error.";
                return false;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        static XElement BuildRoot(string kind, string sourceId)
        {
            var root = new XElement(RootName);
            root.SetAttributeValue("v", RootVersion);
            root.SetAttributeValue("kind", kind ?? string.Empty);
            root.SetAttributeValue("source", sourceId ?? string.Empty);
            root.SetAttributeValue("createdUtc", DateTime.UtcNow.ToString("o"));
            return root;
        }

        static void AddSerialized(XElement root, string uid, string serialized)
        {
            if (root == null)
                return;

            if (string.IsNullOrWhiteSpace(serialized))
                return;

            var trimmed = serialized.TrimStart();

            if (trimmed.StartsWith("<"))
            {
                try
                {
                    var el = XElement.Parse(serialized, LoadOptions.None);

                    if (!string.IsNullOrWhiteSpace(uid))
                        el.SetAttributeValue("uid", uid);

                    root.Add(el);
                    return;
                }
                catch
                {
                    // fall through to Entry wrapper
                }
            }

            root.Add(
                new XElement(
                    "Entry",
                    new XAttribute("uid", uid ?? string.Empty),
                    new XAttribute("format", "text"),
                    new XCData(serialized)
                )
            );
        }

        static string ExtractPayload(XElement el)
        {
            if (el == null)
                return string.Empty;

            if (el.Name.LocalName == "Entry")
                return el.Value ?? string.Empty;

            var copy = new XElement(el);
            copy.SetAttributeValue("uid", null);

            return copy.ToString(SaveOptions.DisableFormatting);
        }

        static bool TryParseXmlRoot(string xml, out XElement root)
        {
            root = null;

            if (string.IsNullOrWhiteSpace(xml))
                return false;

            var trimmed = xml.TrimStart();
            if (!trimmed.StartsWith("<"))
                return false;

            try
            {
                var doc = XDocument.Parse(xml, LoadOptions.None);
                root = doc.Root;
                return root != null;
            }
            catch
            {
                return false;
            }
        }

        static string TryGetStringIdFromUid(string uid)
        {
            if (string.IsNullOrWhiteSpace(uid))
                return null;

            var sep = uid.IndexOf(':');
            if (sep <= 0 || sep >= uid.Length - 1)
                return null;

            return uid.Substring(sep + 1);
        }

        static bool IsCharacterElement(XElement el)
        {
            if (el == null)
                return false;

            var type = (string)el.Attribute("type");
            if (!string.IsNullOrWhiteSpace(type) && type.Contains(".Characters.WCharacter"))
                return true;

            return el.Name.LocalName.Contains("WCharacter");
        }

        static bool IsFactionElement(XElement el)
        {
            if (el == null)
                return false;

            var type = (string)el.Attribute("type");
            if (!string.IsNullOrWhiteSpace(type) && type.Contains(".Factions."))
                return true;

            var n = el.Name.LocalName ?? string.Empty;

            return n.Contains("WFaction")
                || n.Contains("WClan")
                || n.Contains("WKingdom")
                || n.Contains("WCulture");
        }

        static void WriteXml(string path, XElement root)
        {
            if (string.IsNullOrWhiteSpace(path) || root == null)
                return;

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var content = root.ToString(SaveOptions.None);
            System.IO.File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        static void PromptFileName(string kind, object source, Action<string> onChosen)
        {
            var placeholder = GetSourcePlaceholder(kind, source);

            Inquiries.TextInputPopup(
                title: L.T("export_title", "Save"),
                defaultInput: placeholder,
                onConfirm: input =>
                {
                    var baseName = string.IsNullOrWhiteSpace(input) ? placeholder : input;
                    var name = SanitizeFileName(baseName);
                    onChosen?.Invoke(name);
                },
                description: L.T("export_desc", "Choose a save name:")
            );
        }

        static string GetSourcePlaceholder(string kind, object source)
        {
            if (source == null)
                return kind ?? "export";

            if (source is WCharacter wc && !string.IsNullOrWhiteSpace(wc.Name))
                return wc.Name.Trim();

            if (source is IBaseFaction bf && !string.IsNullOrWhiteSpace(bf.Name))
                return bf.Name.Trim();

            if (source is string s && !string.IsNullOrWhiteSpace(s))
                return s.Trim();

            return kind ?? "export";
        }

        static string BuildExportPath(string fileName)
        {
            var safe = SanitizeFileName(fileName);

            if (!safe.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                safe += ".xml";

            return FileSystem.GetPathInRetinuesDocuments(ExportFolderName, safe);
        }

        static string SanitizeFileName(string fileName)
        {
            fileName = (fileName ?? string.Empty).Trim();
            if (fileName.Length == 0)
                fileName = "export";

            fileName = Path.GetFileName(fileName);

            var invalid = new string(Path.GetInvalidFileNameChars());
            var re = new Regex("[" + Regex.Escape(invalid) + "]+");
            fileName = re.Replace(fileName, "_");

            return fileName;
        }

        static IBaseFaction ResolveFaction(string stringId)
        {
            var clan = WClan.Get(stringId);
            if (clan != null)
                return clan;

            var kingdom = WKingdom.Get(stringId);
            if (kingdom != null)
                return kingdom;

            var culture = WCulture.Get(stringId);
            if (culture != null)
                return culture;

            return null;
        }
    }
}
