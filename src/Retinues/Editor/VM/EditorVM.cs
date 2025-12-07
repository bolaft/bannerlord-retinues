using Retinues.Editor.VM.List;
using Retinues.Utilities;
using Retinues.Wrappers.Characters;
using Retinues.Wrappers.Factions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Retinues.Editor.VM
{
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
            // Start each editor session from a clean shared state.
            ResetState();

            // Initialize the troop list VM.
            List = new ListVM();

            // Sort buttons: keep stable ordering and widths.
            List.AddSortButton("name", L.S("sort_by_name", "Name"), 2);
            List.AddSortButton("tier", L.S("sort_by_tier", "Tier"), 1);
            List.AddSortButton("value", L.S("sort_by_value", "Value"), 1);

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
