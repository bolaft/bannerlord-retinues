using System;
using Retinues.Editor.MVC.Pages.Library.Services;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Interface.Services;
using Retinues.Utilities;

namespace Retinues.Editor.Controllers.Library
{
    /// <summary>
    /// Partial class for library controller edit actions.
    /// </summary>
    public partial class LibraryController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                           Edit                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Edits the selected library export item's XML file in the default editor.
        /// </summary>
        public static ControllerAction<ExportLibrary.Entry> Edit { get; } =
            Action<ExportLibrary.Entry>("EditLibraryItem")
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

        /// <summary>
        /// Shows a confirmation popup before opening the file for edit.
        /// </summary>
        private static void ExecuteEditWithConfirm(ExportLibrary.Entry item)
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
