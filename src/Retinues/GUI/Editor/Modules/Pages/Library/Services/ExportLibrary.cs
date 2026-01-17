using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Utilities;

namespace Retinues.GUI.Editor.Modules.Pages.Library.Services
{
    /// <summary>
    /// Discovers export files on disk for the Library page.
    /// </summary>
    public static partial class ExportLibrary
    {
        private const string ExportFolderName = "Exports";

        public static string ExportDirectory =>
            FileSystem.GetPathInRetinuesDocuments(ExportFolderName);

        /// <summary>
        /// Gets all export files in the Library export directory.
        /// </summary>
        public static List<Entry> GetAll()
        {
            var items = new List<Entry>();

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
                Log.Exception(ex, "ExportLibrary.GetAll failed.");
            }

            return
            [
                .. items
                    .OrderByDescending(i => i.Kind == ExportKind.Faction)
                    .ThenByDescending(i => i.CreatedUtc)
                    .ThenBy(i => i.DisplayName),
            ];
        }

        /// <summary>
        /// Reads export metadata from a file path.
        /// </summary>
        private static bool TryRead(string path, out Entry item)
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

                // Current export format root.
                if (root.Name.LocalName != "Retinues")
                    return false;

                var kind = ParseKind((string)root.Attribute("kind"));
                var sourceId = (string)root.Attribute("source") ?? string.Empty;

                var elements = root.Elements().ToList();
                if (elements.Count == 0)
                    return false;

                // Fallback for older/invalid exports.
                if (kind == ExportKind.Unknown)
                    kind = elements.Any(e => !ExportXMLReader.IsCharacterElement(e))
                        ? ExportKind.Faction
                        : ExportKind.Character;

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
                if (kind == ExportKind.Faction)
                    troopCount = elements.Count(ExportXMLReader.IsCharacterElement);

                // Wrapper element (critical for faction exports: must be NON-character).
                var wrapperEl = GetWrapperElement(elements, kind);

                // Read exported XML name from the wrapper element (may be null).
                var exportedNameRaw = TryReadExportedDisplayName(wrapperEl);
                var exportedName = ExportXMLReader.ResolveTextObjectString(exportedNameRaw);

                // Resolve current in-game name from sourceId (may be null).
                var currentName = ResolveNameFromGame(kind, sourceId);

                string displayName;

                if (kind == ExportKind.Character)
                {
                    // Prefer exported XML NameAttribute; fallback to current in-game name.
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

                item = new Entry(
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
                Log.Exception(ex, "ExportLibrary.TryRead failed.");
                return false;
            }
        }

        /// <summary>
        /// Resolves an export display name from the game data.
        /// </summary>
        private static string ResolveNameFromGame(ExportKind kind, string sourceId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourceId))
                    return string.Empty;

                if (kind == ExportKind.Character)
                {
                    var c = WCharacter.Get(sourceId);
                    return ExportXMLReader.ResolveTextObjectString(c?.Name?.ToString());
                }

                if (kind == ExportKind.Faction)
                {
                    var f = ResolveFaction(sourceId);
                    return ExportXMLReader.ResolveTextObjectString(f?.Name?.ToString());
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Resolves a faction (IBaseFaction) from a string ID.
        /// </summary>
        private static IBaseFaction ResolveFaction(string factionStringId)
        {
            if (string.IsNullOrWhiteSpace(factionStringId))
                return null;

            return WClan.Get(factionStringId) as IBaseFaction
                ?? WKingdom.Get(factionStringId) as IBaseFaction
                ?? WCulture.Get(factionStringId);
        }

        /// <summary>
        /// Gets the wrapper XML element for an export.
        /// </summary>
        private static XElement GetWrapperElement(List<XElement> elements, ExportKind kind)
        {
            if (elements == null || elements.Count == 0)
                return null;

            if (kind == ExportKind.Faction)
            {
                // MUST be the faction wrapper: first NON-character element.
                var f = elements.FirstOrDefault(e => !ExportXMLReader.IsCharacterElement(e));
                return f ?? elements[0];
            }

            if (kind == ExportKind.Character)
            {
                // For character exports, the wrapper is the character element.
                var c = elements.FirstOrDefault(ExportXMLReader.IsCharacterElement);
                return c ?? elements[0];
            }

            return elements[0];
        }

        /// <summary>
        /// Reads the character name from a character XML element.
        /// </summary>
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

        /// <summary>
        /// Parses an ExportKind from a raw string.
        /// </summary>
        private static ExportKind ParseKind(string kindRaw)
        {
            if (string.IsNullOrWhiteSpace(kindRaw))
                return ExportKind.Unknown;

            kindRaw = kindRaw.Trim().ToLowerInvariant();

            return kindRaw switch
            {
                "character" => ExportKind.Character,
                "faction" => ExportKind.Faction,
                _ => ExportKind.Unknown,
            };
        }
    }
}
