using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Retinues.Helpers;
using Retinues.Model;
using Retinues.Model.Characters;
using Retinues.Model.Factions;
using Retinues.Utilities;
using TaleWorlds.Localization;

namespace Retinues.Editor.Controllers.Library
{
    /// <summary>
    /// Non-view logic for library import/export.
    /// </summary>
    public class LibraryController : EditorController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Actions                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<MLibrary.Item> Import { get; } =
            Action<MLibrary.Item>("ImportLibraryItem")
                .DefaultTooltip(L.T("library_import_tooltip", "Import into the current game."))
                .AddCondition(
                    item => item != null,
                    L.T("library_import_no_selection", "No export selected.")
                )
                .AddCondition(
                    item => !string.IsNullOrWhiteSpace(item.FilePath) && File.Exists(item.FilePath),
                    item =>
                        L.T("library_import_missing_file", "Export file was not found.")
                            .SetTextVariable("PATH", item?.FilePath ?? "")
                )
                .AddCondition(CanResolveTarget, BuildCantResolveTargetReason)
                .AddCondition(
                    item => !IsDeletedCustomTroopTarget(item),
                    L.T(
                        "library_import_deleted_custom_troop",
                        "The troop corresponding to this export was deleted."
                    )
                )
                .ExecuteWith(ExecuteImportWithConfirm);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Public                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static string GetName(MLibrary.Item item) => item?.DisplayName ?? string.Empty;

        public static string GetExportPath(MLibrary.Item item) => item?.FilePath ?? string.Empty;

        public static string GetTypeText(MLibrary.Item item)
        {
            if (item == null)
                return string.Empty;

            return item.Kind switch
            {
                MLibraryKind.Character => L.T("library_kind_troop", "Troop").ToString(),
                MLibraryKind.Faction => L.T("library_kind_faction", "Faction").ToString(),
                _ => L.T("library_kind_unknown", "Unknown").ToString(),
            };
        }

        public static string GetTroopNameFromFile(MLibrary.Item item)
        {
            if (!TryReadTroopNames(item, out var names) || names.Count == 0)
                return string.Empty;

            // For character export, there should be a single character entry, but we still take the first.
            return names[0] ?? string.Empty;
        }

        public static List<string> GetFactionTroopNamesFromFile(MLibrary.Item item)
        {
            if (!TryReadTroopNames(item, out var names) || names.Count == 0)
                return [];

            return names;
        }

