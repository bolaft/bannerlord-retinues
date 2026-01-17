using System;
using System.IO;
using System.Linq;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Editor.Modules.Pages.Library.Services;
using Retinues.GUI.Editor.Shared.Controllers;
using Retinues.GUI.Services;
using Retinues.Utilities;

namespace Retinues.GUI.Editor.Controllers.Library
{
    /// <summary>
    /// Partial class for library controller delete actions.
    /// </summary>
    public partial class LibraryController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Delete                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Deletes the selected library export item and its associated file.
        /// </summary>
        public static ControllerAction<ExportLibrary.Entry> Delete { get; } =
            Action<ExportLibrary.Entry>("DeleteLibraryItem")
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

        /// <summary>
        /// Shows a confirmation popup before deleting the export file.
        /// </summary>
        private static void ExecuteDeleteWithConfirm(ExportLibrary.Entry item)
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

        /// <summary>
        /// Deletes the export file and refreshes the library list.
        /// </summary>
        private static void ApplyDelete(ExportLibrary.Entry item)
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

        /// <summary>
        /// Refreshes library UI after create/delete operations.
        /// </summary>
        private static void RefreshLibraryAfterChange(ExportLibrary.Entry item)
        {
            try
            {
                // Reset selection if we deleted the selected entry.
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
                    EditorState.Instance.LibraryItem = ExportLibrary.GetAll().FirstOrDefault();
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
    }
}
