using TaleWorlds.Library;
using CustomClanTroops.Game.Troops.Objects;

namespace CustomClanTroops.UI.VM
{
    public sealed class EquipmentEditorVM(ClanManagementMixinVM owner) : ViewModel
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private ClanManagementMixinVM _owner = owner;

        // =========================================================================
        // Selected Troop
        // =========================================================================

        public TroopCharacter SelectedTroop => _owner.SelectedTroop;

        // =========================================================================
        // VMs
        // =========================================================================

        [DataSourceProperty] public EquipmentListVM EquipmentList => new(this);

        // =========================================================================
        // Refresh
        // =========================================================================

        public void Refresh()
        {
            EquipmentList.Refresh();

            OnPropertyChanged(nameof(EquipmentList));
        }
    }
}