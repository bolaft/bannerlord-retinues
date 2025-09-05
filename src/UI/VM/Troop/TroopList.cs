using System.Linq;
using TaleWorlds.Library;
using CustomClanTroops.Logic;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM.Troop
{
    public sealed class TroopListVM(UI.ClanScreen screen) : BaseList<TroopListVM, TroopRowVM>(screen), IView
    {
        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> EliteTroops { get; set; } = new();

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> BasicTroops { get; set; } = new();

        // =========================================================================
        // Public API
        // =========================================================================

        public override System.Collections.Generic.List<TroopRowVM> Rows => EliteTroops.Concat(BasicTroops).ToList();

        public void Select(WCharacter troop)
        {
            var row = Rows.FirstOrDefault(r => r.Troop.Equals(troop));
            if (row is not null)
                Select(row);
        }

        public void Refresh()
        {
            Log.Debug("Refreshing.");

            EliteTroops.Clear();
            foreach (var root in Player.Clan.EliteTroops.Where(t => t.Parent is null))
                AddTroopWithChildren(root, EliteTroops);

            Log.Debug($"Loaded {EliteTroops.Count} elite troops.");

            BasicTroops.Clear();
            foreach (var root in Player.Clan.BasicTroops.Where(t => t.Parent is null))
                AddTroopWithChildren(root, BasicTroops);

            Log.Debug($"Loaded {BasicTroops.Count} basic troops.");

            if (SelectedRow is null)
            {
                Log.Debug("No row is selected.");
                Select(EliteTroops.FirstOrDefault() ?? BasicTroops.FirstOrDefault());
            }
            else
            {
                Log.Debug($"Selected row: {SelectedRow.Troop.Name}.");
            }

            OnPropertyChanged(nameof(SelectedRow));
            OnPropertyChanged(nameof(EliteTroops));
            OnPropertyChanged(nameof(BasicTroops));
        }

        // =========================================================================
        // Internals
        // =========================================================================

        private void AddTroopWithChildren(WCharacter troop, MBBindingList<TroopRowVM> list)
        {
            var row = new TroopRowVM(troop, this);
            list.Add(row);

            var children = Player.Clan.BasicTroops.Concat(Player.Clan.EliteTroops)
                .Where(t => t.Parent != null && t.Parent.Equals(troop));

            foreach (var child in children)
                AddTroopWithChildren(child, list);
        }
    }
}