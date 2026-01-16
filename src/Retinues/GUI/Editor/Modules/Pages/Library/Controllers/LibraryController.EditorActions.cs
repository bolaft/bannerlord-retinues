using Retinues.Domain.Characters.Wrappers;
using Retinues.Framework.Model.Exports;
using Retinues.GUI.Editor.Shared.Controllers;
using Retinues.GUI.Services;

namespace Retinues.GUI.Editor.Controllers.Library
{
    public partial class LibraryController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Import                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Imports a library export into the current game.
        /// </summary>
        public static ControllerAction<MLibrary.Item> Import { get; } =
            Action<MLibrary.Item>("ImportLibraryItem")
                .DefaultTooltip(L.T("library_import_tooltip", "Import into the current game."))
                .AddCondition(
                    item => item != null,
                    L.T("library_import_no_selection", "No export selected.")
                )
                .AddCondition(
                    HasExistingFile,
                    _ => L.T("library_import_missing_file", "Export file was not found.")
                )
                .AddCondition(CanResolveImportContext, BuildCantResolveImportContextReason)
                .ExecuteWith(ExecuteImport);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Convert to NPCCharacters               //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Converts an export into a standalone NPCCharacters module.
        /// </summary>
        public static ControllerAction<MLibrary.Item> ExportNpcCharacters { get; } =
            Action<MLibrary.Item>("ExportNpcCharacters")
                .DefaultTooltip(
                    L.T("library_export_npc_tooltip", "Convert this export into a standalone mod.")
                )
                .AddCondition(
                    item => item != null,
                    L.T("library_export_npc_no_selection", "No export selected.")
                )
                .AddCondition(
                    HasExistingFile,
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

        /// <summary>
        /// Opens the export file in an external editor.
        /// </summary>
        public static ControllerAction<MLibrary.Item> Edit { get; } =
            Action<MLibrary.Item>("EditLibraryItem")
                .DefaultTooltip(
                    L.T("library_edit_tooltip", "Directly edit this export's XML file contents.")
                )
                .AddCondition(
                    item => item != null,
                    L.T("library_edit_no_selection", "No export selected.")
                )
                .AddCondition(
                    HasExistingFile,
                    L.T("library_edit_failed_missing_file", "Export file was not found.")
                )
                .ExecuteWith(ExecuteEditWithConfirm);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Delete                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Deletes the export file and removes it from the library list.
        /// </summary>
        public static ControllerAction<MLibrary.Item> Delete { get; } =
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
                    HasExistingFile,
                    L.T("library_delete_failed_missing_file", "Export file was not found.")
                )
                .ExecuteWith(ExecuteDeleteWithConfirm);
    }
}
