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
    public static class MImportExport
    {
        const string ExportFolderName = "Exports";

        const string RootName = "Retinues";
        const string RootVersion = "2";

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

                        AddSerialized(root, c.UniqueId, c.Serialize());

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

                        AddSerialized(root, null, f.Serialize());

                        var troops = f
                            .Troops.Where(t => t != null && !string.IsNullOrWhiteSpace(t.StringId))
                            .GroupBy(t => t.StringId)
                            .Select(g => g.First())
                            .ToList();

                        for (int i = 0; i < troops.Count; i++)
                        {
                            var t = troops[i];
                            AddSerialized(root, t.UniqueId, t.Serialize());
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
            try
            {
                if (string.IsNullOrWhiteSpace(filepath) || !File.Exists(filepath))
                {
                    Notifications.Message("Import failed: file not found.");
                    return;
                }

                var xml = File.ReadAllText(filepath, Encoding.UTF8);
                if (!TryParseXmlRoot(xml, out var root))
                {
                    Notifications.Message("Import failed: invalid XML file.");
                    return;
                }

                // New pretty export format: <Retinues v="2" ...> <WCharacter .../> </Retinues>
                if (root.Name.LocalName == RootName && (string)root.Attribute("v") == RootVersion)
                {
                    var el = root.Elements().FirstOrDefault();
                    if (el == null)
                    {
                        Notifications.Message("Import failed: missing character element.");
                        return;
                    }

                    var stringId =
                        (string)el.Attribute("stringId")
                        ?? TryGetStringIdFromUid((string)el.Attribute("uid"));
                    if (string.IsNullOrWhiteSpace(stringId))
                    {
                        Notifications.Message(
                            "Import failed: could not resolve character StringId."
                        );
                        return;
                    }

                    var c = WCharacter.Get(stringId);
                    if (c == null)
                    {
                        Notifications.Message($"Import failed: character not found: '{stringId}'.");
                        return;
                    }

                    var payload = ExtractPayload(el);
                    c.Deserialize(payload);

                    Notifications.Message($"Imported '{stringId}'.");
                    Log.Info($"Imported character '{stringId}' from '{filepath}'.");
                    return;
                }

                // Legacy export format (RetinuesExport + payload CDATA)
                if (root.Name.LocalName == "RetinuesExport")
                {
                    var export = MExportFile.FromXmlString(xml);
                    if (export.Kind != "character")
                    {
                        Notifications.Message("Import failed: file is not a character export.");
                        return;
                    }

                    var entry = export.Entries.FirstOrDefault();
                    if (entry == null || string.IsNullOrWhiteSpace(entry.StringId))
                    {
                        Notifications.Message("Import failed: missing character entry.");
                        return;
                    }

                    var c = WCharacter.Get(entry.StringId);
                    if (c == null)
                    {
                        Notifications.Message(
                            $"Import failed: character not found: '{entry.StringId}'."
                        );
                        return;
                    }

                    c.Deserialize(entry.PayloadXml);

                    Notifications.Message($"Imported '{entry.StringId}'.");
                    Log.Info($"Imported character '{entry.StringId}' from '{filepath}'.");
                    return;
                }

                Notifications.Message("Import failed: unknown export format.");
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "ImportCharacter failed.");
                Notifications.Message($"Import failed: {ex.Message}");
            }
        }

        public static void ImportFaction(string filepath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filepath) || !File.Exists(filepath))
                {
                    Notifications.Message("Import failed: file not found.");
                    return;
                }

                var xml = File.ReadAllText(filepath, Encoding.UTF8);
                if (!TryParseXmlRoot(xml, out var root))
                {
                    Notifications.Message("Import failed: invalid XML file.");
                    return;
                }

                // New pretty export format: <Retinues v="2" ...> <WFaction .../> <WCharacter .../> ... </Retinues>
                if (root.Name.LocalName == RootName && (string)root.Attribute("v") == RootVersion)
                {
                    var elements = root.Elements().ToList();
                    if (elements.Count == 0)
                    {
                        Notifications.Message("Import failed: file has no elements.");
                        return;
                    }

                    // First element = faction wrapper
                    var fEl = elements[0];
                    var factionId =
                        (string)fEl.Attribute("stringId")
                        ?? TryGetStringIdFromUid((string)fEl.Attribute("uid"));

                    if (string.IsNullOrWhiteSpace(factionId))
                    {
                        Notifications.Message("Import failed: could not resolve faction StringId.");
                        return;
                    }

                    var f = ResolveFaction(factionId);
                    if (f == null)
                    {
                        Notifications.Message($"Import failed: faction not found: '{factionId}'.");
                        return;
                    }

                    f.Deserialize(ExtractPayload(fEl));

                    int imported = 0;
                    int skipped = 0;

                    for (int i = 1; i < elements.Count; i++)
                    {
                        var el = elements[i];

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

                    Notifications.Message(
                        $"Imported '{factionId}' (troops applied: {imported}, skipped: {skipped})."
                    );
                    Log.Info(
                        $"Imported faction '{factionId}' from '{filepath}'. Troops imported={imported}, skipped={skipped}."
                    );
                    return;
                }

                // Legacy export format (RetinuesExport + payload CDATA)
                if (root.Name.LocalName == "RetinuesExport")
                {
                    var export = MExportFile.FromXmlString(xml);
                    if (export.Kind != "faction")
                    {
                        Notifications.Message("Import failed: file is not a faction export.");
                        return;
                    }

                    if (export.Entries == null || export.Entries.Count == 0)
                    {
                        Notifications.Message("Import failed: file has no entries.");
                        return;
                    }

                    var factionEntry = export.Entries[0];
                    if (factionEntry == null || string.IsNullOrWhiteSpace(factionEntry.StringId))
                    {
                        Notifications.Message("Import failed: invalid faction entry.");
                        return;
                    }

                    var f = ResolveFaction(factionEntry.StringId);
                    if (f == null)
                    {
                        Notifications.Message(
                            $"Import failed: faction not found: '{factionEntry.StringId}'."
                        );
                        return;
                    }

                    f.Deserialize(factionEntry.PayloadXml);

                    int imported = 0;
                    int skipped = 0;

                    for (int i = 1; i < export.Entries.Count; i++)
                    {
                        var e = export.Entries[i];
                        if (e == null || string.IsNullOrWhiteSpace(e.StringId))
                            continue;

                        var c = WCharacter.Get(e.StringId);
                        if (c == null)
                        {
                            skipped++;
                            continue;
                        }

                        c.Deserialize(e.PayloadXml);
                        imported++;
                    }

                    Notifications.Message(
                        $"Imported '{factionEntry.StringId}' (troops applied: {imported}, skipped: {skipped})."
                    );
                    Log.Info(
                        $"Imported faction '{factionEntry.StringId}' from '{filepath}'. Troops imported={imported}, skipped={skipped}."
                    );
                    return;
                }

                Notifications.Message("Import failed: unknown export format.");
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "ImportFaction failed.");
                Notifications.Message($"Import failed: {ex.Message}");
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

        static void WriteXml(string path, XElement root)
        {
            if (string.IsNullOrWhiteSpace(path) || root == null)
                return;

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            // Pretty printed, no XML declaration, UTF-8.
            var content = root.ToString(SaveOptions.None);
            File.WriteAllText(path, content, new UTF8Encoding(false));
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

            // Prefer Name property from known types
            if (source is WCharacter wc && !string.IsNullOrWhiteSpace(wc.Name))
                return wc.Name.Trim();

            if (source is IBaseFaction bf && !string.IsNullOrWhiteSpace(bf.Name))
                return bf.Name.Trim();

            // If a raw string was passed, use it
            if (source is string s && !string.IsNullOrWhiteSpace(s))
                return s.Trim();

            return kind ?? "export";
        }

        static string BuildExportPath(string fileName)
        {
            var safe = SanitizeFileName(fileName);

            if (!safe.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                safe += ".xml";

            // {Documents}/Retinues/Exports/<file>.xml
            return FileSystem.GetPathInRetinuesDocuments(ExportFolderName, safe);
        }

        static string DefaultFileName(string kind, string stringId)
        {
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            return $"{kind}_{stringId}_{stamp}.xml";
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
