using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Retinues.Utilities;

namespace Retinues.Model
{
    /// <summary>
    /// Discovers export files on disk for the Library page.
    /// </summary>
    public static partial class MLibrary
    {
        private const string ExportFolderName = "Exports";

        public static string ExportDirectory =>
            FileSystem.GetPathInRetinuesDocuments(ExportFolderName);

        public static List<Item> GetAll()
        {
            var items = new List<Item>();

            try
            {
                var dir = ExportDirectory;
                if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                    return items;

                var files = Directory.EnumerateFiles(dir, "*.xml", SearchOption.TopDirectoryOnly);

                foreach (var file in files)
                {
                    if (TryRead(file, out var item) && item != null)
                        items.Add(item);
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "MLibrary.GetAll failed.");
            }

            // Factions first, then characters; newest first within each.
            return items
                .OrderByDescending(i => i.Kind == MLibraryKind.Faction)
                .ThenByDescending(i => i.CreatedUtc)
                .ThenBy(i => i.DisplayName)
                .ToList();
        }

        private static bool TryRead(string path, out Item item)
        {
            item = null;

            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return false;

                var fileName = Path.GetFileName(path) ?? string.Empty;

                // Use file time as a fallback if the format doesnt store createdUtc.
                var fileTimeUtc = File.GetLastWriteTimeUtc(path);

                var doc = XDocument.Load(path, LoadOptions.None);
                var root = doc.Root;
                if (root == null)
                    return false;

                var rootName = root.Name.LocalName;

                // Pretty format: <Retinues v="2"> <WCharacter ...> ... </WCharacter> </Retinues>
                if (rootName == "Retinues")
                {
                    var kind = ParseKind((string)root.Attribute("kind"));
                    var source = (string)root.Attribute("source") ?? string.Empty;

                    var first = root.Elements().FirstOrDefault();
                    if (first == null)
                        return false;

                    var type = (string)first.Attribute("type") ?? string.Empty;
                    var firstId =
                        (string)first.Attribute("stringId")
                        ?? (string)first.Attribute("id")
                        ?? string.Empty;

                    if (kind == MLibraryKind.Unknown)
                        kind = GuessKind(first.Name.LocalName, type);

                    var entryCount = root.Elements().Count();

                    var isChar = new Func<XElement, bool>(e =>
                        GuessKind(e.Name.LocalName, (string)e.Attribute("type"))
                        == MLibraryKind.Character
                    );

                    var troopCount = 0;
                    if (kind == MLibraryKind.Faction)
                        troopCount = root.Elements().Count(isChar);

                    var displayName = !string.IsNullOrWhiteSpace(source)
                        ? source
                        : (
                            !string.IsNullOrWhiteSpace(firstId)
                                ? firstId
                                : Path.GetFileNameWithoutExtension(fileName) ?? fileName
                        );

                    item = new Item(
                        filePath: path,
                        fileName: fileName,
                        kind: kind,
                        sourceId: !string.IsNullOrWhiteSpace(source) ? source : firstId,
                        createdUtc: fileTimeUtc,
                        entryCount: entryCount,
                        troopCount: troopCount,
                        displayName: displayName
                    );

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "MLibrary.TryRead failed.");
                return false;
            }
        }

        private static MLibraryKind ParseKind(string kindRaw)
        {
            if (string.IsNullOrWhiteSpace(kindRaw))
                return MLibraryKind.Unknown;

            kindRaw = kindRaw.Trim().ToLowerInvariant();

            return kindRaw switch
            {
                "character" => MLibraryKind.Character,
                "faction" => MLibraryKind.Faction,
                _ => MLibraryKind.Unknown,
            };
        }

        private static MLibraryKind GuessKind(string elementName, string typeAttr)
        {
            // Prefer type attr if present.
            if (!string.IsNullOrWhiteSpace(typeAttr))
            {
                if (typeAttr.Contains(".Characters.WCharacter"))
                    return MLibraryKind.Character;

                if (typeAttr.Contains(".Factions.") || typeAttr.Contains(".Factions.W"))
                    return MLibraryKind.Faction;
            }

            // Fallback on element name.
            if (!string.IsNullOrWhiteSpace(elementName) && elementName.Contains("WCharacter"))
                return MLibraryKind.Character;

            return MLibraryKind.Faction;
        }
    }
}
