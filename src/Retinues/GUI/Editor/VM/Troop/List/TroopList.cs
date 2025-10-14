using System;
using System.Linq;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop.List
{
    [SafeClass]
    public sealed class TroopListVM : BaseList<TroopListVM, TroopRowVM>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public TroopListVM()
        {
            // Retinues
            foreach (
                var root in new[]
                {
                    SelectedFaction.RetinueElite,
                    SelectedFaction.RetinueBasic,
                }.Where(t => t.Parent is null)
            )
                AddTroopTreeInOrder(root, RetinueTroops);

            // Elite troops
            foreach (var root in SelectedFaction.EliteTroops.Where(t => t.Parent is null))
                AddTroopTreeInOrder(root, EliteTroops);

            // Basic troops
            foreach (var root in SelectedFaction.BasicTroops.Where(t => t.Parent is null))
                AddTroopTreeInOrder(root, BasicTroops);

            // Militias
            foreach (
                var root in new[]
                {
                    SelectedFaction.MilitiaMelee,
                    SelectedFaction.MilitiaMeleeElite,
                    SelectedFaction.MilitiaRanged,
                    SelectedFaction.MilitiaRangedElite,
                }.Where(t => t.Parent is null)
            )
                AddTroopTreeInOrder(root, MilitiaTroops);

            // Select the first row by default
            Select(Rows.FirstOrDefault());

            // Elite troops placeholder
            if (EliteTroops.Count == 0)
                EliteTroops.Add(new TroopRowVM(null));

            // Basic troops placeholder
            if (BasicTroops.Count == 0)
                BasicTroops.Add(new TroopRowVM(null));

            // Militia troops placeholder
            if (MilitiaTroops.Count == 0)
                MilitiaTroops.Add(new TroopRowVM(null));

            // Refresh lists at the very end to ensure UI updates
            OnPropertyChanged(nameof(RetinueTroops));
            OnPropertyChanged(nameof(EliteTroops));
            OnPropertyChanged(nameof(BasicTroops));
            OnPropertyChanged(nameof(MilitiaTroops));
        }

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
                if (SelectedFaction == Player.Kingdom)
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
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override MBBindingList<TroopRowVM> Rows =>
            [.. RetinueTroops, .. EliteTroops, .. BasicTroops, .. MilitiaTroops];

        public void Select(WCharacter troop)
        {
            var row = Rows.FirstOrDefault(r => r.Troop == troop);

            if (row != null)
            {
                Select(row);

                // Trigger event
                Editor.TroopScreen.TriggerOnTroopChanged(troop);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void AddTroopTreeInOrder(WCharacter troop, MBBindingList<TroopRowVM> list)
        {
            if (troop == null || !troop.IsValid)
                return; // Ignore null or invalid troops

            // Create and add the row for this troop
            var row = new TroopRowVM(troop);
            list.Add(row);

            if (troop.IsRetinue || troop.IsMilitia)
                return; // Retinue and Militia troops do not have children

            // Find the troop's children
            var children = SelectedFaction
                .BasicTroops.Concat(SelectedFaction.EliteTroops)
                .Where(t => t.Parent != null && t.Parent.Equals(troop));

            // Recursively add each child and its subtree
            foreach (var child in children)
                AddTroopTreeInOrder(child, list);
        }
    }
}
