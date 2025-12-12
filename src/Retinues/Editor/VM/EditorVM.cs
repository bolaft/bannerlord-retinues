using Retinues.Editor.VM.Column;
using Retinues.Editor.VM.List;
using Retinues.Editor.VM.Panel.Character;
using Retinues.Editor.VM.Panel.Equipment;
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
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Construction                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public EditorVM()
        {
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
            State.Reset();
        }

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
                OnPropertyChanged(nameof(CharacterPanel));
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
