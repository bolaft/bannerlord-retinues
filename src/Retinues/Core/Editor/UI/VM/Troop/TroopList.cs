using System.Linq;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.Library;

namespace Retinues.Core.Editor.UI.VM.Troop
{
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
                if (Screen.Faction.StringId == Player.Kingdom?.StringId)
                    return Player.IsFemale
                        ? L.S("queen_guard", "Queen's Guard")
                        : L.S("king_guard", "King's Guard");
                else
                    return L.S("retinue", "Retinue");
            }
        }

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> RetinueTroops { get; set; } = [];

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> EliteTroops { get; set; } = [];

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> BasicTroops { get; set; } = [];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override System.Collections.Generic.List<TroopRowVM> Rows =>
            [.. RetinueTroops, .. EliteTroops, .. BasicTroops];

        public void Select(WCharacter troop)
        {
            var row = Rows.FirstOrDefault(r => r.Troop.Equals(troop));
            if (row is not null)
                Select(row);
        }

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

            OnPropertyChanged(nameof(SelectedRow));
            OnPropertyChanged(nameof(RetinueToggleText));
            OnPropertyChanged(nameof(RetinueTroops));
            OnPropertyChanged(nameof(EliteTroops));
            OnPropertyChanged(nameof(BasicTroops));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void AddTroopTreeInOrder(WCharacter troop, MBBindingList<TroopRowVM> list)
        {
            var row = new TroopRowVM(troop, this);
            list.Add(row);

            var children = Screen
                .Faction.BasicTroops.Concat(Screen.Faction.EliteTroops)
                .Where(t => t.Parent != null && t.Parent.Equals(troop));

            foreach (var child in children)
                AddTroopTreeInOrder(child, list);
        }
    }
}