        public static string GetTargetName(MLibrary.Item item)
        {
            if (item == null || item.Kind != MLibraryKind.Character)
                return string.Empty;

            var id = item.SourceId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id))
                return string.Empty;

            var c = WCharacter.Get(id);
            return c?.Name?.ToString() ?? string.Empty;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Import impl                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void ExecuteImportWithConfirm(MLibrary.Item item)
        {
            if (item == null)
                return;

            var title = L.T("library_import_confirm_title", "Import");
            var desc = item.Kind switch
            {
                MLibraryKind.Character => L.T(
                    "library_import_confirm_desc_char",
                    "This will overwrite the matching troop in the current game."
                ),

                MLibraryKind.Faction => L.T(
                    "library_import_confirm_desc_faction",
                    "This will overwrite the matching faction in the current game."
                ),

                _ => L.T(
                    "library_import_confirm_desc_unknown",
                    "This will import the data into the current game."
                ),
            };

            Inquiries.Popup(title: title, onConfirm: () => ApplyImport(item), description: desc);
        }

        private static void ApplyImport(MLibrary.Item item)
        {
            try
            {
                if (item == null)
                    return;

                if (string.IsNullOrWhiteSpace(item.FilePath) || !File.Exists(item.FilePath))
                {
                    Inquiries.Popup(
                        title: L.T("library_import_failed_title", "Import Failed"),
                        description: L.T(
                            "library_import_failed_missing",
                            "Export file was not found."
                        )
                    );
                    return;
                }

                bool ok;
                string err;

                switch (item.Kind)
                {
                    case MLibraryKind.Character:
                        ok = MImportExport.TryImportCharacter(item.FilePath, out err);
                        break;

                    case MLibraryKind.Faction:
                        ok = MImportExport.TryImportFaction(item.FilePath, out err);
                        break;

                    default:
                        Inquiries.Popup(
                            title: L.T("library_import_failed_title", "Import Failed"),
                            description: L.T(
                                "library_import_failed_unknown_kind",
                                "Unrecognized export type."
                            )
                        );
                        return;
                }

                if (!ok)
                {
                    Inquiries.Popup(
                        title: L.T("library_import_failed_title", "Import Failed"),
                        description: L.T(
                            "library_import_failed_reason",
                            "The file could not be imported."
                        )
                    );
                    return;
                }

                // Refresh state selections.
                State.Instance.ForceRefreshSelection();

                // Refresh any faction-related data.
                EventManager.Fire(UIEvent.Faction, EventScope.Global);

                Inquiries.Popup(
                    title: L.T("library_import_done_title", "Import Complete"),
                    description: L.T("library_import_done_desc", "Import successful.")
                );
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "LibraryController.ApplyImport failed.");
                Inquiries.Popup(
                    title: L.T("library_import_failed_title", "Import Failed"),
                    description: L.T(
                        "library_import_failed_exception",
                        "The file could not be imported."
                    )
                );
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Delete                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void DeleteLibraryItem(MLibrary.Item item)
        {
            if (item == null)
                return;

            var path = item.FilePath ?? string.Empty;

            Inquiries.Popup(
                title: L.T("library_delete_confirm_title", "Delete Export"),
                onConfirm: () => ApplyDelete(item),
                description: L.T("library_delete_confirm_desc", "This action is irreversible.")
            );
        }

        private static void ApplyDelete(MLibrary.Item item)
        {
            try
            {
                if (item == null)
                    return;

                var path = item.FilePath ?? string.Empty;
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    Inquiries.Popup(
                        title: L.T("library_delete_failed_title", "Delete Failed"),
                        description: L.T(
                            "library_delete_failed_missing",
                            "Export file was not found."
                        )
                    );

                    RefreshLibraryAfterChange(item);
                    return;
                }

                File.Delete(path);

                Inquiries.Popup(
                    title: L.T("library_delete_done_title", "Export Deleted"),
                    description: L.T("library_delete_done_desc", "The export file was deleted.")
                );

                RefreshLibraryAfterChange(item);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "LibraryController.ApplyDelete failed.");
                Inquiries.Popup(
                    title: L.T("library_delete_failed_title", "Delete Failed"),
                    description: L.T(
                        "library_delete_failed_exception",
                        "The export could not be deleted."
                    )
                );
            }
        }

        private static void RefreshLibraryAfterChange(MLibrary.Item item)
        {
            try
            {
                // Clear selection if we deleted the selected entry.
                var selected = State.Instance.LibraryItem;
                if (
                    selected != null
                    && item != null
                    && string.Equals(
                        selected.FilePath,
                        item.FilePath,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    State.Instance.LibraryItem = null;
                }

                // Ask any list VMs to rebuild from disk.
                EventManager.Fire(UIEvent.Library, EventScope.Global);
                EventManager.Fire(UIEvent.Page, EventScope.Global);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "LibraryController.RefreshLibraryAfterChange failed.");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Disable Reasons                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool CanResolveTarget(MLibrary.Item item)
        {
            if (item == null)
                return false;

            var id = item.SourceId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id))
                return false;

            if (item.Kind == MLibraryKind.Character)
                return WCharacter.Get(id) != null;

            if (item.Kind == MLibraryKind.Faction)
                return ResolveFaction(id) != null;

            return false;
        }

        private static TextObject BuildCantResolveTargetReason(MLibrary.Item item)
        {
            if (item == null)
                return L.T("library_import_no_selection", "No export selected.");

            return item.Kind switch
            {
                MLibraryKind.Character => L.T(
                    "library_import_target_missing_troop",
                    "The target troop does not exist in the current game."
                ),

                MLibraryKind.Faction => L.T(
                    "library_import_target_missing_faction",
                    "The target faction does not exist in the current game."
                ),

                _ => L.T(
                    "library_import_target_missing_unknown",
                    "The target of this export does not exist in the current game."
                ),
            };
        }

        private static bool IsDeletedCustomTroopTarget(MLibrary.Item item)
        {
            if (item.Kind != MLibraryKind.Character)
                return false; // Only applies to troop exports.

            if (item == null)
                return true; // Can't verify, assume deleted.

            var id = item.SourceId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id))
                return true; // Can't verify, assume deleted.

            var c = WCharacter.Get(id);
            if (c == null)
                return true; // Troop not found, assume deleted.

            if (c.IsCustom && !c.IsActiveStub)
                return true; // Troop is an inactive stub.

            if (c.HiddenInEncyclopedia)
                return true; // Troop is hidden, assume deleted.

            return false;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        File Reads                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool TryReadTroopNames(MLibrary.Item item, out List<string> troopNames)
        {
            troopNames = [];

            try
            {
                if (item == null)
                    return false;

                if (string.IsNullOrWhiteSpace(item.FilePath) || !File.Exists(item.FilePath))
                    return false;

                var doc = XDocument.Load(item.FilePath, LoadOptions.None);
                var root = doc.Root;
                if (root == null)
                    return false;

                // Pretty format: <Retinues ...> <W.../> <WCharacter .../> ... </Retinues>
                if (root.Name.LocalName == "Retinues")
                {
                    foreach (var el in root.Elements())
                    {
                        if (!IsCharacterElement(el))
                            continue;

                        var n = ReadCharacterName(el);
                        if (!string.IsNullOrWhiteSpace(n))
                            troopNames.Add(n);
                    }

                    return troopNames.Count > 0;
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "LibraryController.TryReadTroopNames failed.");
            }

            return false;
        }

        private static bool IsCharacterElement(XElement el)
        {
            if (el == null)
                return false;

            var name = el.Name.LocalName ?? string.Empty;

            if (string.Equals(name, "WCharacter", StringComparison.OrdinalIgnoreCase))
                return true;

            var type = (string)el.Attribute("type") ?? string.Empty;
            return type.IndexOf("WCharacter", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ReadCharacterName(XElement el)
        {
            if (el == null)
                return string.Empty;

            // Prefer NameAttribute inner value if present.
            var nameEl = el.Elements().FirstOrDefault(e => e.Name.LocalName == "NameAttribute");
            if (nameEl != null)
                return ResolveTextObjectString(nameEl.Value);

            // Fallback to attribute.
            var attr = (string)el.Attribute("name") ?? string.Empty;
            return ResolveTextObjectString(attr);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Shared Helpers                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static IBaseFaction ResolveFaction(string stringId)
        {
            if (string.IsNullOrWhiteSpace(stringId))
                return null;

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

        private static string ResolveTextObjectString(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            // If it's already a plain string (most common), keep it.
            // If it looks like a TextObject literal, evaluate it for the current locale.
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
    }
}
