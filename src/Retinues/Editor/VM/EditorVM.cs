using System.ComponentModel;
using Retinues.Editor.VM.List;
using Retinues.Editor.VM.List.Rows;
using Retinues.Wrappers.Characters;
using Retinues.Wrappers.Factions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace Retinues.Editor.VM
{
    public enum EditorMode
    {
        Character = 0,
    }

    /// <summary>
    /// Root editor ViewModel; initializes shared state and child VMs.
    /// </summary>
    public class EditorVM : BaseStatefulVM
    {
        public static EditorMode Mode = EditorMode.Character;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Construction                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public EditorVM()
        {
            // Start each editor session from a clean shared state.
            ResetState();

            // Initialize the troop list VM.
            List = new ListVM();

            // Listen to selection changes to update the tableau model.
            List.PropertyChanged += OnListPropertyChanged;

            // Initialize default state (faction, character, etc.).
            InitializeStateDefaults();
        }

        private void InitializeStateDefaults()
        {
            // Default faction: use the main hero's culture if available.
            IBaseFaction faction = null;

            var hero = Hero.MainHero;
            if (hero?.CharacterObject != null)
            {
                var wrappedHero = WCharacter.Get(hero.CharacterObject);
                faction = wrappedHero?.Culture;
            }

            StateFaction = faction;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Properties                       //
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Lifecycle                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RefreshValues()
        {
            base.RefreshValues();
            List?.RefreshValues();
        }

        private void OnListPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ListVM.SelectedElement))
            {
                UpdateModelFromSelection();
            }
        }

        private void UpdateModelFromSelection()
        {
            var selectedRow = List?.SelectedElement as CharacterRowVM;
            var character = selectedRow?.Character;

            if (character == null)
            {
                Model = null;
                return;
            }

            var co = character.Base;
            if (co == null)
            {
                Model = null;
                return;
            }

            // Simple, safe model setup: let the engine fill everything from the CharacterObject.
            var vm = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
            vm.FillFrom(co, seed: -1);

            Model = vm;
        }
    }
}
