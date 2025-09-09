using System.Linq;
using TaleWorlds.Library;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using Retinues.Core.Game;

namespace Retinues.Core.Editor.UI.VM.Troop
{
    public sealed class TroopListVM(EditorScreenVM screen) : BaseList<TroopListVM, TroopRowVM>(screen), IView
    {
        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty]
        public string RetinueName
        {
            get
            {
                if (Screen.Faction.StringId == Player.Kingdom?.StringId)
                    return Player.IsFemale ? "Queen's Guard" : "King's Guard";
                else
                    return "Retinue";
            }
        }

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> RetinueTroops { get; set; } = [];

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> EliteTroops { get; set; } = [];

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> BasicTroops { get; set; } = [];

        // =========================================================================
        // Public API
        // =========================================================================

        public override System.Collections.Generic.List<TroopRowVM> Rows => [.. RetinueTroops,.. EliteTroops, .. BasicTroops];

        public void Select(WCharacter troop)
        {
            var row = Rows.FirstOrDefault(r => r.Troop.Equals(troop));
            if (row is not null)
                Select(row);
        }

        public void Refresh()
        {
            Log.Debug("Refreshing.");

            RetinueTroops.Clear();
            foreach (var root in TroopManager.CollectRetinueTroops(Screen.Faction).Where(t => t.Parent is null))
                AddTroopTreeInOrder(root, RetinueTroops);

            Log.Debug($"Loaded {RetinueTroops.Count} retinue troops.");

            EliteTroops.Clear();
            foreach (var root in TroopManager.CollectEliteTroops(Screen.Faction).Where(t => t.Parent is null))
                AddTroopTreeInOrder(root, EliteTroops);

            Log.Debug($"Loaded {EliteTroops.Count} elite troops.");

            BasicTroops.Clear();
            foreach (var root in TroopManager.CollectBasicTroops(Screen.Faction).Where(t => t.Parent is null))
                AddTroopTreeInOrder(root, BasicTroops);

            Log.Debug($"Loaded {BasicTroops.Count} basic troops.");

            if (SelectedRow is null)
            {
                Log.Debug("No row is selected.");
                Select(RetinueTroops.FirstOrDefault() ?? EliteTroops.FirstOrDefault() ?? BasicTroops.FirstOrDefault());
            }

            if (EliteTroops.Count == 0)
                EliteTroops.Add(new TroopRowVM(null, this));

            if (BasicTroops.Count == 0)
                BasicTroops.Add(new TroopRowVM(null, this));

            Log.Debug($"Selected row troop: {SelectedRow?.Troop?.Name ?? "none"}.");

            OnPropertyChanged(nameof(SelectedRow));
            OnPropertyChanged(nameof(RetinueName));
            OnPropertyChanged(nameof(RetinueTroops));
            OnPropertyChanged(nameof(EliteTroops));
            OnPropertyChanged(nameof(BasicTroops));
        }

        // =========================================================================
        // Internals
        // =========================================================================

        private void AddTroopTreeInOrder(WCharacter troop, MBBindingList<TroopRowVM> list)
        {
            var row = new TroopRowVM(troop, this);
            list.Add(row);

            var children = Screen.Faction.BasicTroops.Concat(Screen.Faction.EliteTroops)
                .Where(t => t.Parent != null && t.Parent.Equals(troop));

            foreach (var child in children)
                AddTroopTreeInOrder(child, list);
        }
    }
}