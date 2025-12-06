using TaleWorlds.Library;

namespace Retinues.Editor.VM
{
    public class EditorVM : ViewModel
    {
        public EditorVM()
        {
            // Initialize list
            List = new ListVM();

            // Define sort buttons
            List.AddSortButton("name", "Name", 2);
            List.AddSortButton("tier", "Tier", 1);
            List.AddSortButton("value", "Value", 1);

            // Regular section
            var regular = List.AddHeader("regular", "Regular");
            regular.AddElement("troop_1", "Tier 1 Something");
            regular.AddElement("troop_2", "Tier 2 Something");

            // Elite section
            var elite = List.AddHeader("elite", "Elite");
            elite.AddElement("troop_elite_1", "Elite Guard");

            // Militia section
            var militia = List.AddHeader("militia", "Militia");
            militia.AddElement("troop_militia_1", "Town Militia");
            militia.AddElement("troop_militia_2", "City Militia");
            militia.AddElement("troop_militia_3", "Village Militia");
            militia.AddElement("troop_militia_4", "Fort Militia");

            // Civilians section
            var civilians = List.AddHeader("civilians", "Civilians");
            civilians.AddElement("troop_civilian_1", "Farmer");
            civilians.AddElement("troop_civilian_2", "Merchant");
            civilians.AddElement("troop_civilian_3", "Blacksmith");
            civilians.AddElement("troop_civilian_4", "Baker");
            civilians.AddElement("troop_civilian_5", "Carpenter");
            civilians.AddElement("troop_civilian_6", "Fisherman");

            // Bandits section
            var bandits = List.AddHeader("bandits", "Bandits");
            bandits.AddElement("troop_bandit_1", "Forest Bandit");
            bandits.AddElement("troop_bandit_2", "Mountain Bandit");
            bandits.AddElement("troop_bandit_3", "Desert Bandit");
            bandits.AddElement("troop_bandit_4", "Sea Raider");

            List.Refresh();
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
