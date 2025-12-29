using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.Services.Library;
using Retinues.Editor.Services.Library.NPCCharacters;
using Retinues.Framework.Model.Exports;
using Retinues.Modules.Submods;
using Retinues.UI.Services;
using Retinues.Utilities;
using TaleWorlds.Localization;

namespace Retinues.Editor.Controllers.Library
{
    /// <summary>
    /// Library screen operations (import/export/delete/edit).
    /// UI-facing behavior (popups, TextObjects, messaging) stays here.
    /// Pure plumbing stays in services/helpers.
    /// </summary>
    public class LibraryController : BaseController
    {
        private const string NpcExportModulePrefix = "Retinues.Export.";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Import                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<MLibrary.Item> Import { get; } =
            Action<MLibrary.Item>("ImportLibraryItem")
                .DefaultTooltip(L.T("library_import_tooltip", "Import into the current game."))
                .AddCondition(
                    item => item != null,
                    L.T("library_import_no_selection", "No export selected.")
                )
                .AddCondition(
                    item => HasExistingFile(item),
                    _ => L.T("library_import_missing_file", "Export file was not found.")
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
        //                 Convert to NPCCharacters               //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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
                    item => HasExistingFile(item),
                    _ => L.T("library_export_npc_missing_file", "Export file was not found.")
                )
                .AddCondition(
                    item =>
                        item.Kind == MLibraryKind.Character || item.Kind == MLibraryKind.Faction,
                    L.T(
                        "library_export_npc_kind_unsupported",
                        "This export type cannot be converted."
                    )
                )
                .AddCondition(
                    item =>
                    {
                        if (item.Kind != MLibraryKind.Character)
                            return true;

                        var id = item.SourceId ?? string.Empty;
                        return string.IsNullOrWhiteSpace(id)
                            || !id.StartsWith(WCharacter.CustomTroopPrefix);
                    },
                    L.T(
                        "library_export_npc_custom_troop_unsupported",
                        "Only vanilla troop edits can be converted to standalone mods."
                    )
                )
                .ExecuteWith(ExecuteExportNpcCharactersWithConfirm);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                           Edit                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<MLibrary.Item> Edit { get; } =
            Action<MLibrary.Item>("EditLibraryItem")
                .DefaultTooltip(
                    L.T("library_edit_tooltip", "Directly edit this export's XML file contents.")
                )
                .AddCondition(
                    item => item != null,
                    L.T("library_edit_no_selection", "No export selected.")
                )
                .AddCondition(
                    item => HasExistingFile(item),
                    _ => L.T("library_edit_failed_missing_file", "Export file was not found.")
                )
                .ExecuteWith(ExecuteEditWithConfirm);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Delete                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<MLibrary.Item> Delete { get; } =
            Action<MLibrary.Item>("DeleteLibraryItem")
                .DefaultTooltip(
                    L.T(
                        "library_delete_tooltip",
                        "Permanently deletes this library item and associated XML file."
                    )
                )
                .AddCondition(
                    item => item != null,
                    L.T("library_delete_no_selection", "No export selected.")
                )
                .AddCondition(
                    item => HasExistingFile(item),
                    _ => L.T("library_delete_failed_missing_file", "Export file was not found.")
                )
                .ExecuteWith(ExecuteDeleteWithConfirm);

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

