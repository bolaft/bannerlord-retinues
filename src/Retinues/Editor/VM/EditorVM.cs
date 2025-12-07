using Retinues.Editor.VM.List;
using Retinues.Wrappers.Characters;
using Retinues.Wrappers.Factions;
using TaleWorlds.CampaignSystem;
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Lifecycle                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RefreshValues()
        {
            base.RefreshValues();
            List?.RefreshValues();
        }
    }
}
