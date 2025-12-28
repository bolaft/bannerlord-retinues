using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Retinues.Helpers;
using Retinues.Model;
using Retinues.Model.Characters;
using Retinues.Model.Factions;
using Retinues.Utilities;
using TaleWorlds.Library;
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
                    item => L.T("library_import_missing_file", "Export file was not found.")
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
                EventManager.Fire(UIEvent.Faction);

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
                description: L.T(
                    "library_delete_confirm_desc",
                    "Are you sure you want to delete this export? This action is irreversible."
                )
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
                EventManager.Fire(UIEvent.Library);
                EventManager.Fire(UIEvent.Page);
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
        //                          Edit                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void EditLibraryItem(MLibrary.Item item)
        {
            if (item == null)
                return;

            var path = item.FilePath ?? string.Empty;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                Inquiries.Popup(
                    title: L.T("library_edit_failed_title", "Open Failed"),
                    description: L.T("library_edit_failed_missing", "Export file was not found.")
                );
                return;
            }

            Inquiries.Popup(
                title: L.T("library_edit_confirm_title", "Open Export File"),
                onConfirm: () => ApplyOpenInDefaultEditor(path),
                description: L.T(
                        "library_edit_confirm_desc",
                        "This will open the export file in your default editor.\n\n{PATH}"
                    )
                    .SetTextVariable("PATH", path)
            );
        }

        private static void ApplyOpenInDefaultEditor(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return;

                // 1) Try the Windows "Edit" verb first (often opens in an editor instead of browser).
                if (TryShellVerb(path, "edit"))
                {
                    Log.Info($"Opened export file (verb=edit): {path}");
                    return;
                }

                // 2) Fallback.
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "rundll32.exe",
                        Arguments = $"shell32.dll,OpenAs_RunDLL {Quote(path)}",
                        UseShellExecute = false,
                    }
                );

                Log.Info($"Opened export file: {path}");
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "LibraryController.ApplyOpenInDefaultEditor failed.");
                Inquiries.Popup(
                    title: L.T("library_edit_failed_title", "Open Failed"),
                    description: L.T(
                        "library_edit_failed_exception",
                        "The file could not be opened."
                    )
                );
            }
        }

        private static bool TryShellVerb(string path, string verb)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = path,
                    Verb = verb,
                    UseShellExecute = true,
                };

                Process.Start(psi);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string Quote(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "\"\"";
            if (s.StartsWith("\"") && s.EndsWith("\""))
                return s;
            return "\"" + s.Replace("\"", "\\\"") + "\"";
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Standalone Export                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const string NpcExportModulePrefix = "Retinues.Export.";

        public static EditorAction<MLibrary.Item> ExportNpcCharacters { get; } =
            Action<MLibrary.Item>("ExportNpcCharacters")
                .DefaultTooltip(
                    L.T("library_export_npc_tooltip", "Convert this export into a standalone mod.")
                )
                .AddCondition(
                    item => item != null,
                    L.T("library_export_npc_no_selection", "No export selected.")
                )
                .AddCondition(
                    item => !string.IsNullOrWhiteSpace(item.FilePath) && File.Exists(item.FilePath),
                    item => L.T("library_export_npc_missing_file", "Export file was not found.")
                )
                .ExecuteWith(ExecuteExportNpcCharactersWithConfirm);

        private static void ExecuteExportNpcCharactersWithConfirm(MLibrary.Item item)
        {
            if (item == null)
                return;

            if (
                !TryResolveGameModulesDirectory(out var modulesDir)
                || string.IsNullOrWhiteSpace(modulesDir)
            )
            {
                Inquiries.Popup(
                    title: L.T("library_export_npc_failed_title", "Export Failed"),
                    description: L.T(
                        "library_export_npc_failed_no_modules_dir",
                        "Could not locate the game's Modules folder."
                    )
                );
                return;
            }

            var moduleId = BuildNpcModuleId(item);
            var modRoot = Path.Combine(modulesDir, moduleId);

            var willOverwrite = Directory.Exists(modRoot);

            var title = L.T("library_export_npc_confirm_title", "Export as Mod");
            var desc = willOverwrite
                ? L.T(
                        "library_export_npc_confirm_desc_overwrite",
                        "This will export this library element into a standalone mod, and overwrite existing files:\n\n{PATH}"
                    )
                    .SetTextVariable("PATH", modRoot)
                : L.T(
                        "library_export_npc_confirm_desc",
                        "This will export this library element into a standalone mod:\n\n{PATH}"
                    )
                    .SetTextVariable("PATH", modRoot);

            Inquiries.Popup(
                title: title,
                onConfirm: () => ApplyExportNpcCharacters(item),
                description: desc
            );
        }

        private static void ApplyExportNpcCharacters(MLibrary.Item item)
        {
            try
            {
                if (item == null)
                    return;

                if (
                    !TryResolveGameModulesDirectory(out var modulesDir)
                    || string.IsNullOrWhiteSpace(modulesDir)
                )
                {
                    Inquiries.Popup(
                        title: L.T("library_export_npc_failed_title", "Export Failed"),
                        description: L.T(
                            "library_export_npc_failed_no_modules_dir",
                            "Could not locate the game's Modules folder."
                        )
                    );
                    return;
                }

                if (
                    !TryExtractModelCharacterPayloads(item, out var payloads)
                    || payloads.Count == 0
                )
                {
                    Inquiries.Popup(
                        title: L.T("library_export_npc_failed_title", "Export Failed"),
                        description: L.T(
                            "library_export_npc_failed_no_payloads",
                            "No character payloads were found in this export."
                        )
                    );
                    return;
                }

                var npcStrings = new List<string>();
                var missingVanillaBases = new List<string>();

                foreach (var p in payloads)
                {
                    using var lease = LeaseStubFromPayload(
                        p.Payload,
                        p.ModelStringId,
                        out var missingVanillaBaseId
                    );
                    if (lease == null || lease.Character == null)
                        continue;

                    if (!string.IsNullOrWhiteSpace(missingVanillaBaseId))
                        missingVanillaBases.Add(missingVanillaBaseId);

                    var npcId = !string.IsNullOrWhiteSpace(p.ModelStringId)
                        ? p.ModelStringId
                        : item?.SourceId;
                    npcStrings.Add(lease.Character.ExportAsNPC(npcId));
                }

                if (npcStrings.Count == 0)
                {
                    Inquiries.Popup(
                        title: L.T("library_export_npc_failed_title", "Export Failed"),
                        description: L.T(
                            "library_export_npc_failed_no_output",
                            "No characters could be generated from this export."
                        )
                    );
                    return;
                }

                var moduleId = BuildNpcModuleId(item);
                var paths = BuildNpcModExportPaths(modulesDir, moduleId);

                try
                {
                    WriteNpcCharactersMod(paths, npcStrings);
                }
                catch (UnauthorizedAccessException)
                {
                    // Program Files (x86) is commonly protected.
                    var fallback = FileSystem.GetPathInRetinuesDocuments("GeneratedMods", moduleId);
                    var fallbackPaths = BuildNpcModExportPaths(
                        fallback,
                        moduleId,
                        rootIsAlreadyModuleDir: true
                    );

                    WriteNpcCharactersMod(fallbackPaths, npcStrings);

                    var warn =
                        missingVanillaBases.Count > 0
                            ? "\n\nWarnings:\nSome vanilla baselines could not be resolved (delta exports may be incomplete):\n"
                                + string.Join("\n", missingVanillaBases.Distinct())
                            : string.Empty;

                    var baseDesc = L.T(
                            "library_export_npc_done_desc_fallback",
                            "The game install folder is protected, so the module was written here instead:\n{PATH}\n\nCopy this folder into your Bannerlord Modules directory, then restart the game."
                        )
                        .SetTextVariable(
                            "PATH",
                            Path.GetDirectoryName(fallbackPaths.SubModuleXmlPath) ?? fallback
                        )
                        .ToString();

                    var finalDesc = string.IsNullOrWhiteSpace(warn) ? baseDesc : (baseDesc + warn);

                    Inquiries.Popup(
                        title: L.T("library_export_npc_done_title", "Export Complete"),
                        description: new TextObject(finalDesc)
                    );
                    return;
                }

                {
                    var warn =
                        missingVanillaBases.Count > 0
                            ? "\n\nWarnings:\nSome vanilla baselines could not be resolved (delta exports may be incomplete):\n"
                                + string.Join("\n", missingVanillaBases.Distinct())
                            : string.Empty;

                    var baseDesc = L.T(
                            "library_export_npc_done_desc_mod",
                            "Standalone module written:\n{PATH}\n\nRestart the game to load it."
                        )
                        .SetTextVariable(
                            "PATH",
                            Path.GetDirectoryName(paths.SubModuleXmlPath) ?? paths.ModuleRoot
                        )
                        .ToString();

                    var finalDesc = string.IsNullOrWhiteSpace(warn) ? baseDesc : (baseDesc + warn);

                    Inquiries.Popup(
                        title: L.T("library_export_npc_done_title", "Export Complete"),
                        description: new TextObject(finalDesc)
                    );
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "LibraryController.ApplyExportNpcCharacters failed.");
                Inquiries.Popup(
                    title: L.T("library_export_npc_failed_title", "Export Failed"),
                    description: L.T(
                        "library_export_npc_failed_exception",
                        "The file could not be exported."
                    )
                );
            }
        }

        private readonly struct ModelPayload(string payload, string modelStringId)
        {
            public string Payload => payload;
            public string ModelStringId => modelStringId;
        }

        private static bool TryExtractModelCharacterPayloads(
            MLibrary.Item item,
            out List<ModelPayload> payloads
        )
        {
            payloads = new List<ModelPayload>();

            try
            {
                if (item == null)
                    return false;

                if (string.IsNullOrWhiteSpace(item.FilePath) || !File.Exists(item.FilePath))
                    return false;

                var doc = XDocument.Load(item.FilePath, LoadOptions.None);
                var root = doc.Root;
                if (root == null || root.Name.LocalName != "Retinues")
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

                    // For pure character exports, only one payload is expected.
                    if (item.Kind == MLibraryKind.Character)
                        break;
                }

                return payloads.Count > 0;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "LibraryController.TryExtractModelCharacterPayloads failed.");
                return false;
            }
        }

        private static string ExtractPayload(XElement el)
        {
            if (el == null)
                return string.Empty;

            // Same idea as the library model lease: keep the element, drop uid.
            var copy = new XElement(el);
            copy.SetAttributeValue("uid", null);
            return copy.ToString(SaveOptions.DisableFormatting);
        }

        private static string TryGetModelStringId(XElement el)
        {
            if (el == null)
                return null;

            var id = (string)el.Attribute("stringId") ?? (string)el.Attribute("id");
            if (!string.IsNullOrWhiteSpace(id))
                return id;

            var uid = (string)el.Attribute("uid");
            return TryGetStringIdFromUid(uid);
        }

        private static string TryGetStringIdFromUid(string uid)
        {
            if (string.IsNullOrWhiteSpace(uid))
                return null;

            var sep = uid.IndexOf(':');
            if (sep <= 0 || sep >= uid.Length - 1)
                return null;

            return uid.Substring(sep + 1);
        }

        private readonly struct NpcModPaths(
            string moduleRoot,
            string subModuleXmlPath,
            string charactersXmlPath
        )
        {
            public string ModuleRoot => moduleRoot;
            public string SubModuleXmlPath => subModuleXmlPath;
            public string CharactersXmlPath => charactersXmlPath;
        }

        private static bool TryResolveGameModulesDirectory(out string modulesDir)
        {
            modulesDir = string.Empty;

            try
            {
                // Best option in-game: resolves actual install base (Steam, GOG, custom library, etc.)
                var basePath = BasePath.Name;
                if (!string.IsNullOrWhiteSpace(basePath))
                {
                    var candidate = Path.GetFullPath(Path.Combine(basePath, "Modules"));
                    if (Directory.Exists(candidate))
                    {
                        modulesDir = candidate;
                        return true;
                    }
                }
            }
            catch
            {
                // fall through
            }

            try
            {
                // Fallback: walk up from bin folder until we find "Modules"
                var dir = AppDomain.CurrentDomain.BaseDirectory;
                for (int i = 0; i < 8 && !string.IsNullOrWhiteSpace(dir); i++)
                {
                    var candidate = Path.Combine(dir, "Modules");
                    if (Directory.Exists(candidate))
                    {
                        modulesDir = Path.GetFullPath(candidate);
                        return true;
                    }

                    dir = Directory.GetParent(dir)?.FullName;
                }
            }
            catch
            {
                // fall through
            }

            return false;
        }

        private static string BuildNpcModuleId(MLibrary.Item item)
        {
            // Prefer display name, fallback to source id.
            var raw = item?.DisplayName;
            if (string.IsNullOrWhiteSpace(raw))
                raw = item?.SourceId;

            if (string.IsNullOrWhiteSpace(raw))
                raw = "Export";

            // We want: "Retinues.Export.Nord_Scion"
            // Preserve dots in the prefix, but sanitize the suffix.
            var suffix = SanitizeModuleId(raw); // turns spaces -> '_' and strips invalid chars
            return NpcExportModulePrefix + suffix;
        }

        private static string SanitizeModuleId(string raw)
        {
            raw ??= string.Empty;

            var s = raw.Trim();

            // Convert whitespace to underscores to keep module ids launcher-friendly.
            s = string.Join(
                "_",
                s.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            );

            // Remove invalid filename chars.
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c.ToString(), string.Empty);

            // Keep it conservative: letters, digits, '_' and '-'.
            var chars = s.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                var ch = chars[i];
                var ok =
                    (ch >= 'a' && ch <= 'z')
                    || (ch >= 'A' && ch <= 'Z')
                    || (ch >= '0' && ch <= '9')
                    || ch == '_'
                    || ch == '-';

                if (!ok)
                    chars[i] = '_';
            }

            s = new string(chars);

            // Trim underscores that can accumulate.
            s = s.Trim('_');

            if (string.IsNullOrWhiteSpace(s))
                s = "RetinuesExport";

            return s;
        }

        private static NpcModPaths BuildNpcModExportPaths(
            string modulesDir,
            string moduleId,
            bool rootIsAlreadyModuleDir = false
        )
        {
            // If rootIsAlreadyModuleDir=true, modulesDir is actually the module root container already.
            // Example: fallback folder points to .../GeneratedMods/<moduleId>
            var moduleRoot = rootIsAlreadyModuleDir
                ? modulesDir
                : Path.Combine(modulesDir, moduleId);

            var subModuleXmlPath = Path.Combine(moduleRoot, "SubModule.xml");
            var charactersXmlPath = Path.Combine(moduleRoot, "ModuleData", "characters.xml");

            return new NpcModPaths(moduleRoot, subModuleXmlPath, charactersXmlPath);
        }

        private static void WriteNpcCharactersMod(NpcModPaths paths, List<string> npcElements)
        {
            if (string.IsNullOrWhiteSpace(paths.ModuleRoot))
                return;

            Directory.CreateDirectory(paths.ModuleRoot);
            Directory.CreateDirectory(
                Path.GetDirectoryName(paths.CharactersXmlPath) ?? paths.ModuleRoot
            );

            WriteSubModuleXml(paths.SubModuleXmlPath, Path.GetFileName(paths.ModuleRoot));
            WriteNpcCharactersFile(paths.CharactersXmlPath, npcElements);
        }

        private static void WriteSubModuleXml(string filePath, string moduleId)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(
                    "Module",
                    new XElement("Name", new XAttribute("value", moduleId)),
                    new XElement("Id", new XAttribute("value", moduleId)),
                    new XElement("Version", new XAttribute("value", "v1.0.0")),
                    new XElement("DefaultModule", new XAttribute("value", "false")),
                    new XElement("SingleplayerModule", new XAttribute("value", "true")),
                    new XElement("MultiplayerModule", new XAttribute("value", "false")),
                    new XElement("Official", new XAttribute("value", "false")),
                    new XElement(
                        "Xmls",
                        new XElement(
                            "XmlNode",
                            new XElement(
                                "XmlName",
                                new XAttribute("id", "NPCCharacters"),
                                new XAttribute("path", "characters")
                            ),
                            new XElement(
                                "IncludedGameTypes",
                                new XElement("GameType", new XAttribute("value", "Campaign")),
                                new XElement(
                                    "GameType",
                                    new XAttribute("value", "CampaignStoryMode")
                                ),
                                new XElement("GameType", new XAttribute("value", "CustomGame"))
                            )
                        )
                    )
                )
            );

            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");

            using var fs = File.Create(filePath);
            using var xw = XmlWriter.Create(
                fs,
                new XmlWriterSettings
                {
                    Indent = true,
                    OmitXmlDeclaration = false,
                    Encoding = new System.Text.UTF8Encoding(false),
                }
            );
            doc.Save(xw);
        }

        private static void WriteNpcCharactersFile(string filePath, List<string> npcElements)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            var root = new XElement("NPCCharacters");

            foreach (var s in npcElements)
            {
                if (string.IsNullOrWhiteSpace(s))
                    continue;

                root.Add(XElement.Parse(s));
            }

            var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), root);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");

            // Use an XmlWriter so the file matches your target formatting + utf-8.
            using var fs = File.Create(filePath);
            using var xw = XmlWriter.Create(
                fs,
                new XmlWriterSettings
                {
                    Indent = true,
                    OmitXmlDeclaration = false,
                    Encoding = new System.Text.UTF8Encoding(false),
                }
            );
            doc.Save(xw);
        }

        private sealed class StubLease : IDisposable
        {
            private readonly string _snapshot;

            public WCharacter Character { get; }

            public StubLease(WCharacter character, string snapshot)
            {
                Character = character;
                _snapshot = snapshot;
            }

            public void Dispose()
            {
                try
                {
                    if (Character == null)
                        return;

                    if (!string.IsNullOrWhiteSpace(_snapshot))
                        Character.Deserialize(_snapshot);

                    Character.IsActiveStub = false;
                    Character.MarkAllAttributesClean();
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, "LibraryController.StubLease.Dispose failed.");
                }
            }
        }

        private static StubLease LeaseStubFromPayload(
            string payload,
            string modelStringId,
            out string missingVanillaBaseId
        )
        {
            missingVanillaBaseId = null;

            if (string.IsNullOrWhiteSpace(payload))
                return null;

            var stub = WCharacter.GetFreeStub();
            if (stub == null)
                return null;

            var snapshot = stub.Serialize();

            try
            {
                // If the export was delta-only for a vanilla troop, we need to clone vanilla baseline first.
                if (!string.IsNullOrWhiteSpace(modelStringId))
                {
                    var src = WCharacter.Get(modelStringId);

                    if (src != null && src.IsVanilla)
                        src.Clone(skills: true, equipments: true, intoStub: stub);
                    else
                        missingVanillaBaseId = modelStringId; // export may be incomplete without baseline
                }

                stub.Deserialize(payload);
                stub.HiddenInEncyclopedia = true;

                return new StubLease(stub, snapshot);
            }
            catch
            {
                // If anything goes wrong, restore immediately and release stub.
                try
                {
                    stub.Deserialize(snapshot);
                    stub.IsActiveStub = false;
                    stub.MarkAllAttributesClean();
                }
                catch
                {
                    // ignore
                }

                throw;
            }
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
