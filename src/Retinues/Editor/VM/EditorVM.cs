using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Domain.Factions.Helpers;
using Retinues.Editor.Controllers.Faction;
using Retinues.Editor.Events;
using Retinues.Editor.VM.Column;
using Retinues.Editor.VM.List;
using Retinues.Editor.VM.Panel.Character;
using Retinues.Editor.VM.Panel.Equipment;
using Retinues.Editor.VM.Panel.Library;
using Retinues.UI.Services;
using Retinues.UI.VM;
using Retinues.Utilities;
using TaleWorlds.Library;

namespace Retinues.Editor.VM
{
    /// <summary>
    /// Editor pages available in the editor.
    /// </summary>
    public enum EditorPage
    {
        Character = 0,
        Equipment = 1,
        Doctrines = 2,
        Library = 3,
    }

    /// <summary>
    /// Root editor ViewModel; initializes shared state and child VMs.
    /// </summary>
    public class EditorVM : EventListenerVM
    {
        private readonly Action _close;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Construction                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public EditorVM(Action close, EditorLaunchArgs args = null)
        {
            _close = close;

            // Initialize the troop list VM.
            List = new ListVM();

            // Initialize the tableau VM.
            Column = new ColumnVM();

            // Initialize the character panel VM.
            CharacterPanel = new CharacterPanelVM();

            // Initialize the equipment panel VM.
            EquipmentPanel = new EquipmentPanelVM();

            // Initialize the library panel VM.
            LibraryPanel = new LibraryPanelVM();

            // Page defaults to character editing.
            SetPage(EditorPage.Character);

            // Start each editor session from a clean shared state.
            EditorState.Reset(args);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Rooting                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteClose()
        {
            _close?.Invoke();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Done Button                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string DoneButtonText => L.S("editor_done_button", "Done");

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Background                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool IndoorsBackground => State.Mode == EditorMode.Player;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Top Panel                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Faction)]
        [DataSourceProperty]
        public string EditorTitle => State.Faction.Name;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Tabs                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string EditorTabText => L.S("editor_tab_editor", "Editor");

        [DataSourceProperty]
        public string DoctrinesTabText => L.S("editor_tab_doctrines", "Doctrines");

        [DataSourceProperty]
        public string LibraryTabText => L.S("editor_tab_library", "Library");

        [DataSourceProperty]
        public string SettingsTabText => L.S("editor_tab_settings", "Settings");

        [DataSourceProperty]
        public bool IsPlayerMode => EditorState.Instance?.Mode == EditorMode.Player;

        // Selected state for the *top* tabs
        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool IsEditorTabSelected =>
            Page == EditorPage.Character || Page == EditorPage.Equipment;

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool IsDoctrinesTabSelected => Page == EditorPage.Doctrines;

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool IsLibraryTabSelected => Page == EditorPage.Library;

        [DataSourceProperty]
        public bool IsDoctrinesTabVisible => IsPlayerMode;

        [DataSourceMethod]
        public void ExecuteSelectEditorTab()
        {
            SetPage(_lastEditorSubPage);
        }

        [DataSourceMethod]
        public void ExecuteSelectDoctrinesTab()
        {
            SetPage(EditorPage.Doctrines);
        }

        [DataSourceMethod]
        public void ExecuteSelectLibraryTab()
        {
            SetPage(EditorPage.Library);
        }

        [DataSourceMethod]
        public void ExecuteSelectSettingsTab()
        {
            Log.Info("Settings tab clicked. Opening MCM settings: Retinues.Settings");

            if (!MCMLauncher.TryOpenSettings("Retinues.Settings"))
                Log.Warn("Failed to open MCM settings screen.");
        }

        /* ━━━━━━━━ Culture ━━━━━━━ */

        [EventListener(UIEvent.CultureFaction)]
        [DataSourceProperty]
        public string CultureName =>
            State.Culture?.Name?.ToString() ?? L.S("editor_culture_select", "Select a Culture");

        [EventListener(UIEvent.CultureFaction)]
        [DataSourceProperty]
        public object CultureBanner => State.Culture?.Image ?? BannerHelper.EmptyImage;

