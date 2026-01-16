using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.GUI.Services;
using Retinues.Utilities;

namespace Retinues.Framework.Model.Exports
{
    public static partial class MImportExport
    {
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

            if (c.IsHero)
            {
                Notifications.Message("Export failed: heroes cannot be exported.");
                return;
            }

            PromptFileName(
                kind: "character",
                sourceName: c.Name,
                onChosen: fileName =>
                {
                    var path = BuildExportPath(fileName);

                    var root = BuildRoot(kind: "character", sourceId: c.StringId);
                    AddSerialized(root, c.UniqueId, c.SerializeAll());
                    WriteXml(path, root);

                    Notifications.Message(
                        $"Exported '{c.StringId}' to '{Path.GetFileName(path)}'."
                    );
                    Log.Debug($"Exported character '{c.StringId}' to '{path}'.");
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
                sourceName: f.Name,
                onChosen: fileName =>
                {
                    var path = BuildExportPath(fileName);

                    var root = BuildRoot(kind: "faction", sourceId: f.StringId);

                    // Wrapper payload if any (may be empty for some faction types).
                    AddSerialized(root, null, f.SerializeAll());

                    var troops = CollectFactionTroopsWithRosterKeys(f);

                    for (int i = 0; i < troops.Count; i++)
                    {
                        var (t, rosterKey) = troops[i];
                        var added = AddSerialized(root, t.UniqueId, t.SerializeAll());
                        if (added != null && !string.IsNullOrWhiteSpace(rosterKey))
                            added.SetAttributeValue("r", rosterKey);
                    }

                    WriteXml(path, root);

                    Notifications.Message(
                        $"Exported '{f.StringId}' ({troops.Count} troops) to '{Path.GetFileName(path)}'."
                    );
                    Log.Debug(
                        $"Exported faction '{f.StringId}' to '{path}' with {troops.Count + 1} elements."
                    );
                }
            );
        }

        private static List<(
            WCharacter troop,
            string rosterKey
        )> CollectFactionTroopsWithRosterKeys(IBaseFaction f)
        {
            var ordered = new List<(WCharacter, string)>();

            AddMany(ordered, f.RosterRetinues, RRetinues);
            AddMany(ordered, f.RosterElite, RElite);
            AddMany(ordered, f.RosterBasic, RBasic);
            AddMany(ordered, f.RosterMercenary, RMercenary);
            AddMany(ordered, f.RosterMilitia, RMilitia);
            AddMany(ordered, f.RosterCaravan, RCaravan);
            AddMany(ordered, f.RosterVillager, RVillager);
            AddMany(ordered, f.RosterBandit, RBandit);
            AddMany(ordered, f.RosterCivilian, RCivilian);

            // Unique by StringId, keep first occurrence and its roster key.
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var unique = new List<(WCharacter, string)>();

            for (int i = 0; i < ordered.Count; i++)
            {
                var (t, k) = ordered[i];
                var id = t?.StringId;

                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (!seen.Add(id))
                    continue;

                unique.Add((t, k));
            }

            return unique;
        }

        private static void AddMany(
            List<(WCharacter troop, string rosterKey)> list,
            List<WCharacter> troops,
            string rosterKey
        )
        {
            if (list == null || troops == null || troops.Count == 0)
                return;

            for (int i = 0; i < troops.Count; i++)
            {
                var t = troops[i];
                if (t == null)
                    continue;

                list.Add((t, rosterKey));
            }
        }

        private static void PromptFileName(string kind, string sourceName, Action<string> onChosen)
        {
            var placeholder = string.IsNullOrWhiteSpace(sourceName)
                ? (kind ?? "export")
                : sourceName;

            Inquiries.TextInputPopup(
                title: L.T("export_title", "Save"),
                defaultInput: placeholder,
                onConfirm: input =>
                {
                    var baseName = string.IsNullOrWhiteSpace(input) ? placeholder : input;
                    var name = SanitizeFileName(baseName);

                    var path = BuildExportPath(name);

                    if (File.Exists(path))
                    {
                        Inquiries.Popup(
                            title: L.T("export_overwrite_title", "Overwrite file?"),
                            onConfirm: () => onChosen?.Invoke(name),
                            description: L.T(
                                    "export_overwrite_desc",
                                    "A file named '{FILE}' already exists and will be overwritten.\n\n{PATH}"
                                )
                                .SetTextVariable("FILE", Path.GetFileName(path))
                                .SetTextVariable("PATH", path)
                        );

                        return;
                    }

                    onChosen?.Invoke(name);
                },
                description: L.T("export_desc", "Choose a save name:")
            );
        }

        private static string BuildExportPath(string fileName)
        {
            var safe = SanitizeFileName(fileName);

            if (!safe.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                safe += ".xml";

            return FileSystem.GetPathInRetinuesDocuments(ExportFolderName, safe);
        }

        private static string SanitizeFileName(string fileName)
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

        private static void WriteXml(string path, XElement root)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var content = root.ToString(SaveOptions.None);
            XML.WriteAllTextUtf8NoBom(path, content);
        }
    }
}