        public static List<string> GetFactionTroopNamesFromFile(MLibrary.Item item)
        {
            if (!LibraryFileReader.TryReadTroopNames(item, out var names) || names.Count == 0)
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
            return c?.Name ?? string.Empty;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Import helpers                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void ExecuteImportWithConfirm(MLibrary.Item item)
        {
            if (item == null)
                return;

            var desc = item.Kind switch
            {
                MLibraryKind.Character => L.T(
                    "library_import_confirm_desc_char",
                    "This will import the exported troop into the current game.\n\nContinue?"
                ),

                MLibraryKind.Faction => L.T(
                    "library_import_confirm_desc_faction",
                    "This will import the exported faction into the current game.\n\nContinue?"
                ),

                _ => L.T(
                    "library_import_confirm_desc_unknown",
                    "This will import the exported data into the current game.\n\nContinue?"
                ),
            };

            Inquiries.Popup(
                title: L.T("library_import_confirm_title", "Import"),
                description: desc,
                onConfirm: () => ApplyImport(item)
            );
        }

        private static void ApplyImport(MLibrary.Item item)
        {
            try
            {
                if (item == null)
                    return;

                if (!HasExistingFile(item))
                {
                    Inquiries.Popup(
                        title: L.T("library_import_failed_title", "Import Failed"),
                        description: L.T(
                            "library_import_failed_missing_file",
                            "Export file was not found."
                        )
                    );
                    return;
                }

                var path = item.FilePath ?? string.Empty;

                var ok = false;

                if (item.Kind == MLibraryKind.Character)
                    ok = MImportExport.TryImportCharacter(path, out _);

                if (item.Kind == MLibraryKind.Faction)
                    ok = MImportExport.TryImportFaction(path, out _);

                if (!ok)
                {
                    Inquiries.Popup(
                        title: L.T("library_import_failed_title", "Import Failed"),
                        description: L.T(
                            "library_import_failed_invalid",
                            "The export could not be imported."
                        )
                    );
                    return;
                }

                // Refresh UI.
                EventManager.Fire(UIEvent.Tree);
                EventManager.Fire(UIEvent.Library);
                EventManager.Fire(UIEvent.Page);
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
        //                      Target guards                     //
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
                return MImportExport.ResolveFaction(id) != null;

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
            if (item == null)
                return true; // Can't verify, assume deleted.

            if (item.Kind != MLibraryKind.Character)
                return false; // Only applies to troop exports.

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
        //                       Edit helpers                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void ExecuteEditWithConfirm(MLibrary.Item item)
        {
            if (item == null)
                return;

            if (!HasExistingFile(item))
            {
                Inquiries.Popup(
                    title: L.T("library_edit_failed_title", "Edit Failed"),
                    description: L.T(
                        "library_edit_failed_missing_file",
                        "Export file was not found."
                    )
                );
                return;
            }

            var path = item.FilePath ?? string.Empty;

            Inquiries.Popup(
                title: L.T("library_edit_confirm_title", "Edit Export"),
                description: L.T(
                    "library_edit_confirm_desc",
                    "This will open the export XML in your default editor.\n\nContinue?"
                ),
                onConfirm: () => ApplyEdit(path)
            );
        }

        private static void ApplyEdit(string path)
        {
            try
            {
                Shell.OpenForEdit(path);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "LibraryController.ApplyEdit failed.");
                Inquiries.Popup(
                    title: L.T("library_edit_failed_title", "Edit Failed"),
                    description: L.T(
                        "library_edit_failed_exception",
                        "The file could not be opened."
                    )
                );
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Delete helpers                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void ExecuteDeleteWithConfirm(MLibrary.Item item)
        {
            if (item == null)
                return;

            Inquiries.Popup(
                title: L.T("library_delete_confirm_title", "Delete Export"),
                description: L.T(
                    "library_delete_confirm_desc",
                    "This will permanently delete the export file.\n\nContinue?"
                ),
                onConfirm: () => ApplyDelete(item)
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
                        "The file could not be deleted."
                    )
                );
            }
        }

        private static void RefreshLibraryAfterChange(MLibrary.Item item)
        {
            try
            {
                // Clear selection if we deleted the selected entry.
                var selected = EditorState.Instance.LibraryItem;
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
                    EditorState.Instance.LibraryItem = null;
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
        //               Standalone NPC export helpers            //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void ExecuteExportNpcCharactersWithConfirm(MLibrary.Item item)
        {
            if (item == null)
                return;

            if (
                !SubmodEnvironment.TryGetGameModulesDirectory(out var modulesDir)
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
            var moduleRoot = Path.Combine(modulesDir, moduleId);

            if (Directory.Exists(moduleRoot))
            {
                Inquiries.Popup(
                    title: L.T("library_export_npc_overwrite_title", "Overwrite Module"),
                    description: L.T(
                            "library_export_npc_overwrite_desc",
                            "A module with this name already exists:\n{PATH}\n\nOverwrite it?"
                        )
                        .SetTextVariable("PATH", moduleRoot),
                    onConfirm: () => ApplyExportNpcCharacters(item)
                );
                return;
            }

            ApplyExportNpcCharacters(item);
        }

        private static void ApplyExportNpcCharacters(MLibrary.Item item)
        {
            try
            {
                if (item == null)
                    return;

                if (
                    !SubmodEnvironment.TryGetGameModulesDirectory(out var modulesDir)
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
                    !LibraryExportPayloadReader.TryExtractModelCharacterPayloads(
                        item,
                        out var payloads
                    )
                    || payloads.Count == 0
                )
                {
                    Inquiries.Popup(
                        title: L.T("library_export_npc_failed_title", "Export Failed"),
                        description: L.T(
                            "library_export_npc_failed_no_payload",
                            "Nothing could be exported from this file."
                        )
                    );
                    return;
                }

                var npcStrings = new List<string>();
                var npcIds = new List<string>();
                var missingVanillaBases = new List<string>();

                foreach (var p in payloads)
                {
                    using var lease = CharacterStubLeaser.LeaseFromPayload(
                        p.Payload,
                        p.ModelStringId,
                        out var missingVanillaBaseId
                    );

                    if (lease == null || lease.Character == null)
                        continue;

                    if (!string.IsNullOrWhiteSpace(missingVanillaBaseId))
                        missingVanillaBases.Add(missingVanillaBaseId);

                    if (
                        !TryBuildNpcCharacterXml(
                            lease.Character,
                            p.ModelStringId,
                            out var npcXml,
                            out var npcId
                        )
                    )
                        continue;

                    npcStrings.Add(npcXml);
                    npcIds.Add(npcId);
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

                var project = NpcCharactersSubmodBuilder.BuildNpcCharactersSubmodProject(
                    moduleId,
                    npcStrings,
                    npcIds
                );

                var writer = new SubmodWriter();

                // First try to write to the game's Modules folder.
                var moduleRoot = Path.Combine(modulesDir, moduleId);
                var result = writer.WriteToDirectory(moduleRoot, project, overwrite: true);

                if (!result.Success && result.Exception is UnauthorizedAccessException)
                {
                    // Program Files (x86) is commonly protected.
                    var fallbackRoot = FileSystem.GetPathInRetinuesDocuments(
                        "GeneratedMods",
                        moduleId
                    );
                    var fallbackResult = writer.WriteToDirectory(
                        fallbackRoot,
                        project,
                        overwrite: true
                    );

                    if (!fallbackResult.Success)
                    {
                        Inquiries.Popup(
                            title: L.T("library_export_npc_failed_title", "Export Failed"),
                            description: L.T(
                                "library_export_npc_failed_exception",
                                "The file could not be exported."
                            )
                        );
                        return;
                    }

                    var warn =
                        missingVanillaBases.Count > 0
                            ? "\n\nWarnings:\nSome vanilla baselines could not be resolved (delta exports may be incomplete):\n"
                                + string.Join("\n", missingVanillaBases.Distinct())
                            : string.Empty;

                    var baseDesc = L.T(
                            "library_export_npc_done_desc_fallback",
                            "The game install folder is protected, so the module was written here instead:\n{PATH}\n\nCopy this folder into your Bannerlord Modules directory, then restart the game."
                        )
                        .SetTextVariable("PATH", fallbackRoot)
                        .ToString();

                    Inquiries.Popup(
                        title: L.T("library_export_npc_done_title", "Export Complete"),
                        description: new TextObject(baseDesc + warn)
                    );

                    return;
                }

                if (!result.Success)
                {
                    Inquiries.Popup(
                        title: L.T("library_export_npc_failed_title", "Export Failed"),
                        description: L.T(
                            "library_export_npc_failed_exception",
                            "The file could not be exported."
                        )
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
                        .SetTextVariable("PATH", moduleRoot)
                        .ToString();

                    Inquiries.Popup(
                        title: L.T("library_export_npc_done_title", "Export Complete"),
                        description: new TextObject(baseDesc + warn)
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

        private static bool TryBuildNpcCharacterXml(
            WCharacter character,
            string overrideId,
            out string npcXml,
            out string npcId
        )
        {
            npcXml = null;
            npcId = null;

            if (character == null)
                return false;

            npcId = !string.IsNullOrWhiteSpace(overrideId) ? overrideId : character.StringId;
            if (string.IsNullOrWhiteSpace(npcId))
                return false;

            npcXml = character.ExportAsNPC(npcId);
            return !string.IsNullOrWhiteSpace(npcXml);
        }

        private static string BuildNpcModuleId(MLibrary.Item item)
        {
            // Unique-enough id based on export's source id.
            var raw = item?.SourceId;

            if (string.IsNullOrWhiteSpace(raw))
                raw = item?.DisplayName;

            if (string.IsNullOrWhiteSpace(raw))
                raw = item?.SourceId;

            if (string.IsNullOrWhiteSpace(raw))
                raw = "Export";

            var suffix = SubmodEnvironment.SanitizeModuleId(raw, fallback: "Export");
            return NpcExportModulePrefix + suffix;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Shared helpers                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool HasExistingFile(MLibrary.Item item)
        {
            var path = item?.FilePath ?? string.Empty;
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        }
    }
}
