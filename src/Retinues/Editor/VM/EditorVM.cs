using Retinues.Editor.VM.List;
using Retinues.Wrappers.Characters;
using Retinues.Wrappers.Factions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Retinues.Editor.VM
{
    public class EditorVM : ViewModel
    {
        public EditorVM()
        {
            // Wrap the main hero and get their culture as an IBaseFaction.
            IBaseFaction faction = null;

            var hero = Hero.MainHero;
            if (hero != null && hero.CharacterObject != null)
            {
                var wHero = WCharacter.Get(hero.CharacterObject);
                faction = wHero?.Culture;
            }

            // Initialize list
            List = new ListVM();

            // Define sort buttons
            List.AddSortButton("name", "Name", 2);
            List.AddSortButton("tier", "Tier", 1);
            List.AddSortButton("value", "Value", 1);

            if (faction != null)
            {
                // Retinues
                var retinues = List.AddHeader("retinues", "Retinues");
                if (faction.RootElite != null)
                    foreach (var troop in faction.RosterRetinues)
                        retinues.AddCharacterRow(troop);

                // Elite section: elite root
                var elite = List.AddHeader("elite", "Elite");
                if (faction.RootElite != null)
                    foreach (var troop in faction.RootElite.Tree)
                        elite.AddCharacterRow(troop);

                // Regular section: basic root
                var basic = List.AddHeader("regular", "Regular");
                if (faction.RootBasic != null)
                    foreach (var troop in faction.RootBasic.Tree)
                        basic.AddCharacterRow(troop);

                // Militia section
                var militia = List.AddHeader("militia", "Militia");
                var rosterMilitia = faction.RosterMilitia;
                if (rosterMilitia?.Count > 0)
                {
                    foreach (var troop in rosterMilitia)
                    {
                        if (troop == null)
                            continue;

                        militia.AddCharacterRow(troop);
                    }
                }

                // Caravan section
                var caravan = List.AddHeader("caravan", "Caravan");
                var rosterCaravan = faction.RosterCaravan;
                if (rosterCaravan?.Count > 0)
                {
                    foreach (var troop in rosterCaravan)
                    {
                        if (troop == null)
                            continue;

                        caravan.AddCharacterRow(troop);
                    }
                }

                // Villager section
                var villagers = List.AddHeader("villagers", "Villagers");
                var rosterVillager = faction.RosterVillager;
                if (rosterVillager?.Count > 0)
                {
                    foreach (var troop in rosterVillager)
                    {
                        if (troop == null)
                            continue;

                        villagers.AddCharacterRow(troop);
                    }
                }

                // Bandits section
                var bandits = List.AddHeader("bandits", "Bandits");
                var rosterBandits = faction.RosterBandit;
                if (rosterBandits?.Count > 0)
                {
                    foreach (var troop in rosterBandits)
                    {
                        if (troop == null)
                            continue;

                        bandits.AddCharacterRow(troop);
                    }
                }

                // Civilians section
                var civilians = List.AddHeader("civilians", "Civilians");

                var rosterCivilians = faction.RosterCivilian;
                if (rosterCivilians?.Count > 0)
                {
                    foreach (var troop in rosterCivilians)
                    {
                        if (troop == null)
                            continue;

                        civilians.AddCharacterRow(troop);
                    }
                }
            }

            List.RefreshValues();
        }

        private bool _isVisible = false;

        [DataSourceProperty]
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;
                    OnPropertyChanged(nameof(IsVisible));
                }
            }
        }

        private ListVM _list;

        [DataSourceProperty]
        public ListVM List
        {
            get => _list;
            set
            {
                if (value != _list)
                {
                    _list = value;
                    OnPropertyChanged(nameof(List));
                }
            }
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            List.RefreshValues();
        }
    }
}
