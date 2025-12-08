using Retinues.Editor.VM.List;
using Retinues.Editor.VM.Panel.Character;
using Retinues.Editor.VM.Tableau;
using TaleWorlds.Library;

namespace Retinues.Editor.VM
{
    /// <summary>
    /// Editor modes available in the Retinues editor.
    /// </summary>
    public enum EditorMode
    {
        Character = 0,
    }

    /// <summary>
    /// Root editor ViewModel; initializes shared state and child VMs.
    /// </summary>
    public class EditorVM : BaseStatefulVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Construction                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public EditorVM()
        {
            // Initialize the troop list VM.
            List = new ListVM();

            // Initialize the tableau VM.
            Tableau = new TableauVM();

            // Initialize the character panel VM.
            CharacterPanel = new CharacterPanel();

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

        private TableauVM _tableau;

        [DataSourceProperty]
        public TableauVM Tableau
        {
            get => _tableau;
            private set
            {
                if (value == _tableau)
                {
                    return;
                }

                _tableau = value;
                OnPropertyChanged(nameof(Tableau));
            }
        }

        private CharacterPanel _characterPanel;

        [DataSourceProperty]
        public CharacterPanel CharacterPanel
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Mode                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorMode Mode = EditorMode.Character;

        public static void SetMode(EditorMode mode)
        {
            if (Mode == mode)
            {
                return;
            }

            Mode = mode;

            // Notify any listeners that mode changed (columns, buttons, etc.).
            EventManager.Fire(UIEvent.Mode, EventScope.Global);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        IsVisible                       //
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
