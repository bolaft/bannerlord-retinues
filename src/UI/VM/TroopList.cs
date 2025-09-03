using TaleWorlds.Library;
using CustomClanTroops.Game.Troops.Objects;

namespace CustomClanTroops.UI.VM
{
    public sealed class TroopListVM(TroopEditorVM owner) : ViewModel
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private TroopEditorVM _owner = owner;

        private TroopRowVM _selectedRow;

        // =========================================================================
        // Selected Troop
        // =========================================================================

        public TroopCharacter SelectedTroop => _owner.SelectedTroop;

        // =========================================================================
        // Public API
        // =========================================================================

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> Troops { get; } = [];

        // =========================================================================
        // Refresh
        // =========================================================================

        public void Refresh()
        {
            
            OnPropertyChanged(nameof(Troops));
        }
    }
}