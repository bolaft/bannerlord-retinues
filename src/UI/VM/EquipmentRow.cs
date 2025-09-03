using TaleWorlds.Library;
using CustomClanTroops.Game.Troops.Objects;

namespace CustomClanTroops.UI.VM
{
    public sealed class EquipmentRowVM(EquipmentListVM owner, TroopItem item) : ViewModel
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private EquipmentListVM _owner = owner;
        private TroopItem _item = item;

        // =========================================================================
        // Selected Troop
        // =========================================================================

        public TroopCharacter SelectedTroop => _owner.SelectedTroop;

        // =========================================================================
        // Refresh
        // =========================================================================

        public void Refresh()
        {

        }
    }
}