        [EventListener(UIEvent.CultureFaction, UIEvent.Faction)]
        [DataSourceProperty]
        public bool CanSelectCulture => FactionController.SelectCulturePopup.Allow(true);

        [EventListener(UIEvent.CultureFaction, UIEvent.Faction)]
        [DataSourceProperty]
        public Tooltip CultureBannerHint => FactionController.SelectCulturePopup.Tooltip(true);

        [DataSourceMethod]
        public void ExecuteSelectCulture() => FactionController.SelectCulturePopup.Execute(true);

        /* ━━━━━━━━━ Clan ━━━━━━━━━ */

        [EventListener(UIEvent.ClanFaction)]
        [DataSourceProperty]
        public string ClanName =>
            State.Clan?.Name?.ToString() ?? L.S("editor_clan_select", "Select a Clan");

        [EventListener(UIEvent.ClanFaction)]
        [DataSourceProperty]
        public object ClanBanner => State.Clan?.Image ?? BannerHelper.EmptyImage;

        [EventListener(UIEvent.ClanFaction, UIEvent.CultureFaction, UIEvent.Faction)]
        [DataSourceProperty]
        public bool CanSelectClan => FactionController.SelectClanPopup.Allow(true);

        [EventListener(UIEvent.ClanFaction, UIEvent.CultureFaction, UIEvent.Faction)]
        [DataSourceProperty]
        public Tooltip ClanBannerHint => FactionController.SelectClanPopup.Tooltip(true);

        [DataSourceMethod]
        public void ExecuteSelectClan() => FactionController.SelectClanPopup.Execute(true);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Components                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private ListVM _list;

        [DataSourceProperty]
        public ListVM List
        {
            get => _list;
            private set
            {
                if (value == _list)
                    return;

                _list = value;
                OnPropertyChanged(nameof(List));
            }
        }

        private ColumnVM _column;

        [DataSourceProperty]
        public ColumnVM Column
        {
            get => _column;
            private set
            {
                if (value == _column)
                    return;

                _column = value;
                OnPropertyChanged(nameof(Column));
            }
        }

        private CharacterPanelVM _characterPanel;

        [DataSourceProperty]
        public CharacterPanelVM CharacterPanel
        {
            get => _characterPanel;
            private set
            {
                if (value == _characterPanel)
                    return;

                _characterPanel = value;
                OnPropertyChanged(nameof(CharacterPanel));
            }
        }

        private EquipmentPanelVM _equipmentPanel;

        [DataSourceProperty]
        public EquipmentPanelVM EquipmentPanel
        {
            get => _equipmentPanel;
            private set
            {
                if (value == _equipmentPanel)
                    return;

                _equipmentPanel = value;
                OnPropertyChanged(nameof(EquipmentPanel));
            }
        }

        private LibraryPanelVM _libraryPanel;

        [DataSourceProperty]
        public LibraryPanelVM LibraryPanel
        {
            get => _libraryPanel;
            private set
            {
                if (value == _libraryPanel)
                    return;

                _libraryPanel = value;
                OnPropertyChanged(nameof(LibraryPanel));
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Page                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorPage Page = EditorPage.Character;

        private static EditorPage _lastEditorSubPage = EditorPage.Character;

        public static void SetPage(EditorPage page)
        {
            if (Page == page)
                return;

            Page = page;

            // Keep "Editor" tab sticky to the last real editor sub-page.
            if (page == EditorPage.Character || page == EditorPage.Equipment)
                _lastEditorSubPage = page;

            // Notify any listeners that page changed (columns, buttons, etc.).
            EventManager.Fire(UIEvent.Page);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Faction Export                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Button<bool> ExportFactionButton { get; } =
            new(
                action: FactionController.ExportFaction,
                arg: () => true,
                refresh: [UIEvent.Faction],
                sprite: "SPGeneral\\Skills\\gui_skills_icon_steward_tiny",
                color: "#f8eed1ff"
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Visibility                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool _isVisible;

        [DataSourceProperty]
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (value == _isVisible)
                    return;

                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }
}
