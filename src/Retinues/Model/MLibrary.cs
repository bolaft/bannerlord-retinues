using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Retinues.Model.Characters;
using Retinues.Model.Factions;
using Retinues.Utilities;
using TaleWorlds.Localization;

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

            return
            [
                .. items
                    .OrderByDescending(i => i.Kind == MLibraryKind.Faction)
                    .ThenByDescending(i => i.CreatedUtc)
                    .ThenBy(i => i.DisplayName),
            ];
        }

        private static bool TryRead(string path, out Item item)
        {
            item = null;

            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return false;

                var fileName = Path.GetFileName(path) ?? string.Empty;
                var fileTimeUtc = File.GetLastWriteTimeUtc(path);

                var doc = XDocument.Load(path, LoadOptions.None);
                var root = doc.Root;
                if (root == null)
                    return false;

                if (root.Name.LocalName != "Retinues")
                    return false;

                var kind = ParseKind((string)root.Attribute("kind"));
                var sourceId = (string)root.Attribute("source") ?? string.Empty;

                var elements = root.Elements().ToList();
                if (elements.Count == 0)
                    return false;

                // Fallback for older/invalid exports.
                if (kind == MLibraryKind.Unknown)
                    kind = elements.Any(e => !IsCharacterElement(e))
                        ? MLibraryKind.Faction
                        : MLibraryKind.Character;

                if (string.IsNullOrWhiteSpace(sourceId))
                {
                    var first = elements[0];
                    sourceId =
                        (string)first.Attribute("stringId")
                        ?? (string)first.Attribute("id")
                        ?? string.Empty;
                }

                var entryCount = elements.Count;

                var troopCount = 0;
                if (kind == MLibraryKind.Faction)
                    troopCount = elements.Count(IsCharacterElement);

                // Wrapper element (critical for faction exports: must be NON-character).
                var wrapperEl = GetWrapperElement(elements, kind);

                // Read exported XML name from the wrapper element (may be null).
                var exportedNameRaw = TryReadExportedDisplayName(wrapperEl);
                var exportedName = ResolveTextObjectString(exportedNameRaw);

                // Resolve current in-game name from sourceId (may be null).
                var currentName = ResolveNameFromGame(kind, sourceId);

                string displayName;

                if (kind == MLibraryKind.Character)
                {
                    // NEW behavior applies ONLY to character exports:
                    // Prefer the exported XML NameAttribute; fallback to current in-game name.
                    displayName = !string.IsNullOrWhiteSpace(exportedName)
                        ? exportedName
                        : (
                            !string.IsNullOrWhiteSpace(currentName)
                                ? currentName
                                : (
                                    !string.IsNullOrWhiteSpace(sourceId)
                                        ? sourceId
                                        : (Path.GetFileNameWithoutExtension(fileName) ?? fileName)
                                )
                        );
                }
                else
                {
                    // Faction exports: NEVER pick a troop name.
                    // Prefer current in-game faction name (sourceId), then fallback to XML wrapper name.
                    displayName = !string.IsNullOrWhiteSpace(currentName)
                        ? currentName
                        : (
                            !string.IsNullOrWhiteSpace(exportedName)
                                ? exportedName
                                : (
                                    !string.IsNullOrWhiteSpace(sourceId)
                                        ? sourceId
                                        : (Path.GetFileNameWithoutExtension(fileName) ?? fileName)
                                )
                        );
                }

                item = new Item(
                    filePath: path,
                    fileName: fileName,
                    kind: kind,
                    sourceId: sourceId,
                    createdUtc: fileTimeUtc,
                    entryCount: entryCount,
                    troopCount: troopCount,
                    displayName: displayName
                );

                return true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "MLibrary.TryRead failed.");
                return false;
            }
        }

        private static string ResolveNameFromGame(MLibraryKind kind, string sourceId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourceId))
                    return string.Empty;

                if (kind == MLibraryKind.Character)
                {
                    var c = WCharacter.Get(sourceId);
                    return ResolveTextObjectString(c?.Name?.ToString());
                }

                if (kind == MLibraryKind.Faction)
                {
                    var f = ResolveFaction(sourceId);
                    return ResolveTextObjectString(f?.Name?.ToString());
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static IBaseFaction ResolveFaction(string factionStringId)
        {
            if (string.IsNullOrWhiteSpace(factionStringId))
                return null;

            return WClan.Get(factionStringId) as IBaseFaction
                ?? WKingdom.Get(factionStringId) as IBaseFaction
                ?? WCulture.Get(factionStringId);
        }

        private static XElement GetWrapperElement(List<XElement> elements, MLibraryKind kind)
        {
            if (elements == null || elements.Count == 0)
                return null;

            if (kind == MLibraryKind.Faction)
            {
                // MUST be the faction wrapper: first NON-character element.
                var f = elements.FirstOrDefault(e => !IsCharacterElement(e));
                return f ?? elements[0];
            }

            if (kind == MLibraryKind.Character)
            {
                // For character exports, the wrapper is the character element.
                var c = elements.FirstOrDefault(IsCharacterElement);
                return c ?? elements[0];
            }

            return elements[0];
        }

        private static bool IsCharacterElement(XElement el)
        {
            if (el == null)
                return false;

            var type = (string)el.Attribute("type");
            if (!string.IsNullOrWhiteSpace(type) && type.Contains(".Characters.WCharacter"))
                return true;

            return el.Name.LocalName.Contains("WCharacter");
        }

        private static string TryReadExportedDisplayName(XElement wrapperEl)
        {
            try
            {
                if (wrapperEl == null)
                    return null;

                var nameEl =
                    wrapperEl.Element("NameAttribute")
                    ?? wrapperEl.Descendants("NameAttribute").FirstOrDefault();

                var raw = nameEl?.Value?.Trim();
                return string.IsNullOrWhiteSpace(raw) ? null : raw;
            }
            catch
            {
                return null;
            }
        }

        private static string ResolveTextObjectString(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            raw = raw.Trim();

            if (raw.Length >= 4 && raw[0] == '{' && raw[1] == '=')
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
    }
}
