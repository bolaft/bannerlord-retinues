using Retinues.Editor.MVC.Pages.Library.Services;
using Retinues.Editor.MVC.Shared.Controllers;

namespace Retinues.Editor.Controllers.Library
{
    /// <summary>
    /// Library screen operations (import/export/delete/edit).
    /// </summary>
    public partial class LibraryController : BaseController
    {
        private const string NpcExportModulePrefix = "Retinues.Export.";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Public                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns the display name for a library item.
        /// </summary>
        public static string GetName(ExportLibrary.Entry item) => item?.DisplayName ?? string.Empty;

        /// <summary>
        /// Returns the export file path for a library item.
        /// </summary>
        public static string GetExportPath(ExportLibrary.Entry item) =>
            item?.FilePath ?? string.Empty;
    }
}
