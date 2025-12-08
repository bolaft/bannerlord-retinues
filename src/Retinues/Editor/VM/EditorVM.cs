using Retinues.Editor.VM.List;
using TaleWorlds.Core.ViewModelCollection;
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Model                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private CharacterViewModel _model;

        [DataSourceProperty]
        public CharacterViewModel Model
        {
            get => _model;
            private set
            {
                if (ReferenceEquals(value, _model))
                {
                    return;
                }

                _model = value;
                OnPropertyChanged(nameof(Model));
            }
        }

        // Rebuild the tableau model whenever the current troop changes.
        [EventListener(UIEvent.Troop)]
        private void RebuildModel()
        {
            var character = State.Character;
            if (character?.Base == null)
            {
                Model = null;
                return;
            }

            var co = character.Base;
            var vm = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
            vm.FillFrom(co, seed: -1);

            Model = vm;
        }
    }
}
