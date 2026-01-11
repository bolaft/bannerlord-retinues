using System.Linq;
using Retinues.Editor.Controllers.Library;
using Retinues.Editor.Events;
using Retinues.Framework.Model.Exports;
using Retinues.UI.Services;
using Retinues.UI.VM;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Panel.Library
{
    /// <summary>
    /// Library panel.
    /// </summary>
    public class LibraryPanelVM : EventListenerVM
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
        //                       Empty Page                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Library, UIEvent.Page)]
        [DataSourceProperty]
        public bool ShowEmptyText => IsVisible && !HasSelection;

        [DataSourceProperty]
        public string ExportsEmptyText =>
            L.S("library_exports_empty_text", "No saved exports found.");

        [DataSourceProperty]
        public string ExportsHintText =>
            L.S(
                "library_exports_hint_text",
                "Troops and factions can be exported from the editor tab."
            );

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
        public string ContentsHeader => L.S("library_contents_header", "Contents");

        [DataSourceProperty]
        public string ExportFileHeader => L.S("library_export_file_header", "File");

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

        public class FactionTroopNameVM(string name) : EventListenerVM
        {
            readonly string _name = name ?? string.Empty;

            [DataSourceProperty]
            public string Name => _name;
        }

        private readonly MBBindingList<FactionTroopNameVM> _troopNames = [];

        [DataSourceProperty]
        public MBBindingList<FactionTroopNameVM> TroopNames => _troopNames;

        [EventListener(UIEvent.Library)]
        private void RefreshTroopNames()
        {
            _troopNames.Clear();

            if (!IsFaction)
            {
                OnPropertyChanged(nameof(TroopNames));
                return;
            }

            var all =
                LibraryController.GetFactionTroopNamesFromFile(State.LibraryItem)?.ToList() ?? [];

            const int limit = 10;
            int total = all.Count;

            foreach (var name in all.Take(limit))
                _troopNames.Add(new FactionTroopNameVM(name));

            if (total > limit)
                _troopNames.Add(
                    new FactionTroopNameVM(
                        L.T("troop_count_more", "and {NUMBER} more troops.")
                            .SetTextVariable("NUMBER", total - limit)
                            .ToString()
                    )
                );

            OnPropertyChanged(nameof(TroopNames));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Buttons                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Button<MLibrary.Item> ImportButton { get; } =
            new(
                action: LibraryController.Import,
                arg: () => State.LibraryItem,
                refresh: [UIEvent.Library],
                label: L.S("library_import_button", "Import")
            );

        [DataSourceProperty]
        public Button<MLibrary.Item> ConvertButton { get; } =
            new(
                action: LibraryController.ExportNpcCharacters,
                arg: () => State.LibraryItem,
                refresh: [UIEvent.Library],
                label: L.S("library_export_npc_button", "Convert")
            );

        [DataSourceProperty]
        public Button<MLibrary.Item> EditButton { get; } =
            new(
                action: LibraryController.Edit,
                arg: () => State.LibraryItem,
                refresh: [UIEvent.Library],
                label: L.S("library_edit_button", "Edit")
            );

        [DataSourceProperty]
        public Button<MLibrary.Item> DeleteButton { get; } =
            new(
                action: LibraryController.Delete,
                arg: () => State.LibraryItem,
                refresh: [UIEvent.Library],
                label: L.S("library_delete_button", "Delete")
            );
    }
}
