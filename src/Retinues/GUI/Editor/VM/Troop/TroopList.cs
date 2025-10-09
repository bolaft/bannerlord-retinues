using System.Linq;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using Retinues.Troops.Edition;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop
{
    /// <summary>
    /// ViewModel for troop list. Handles toggles, search, selection, and refreshing troop rows by category.
    /// </summary>
    [SafeClass]
    public sealed class TroopListVM(EditorScreenVM screen)
        : BaseList<TroopListVM, TroopRowVM>(screen)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string RegularToggleText => L.S("list_toggle_regular", "Regular");

        [DataSourceProperty]
        public string EliteToggleText => L.S("list_toggle_elite", "Elite");

        [DataSourceProperty]
        public string RetinueToggleText
        {
            get
            {
                if (Screen.Faction == Player.Kingdom)
                    return Player.IsFemale
                        ? L.S("queen_guard", "Queen's Guard")
                        : L.S("king_guard", "King's Guard");
                else
                    return L.S("retinue", "Retinue");
            }
        }

        [DataSourceProperty]
        public string MilitiaToggleText => L.S("list_toggle_militia", "Militia");

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> RetinueTroops { get; set; } = [];

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> EliteTroops { get; set; } = [];

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> BasicTroops { get; set; } = [];

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> MilitiaTroops { get; set; } = [];

        [DataSourceProperty]
        public string SearchLabel => L.S("item_search_label", "Filter:");

        private string _searchText;

        [DataSourceProperty]
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                    return;
                _searchText = value;
                foreach (var troop in Rows)
                {
                    troop.RefreshVisibility(_searchText);
                }
                OnPropertyChanged(nameof(SearchText));
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override System.Collections.Generic.List<TroopRowVM> Rows =>
            [.. RetinueTroops, .. EliteTroops, .. BasicTroops, .. MilitiaTroops];

        /// <summary>
        /// Selects the row for the given troop, if present.
        /// </summary>
        public void Select(WCharacter troop)
        {
            var row = Rows.FirstOrDefault(r => r.Troop.Equals(troop));
            if (row is not null)
                Select(row);
        }

        /// <summary>
        /// Refreshes all troop rows for each category and updates selection.
        /// </summary>
        public void Refresh()
        {
            RetinueTroops.Clear();
            foreach (
                var root in TroopManager
                    .CollectRetinueTroops(Screen.Faction)
                    .Where(t => t.Parent is null)
            )
                AddTroopTreeInOrder(root, RetinueTroops);

            EliteTroops.Clear();
            foreach (
                var root in TroopManager
                    .CollectEliteTroops(Screen.Faction)
                    .Where(t => t.Parent is null)
            )
                AddTroopTreeInOrder(root, EliteTroops);

            BasicTroops.Clear();
            foreach (
                var root in TroopManager
                    .CollectBasicTroops(Screen.Faction)
                    .Where(t => t.Parent is null)
            )
                AddTroopTreeInOrder(root, BasicTroops);

            MilitiaTroops.Clear();
            foreach (
                var root in TroopManager
                    .CollectMilitiaTroops(Screen.Faction)
                    .Where(t => t.Parent is null)
            )
                AddTroopTreeInOrder(root, MilitiaTroops);

            if (SelectedRow is null)
            {
                Select(
                    RetinueTroops.FirstOrDefault()
                        ?? EliteTroops.FirstOrDefault()
                        ?? BasicTroops.FirstOrDefault()
                );
            }

            if (EliteTroops.Count == 0)
                EliteTroops.Add(new TroopRowVM(null, this));

            if (BasicTroops.Count == 0)
                BasicTroops.Add(new TroopRowVM(null, this));

            if (MilitiaTroops.Count == 0)
                MilitiaTroops.Add(new TroopRowVM(null, this));

            OnPropertyChanged(nameof(SelectedRow));
            OnPropertyChanged(nameof(RetinueToggleText));
            OnPropertyChanged(nameof(RetinueTroops));
            OnPropertyChanged(nameof(EliteTroops));
            OnPropertyChanged(nameof(BasicTroops));
            OnPropertyChanged(nameof(MilitiaTroops));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Adds a troop and its children to the list in tree order.
        /// </summary>
        private void AddTroopTreeInOrder(WCharacter troop, MBBindingList<TroopRowVM> list)
        {
            if (troop == null || !troop.IsActive)
                return;

            var row = new TroopRowVM(troop, this);
            list.Add(row);

            if (troop.IsRetinue || troop.IsMilitia)
                return; // Retinue and Militia troops do not have children

            var children = Screen
                .Faction.BasicTroops.Concat(Screen.Faction.EliteTroops)
                .Where(t => t.Parent != null && t.Parent.Equals(troop));

            foreach (var child in children)
                AddTroopTreeInOrder(child, list);
        }
    }
}
