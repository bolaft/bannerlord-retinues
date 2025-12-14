using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.VM.Column;
using Retinues.Editor.VM.List;
using Retinues.Editor.VM.Panel.Character;
using Retinues.Editor.VM.Panel.Equipment;
using Retinues.Helpers;
using Retinues.Utilities;
using TaleWorlds.Library;

namespace Retinues.Editor.VM
{
    /// <summary>
    /// Editor modes available in the Retinues editor.
    /// </summary>
    public enum EditorMode
    {
        Character = 0,
        Equipment = 1,
        Doctrines = 2,
    }

    /// <summary>
    /// Root editor ViewModel; initializes shared state and child VMs.
    /// </summary>
    public class EditorVM : BaseVM
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

            // Mode defaults to character editing.
            SetMode(EditorMode.Character);

            // Start each editor session from a clean shared state.
            State.Reset(args);
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
        //                        Top Panel                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string EditorTitle => L.S("editor_title", "Troop Editor");

        [DataSourceProperty]
        public string CultureName =>
            State.Culture?.Name?.ToString() ?? L.S("editor_culture_select", "Select a Culture");

        [DataSourceProperty]
        public string ClanName =>
            State.Clan?.Name?.ToString() ?? L.S("editor_clan_select", "Select a Clan");

        [DataSourceProperty]
        public object CultureBanner => State.Culture?.Image ?? Banners.EmptyImage;

        [DataSourceProperty]
        public object ClanBanner => State.Clan?.Image ?? Banners.EmptyImage;

        [DataSourceProperty]
        public Tooltip CultureBannerHint => new(L.S("editor_culture_select", "Select a Culture"));

        [DataSourceProperty]
        public Tooltip ClanBannerHint => new(L.S("editor_clan_select", "Select a Clan"));

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
                {
                    return;
                }

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
                {
                    return;
                }

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
                {
                    return;
                }

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
                {
                    return;
                }

                _equipmentPanel = value;
                OnPropertyChanged(nameof(EquipmentPanel));
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Mode                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorMode Mode = EditorMode.Character;

        public static void SetMode(EditorMode mode)
        {
            if (Mode == mode)
                return;

            Mode = mode;

            // Notify any listeners that mode changed (columns, buttons, etc.).
            EventManager.Fire(UIEvent.Mode, EventScope.Global);
        }

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
                {
                    return;
                }

                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }
}
