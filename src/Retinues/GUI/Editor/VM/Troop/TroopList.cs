using System.Linq;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop
{
    /// <summary>
    /// ViewModel for troop list. Handles toggles, search, selection, and refreshing troop rows by category.
    /// </summary>
    [SafeClass]
    public sealed class TroopListVM : BaseList<TroopListVM, TroopRowVM>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Rebuilds all troop rows for each category and updates selection.
        /// </summary>
        public TroopListVM()
        {
            // Retinues
            foreach (
                var root in TroopManager
                    .CollectRetinueTroops(Faction)
                    .Where(t => t.Parent is null)
            )
                AddTroopTreeInOrder(root, RetinueTroops);

            // Elite troops
            foreach (
                var root in TroopManager
                    .CollectEliteTroops(Faction)
                    .Where(t => t.Parent is null)
            )
                AddTroopTreeInOrder(root, EliteTroops);

            // Basic troops
            foreach (
                var root in TroopManager
                    .CollectBasicTroops(Faction)
                    .Where(t => t.Parent is null)
            )
                AddTroopTreeInOrder(root, BasicTroops);

            // Militias
            foreach (
                var root in TroopManager
                    .CollectMilitiaTroops(Faction)
                    .Where(t => t.Parent is null)
            )
                AddTroopTreeInOrder(root, MilitiaTroops);

            // Select the first row by default
            Select(Rows.FirstOrDefault());

            // Elite troops placeholder
            if (EliteTroops.Count == 0)
                EliteTroops.Add(new TroopRowVM(null, this));

            // Basic troops placeholder
            if (BasicTroops.Count == 0)
                BasicTroops.Add(new TroopRowVM(null, this));

            // Militia troops placeholder
            if (MilitiaTroops.Count == 0)
                MilitiaTroops.Add(new TroopRowVM(null, this));
            
            // Refresh lists at the very end to ensure UI updates
            @OnPropertyChanged(nameof(RetinueTroops));
            @OnPropertyChanged(nameof(EliteTroops));
            @OnPropertyChanged(nameof(BasicTroops));
            @OnPropertyChanged(nameof(MilitiaTroops));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Quick Access                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WFaction Faction => EditorVM.Instance.Faction;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━ Toggle Texts ━━━━━ */

        [DataSourceProperty]
        public string RegularToggleText => L.S("list_toggle_regular", "Regular");

        [DataSourceProperty]
        public string EliteToggleText => L.S("list_toggle_elite", "Elite");

        [DataSourceProperty]
        public string RetinueToggleText
        {
            get
            {
                if (Faction == Player.Kingdom)
                    return Player.IsFemale
                        ? L.S("queen_guard", "Queen's Guard")
                        : L.S("king_guard", "King's Guard");
                else
                    return L.S("retinue", "Retinue");
            }
        }

        [DataSourceProperty]
        public string MilitiaToggleText => L.S("list_toggle_militia", "Militia");

        /* ━━━━━━ Troop Lists ━━━━━ */

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Override for BaseList functionality.
        /// </summary>
        public override System.Collections.Generic.List<TroopRowVM> Rows =>
            [.. RetinueTroops, .. EliteTroops, .. BasicTroops, .. MilitiaTroops];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Adds a troop and its children to the list in tree order.
        /// </summary>
        private void AddTroopTreeInOrder(WCharacter troop, MBBindingList<TroopRowVM> list)
        {
            if (troop == null || !troop.IsValid)
                return; // Ignore null or invalid troops

            // Create and add the row for this troop
            var row = new TroopRowVM(troop, this);
            list.Add(row);

            if (troop.IsRetinue || troop.IsMilitia)
                return; // Retinue and Militia troops do not have children

            // Find the troop's children
            var children = Faction.BasicTroops.Concat(Faction.EliteTroops)
                .Where(t => t.Parent != null && t.Parent.Equals(troop));

            // Recursively add each child and its subtree
            foreach (var child in children)
                AddTroopTreeInOrder(child, list);
        }
    }
}
