using System;
using System.IO;
using Retinues.Framework.Model.Exports;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Services;
using Retinues.Utilities;

namespace Retinues.GUI.Editor.Controllers.Library
{
    public partial class LibraryController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Delete                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Shows a confirmation popup before deleting the export file.
        /// </summary>
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

        /// <summary>
        /// Deletes the export file and refreshes the library list.
        /// </summary>
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

        /// <summary>
        /// Refreshes library UI after create/delete operations.
        /// </summary>
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
    }
}
