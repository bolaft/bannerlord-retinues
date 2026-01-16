using System;
using Retinues.Framework.Model.Exports;
using Retinues.GUI.Services;
using Retinues.Utilities;

namespace Retinues.GUI.Editor.Controllers.Library
{
    public partial class LibraryController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                           Edit                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Shows a confirmation popup before opening the file for edit.
        /// </summary>
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

        /// <summary>
        /// Opens the file in the default external editor.
        /// </summary>
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
    }
}
