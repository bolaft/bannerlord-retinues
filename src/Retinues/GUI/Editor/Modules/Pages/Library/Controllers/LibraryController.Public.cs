using System.Collections.Generic;
using Retinues.Editor.Services.Library;
using Retinues.Framework.Model.Exports;
using Retinues.UI.Services;

namespace Retinues.Editor.Controllers.Library
{
    public partial class LibraryController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Public                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns a localized type string for the library item.
        /// </summary>
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

        /// <summary>
        /// Returns faction troop names from the file for UI preview.
        /// </summary>
        public static List<string> GetFactionTroopNamesFromFile(MLibrary.Item item)
        {
            if (!LibraryFileReader.TryReadTroopNames(item, out var names) || names.Count == 0)
                return [];

            return names;
        }

        /// <summary>
        /// Returns a UI hint describing the import target context.
        /// </summary>
        public static string GetTargetName(MLibrary.Item item)
        {
            if (item == null)
                return string.Empty;

            if (item.Kind == MLibraryKind.Character)
            {
                var f = EditorState.Instance.Faction;
                return f != null
                    ? L.T("library_import_target_current_faction", "Current faction: {NAME}")
                        .SetTextVariable("NAME", f.Name)
                        .ToString()
                    : string.Empty;
            }

            return string.Empty;
        }
    }
}
