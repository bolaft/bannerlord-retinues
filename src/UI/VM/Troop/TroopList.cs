using System.Linq;
using TaleWorlds.Library;
using CustomClanTroops.Logic;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM.Troop
{
    public sealed class TroopListVM(TroopEditorVM owner) : ViewModel, IView
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private readonly TroopEditorVM _owner = owner;

        // =========================================================================
        // Selected Troop
        // =========================================================================

        public WCharacter SelectedTroop => _owner.SelectedTroop;

        // =========================================================================
        // Public API
        // =========================================================================

        [DataSourceProperty]
        public TroopRowVM SelectedRow => EliteTroops.FirstOrDefault(t => t.IsSelected) ?? BasicTroops.FirstOrDefault(t => t.IsSelected);

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> EliteTroops { get; } = [];

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> BasicTroops { get; } = [];

        // =========================================================================
        // Refresh
        // =========================================================================

        public void Refresh()
        {
            Log.Debug("Refreshing Troop List.");

            foreach (var troop in Player.Clan.EliteTroops)
                EliteTroops.Add(new TroopRowVM(troop, this));
            
            Log.Debug($"Loaded {EliteTroops.Count} elite troops.");

            foreach (var troop in Player.Clan.BasicTroops)
                BasicTroops.Add(new TroopRowVM(troop, this));

            Log.Debug($"Loaded {BasicTroops.Count} basic troops.");

            if (SelectedRow is null)
                // Select first elite troop by default
                EliteTroops[0].IsSelected = true;

            OnPropertyChanged(nameof(SelectedRow));
            OnPropertyChanged(nameof(EliteTroops));
            OnPropertyChanged(nameof(BasicTroops));
        }
    }
}