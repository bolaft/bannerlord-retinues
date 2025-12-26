using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Controllers.Library;
using Retinues.Helpers;
using Retinues.Model;
using Retinues.Utilities;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Panel.Library
{
    /// <summary>
    /// Library panel.
    /// </summary>
    public class LibraryPanelVM : BaseVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Visibility                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool IsVisible => EditorVM.Page == EditorPage.Library;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Library)]
        [DataSourceProperty]
        public bool HasSelection => State.LibraryItem != null;

        [EventListener(UIEvent.Library)]
        [DataSourceProperty]
        public bool IsTroop => State.LibraryItem?.Kind == MLibraryKind.Character;

        [EventListener(UIEvent.Library)]
        [DataSourceProperty]
        public bool IsFaction => State.LibraryItem?.Kind == MLibraryKind.Faction;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Headers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Library)]
        [DataSourceProperty]
        public string NameHeader =>
            IsFaction
                ? L.S("library_name_faction_header", "Faction")
                : L.S("library_name_character_header", "Unit");

        [DataSourceProperty]
        public string TargetHeader => L.S("library_target_header", "Replaces");

        [DataSourceProperty]
        public string ContentsHeader => L.S("library_contents_header", "Contents");

        [DataSourceProperty]
        public string ExportFileHeader => L.S("library_export_file_header", "Export File");

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Main fields                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Library)]
        [DataSourceProperty]
        public string Name => LibraryController.GetName(State.LibraryItem);

        [EventListener(UIEvent.Library)]
        [DataSourceProperty]
        public string TypeText => LibraryController.GetTypeText(State.LibraryItem);

        [EventListener(UIEvent.Library)]
        [DataSourceProperty]
        public string ExportPath => LibraryController.GetExportPath(State.LibraryItem);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Troops                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// For Character exports: current in-game name of the troop matching the stringId.
        /// </summary>
        [EventListener(UIEvent.Library)]
        [DataSourceProperty]
        public string TargetName =>
            IsTroop ? LibraryController.GetTargetName(State.LibraryItem) : string.Empty;

        /// <summary>
        /// For Faction exports: troop name entry in the troop list.
        /// </summary>
        public class FactionTroopNameVM(string name) : BaseVM
        {
            readonly string _name = name ?? string.Empty;

            [DataSourceProperty]
            public string Name => _name;
        }

        /// <summary>
        /// For Faction exports: included troop names from the export file.
        /// </summary>
        [EventListener(UIEvent.Library)]
        [DataSourceProperty]
        public MBBindingList<FactionTroopNameVM> TroopNames
        {
            get
            {
                var list = new MBBindingList<FactionTroopNameVM>();
                if (!IsFaction)
                    return list;

                var all =
                    LibraryController.GetFactionTroopNamesFromFile(State.LibraryItem)?.ToList()
                    ?? [];
                const int limit = 5;
                int total = all.Count;

                foreach (var name in all.Take(limit))
                    list.Add(new FactionTroopNameVM(name));

                if (total > limit)
                    list.Add(
                        new FactionTroopNameVM(
                            L.T("troop_count_more", "and {NUMBER} more troops.")
                                .SetTextVariable("NUMBER", (total - limit))
                                .ToString()
                        )
                    );

                return list;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Import                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Library)]
        [DataSourceProperty]
        public bool CanImport => LibraryController.Import.Allow(State.LibraryItem);

        [EventListener(UIEvent.Library)]
        [DataSourceProperty]
        public Tooltip ImportTooltip => LibraryController.Import.Tooltip(State.LibraryItem);

        [DataSourceProperty]
        public string ImportButtonText => L.S("library_import_button", "Import");

        [DataSourceMethod]
        public void ExecuteImport() => LibraryController.Import.Execute(State.LibraryItem);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Delete                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string DeleteButtonText => L.S("library_delete_button", "Delete");

        [DataSourceMethod]
        public void ExecuteDelete() => LibraryController.DeleteLibraryItem(State.LibraryItem);
    }
}
