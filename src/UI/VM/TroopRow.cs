using TaleWorlds.Library;
using CustomClanTroops.Game.Troops.Objects;

namespace CustomClanTroops.UI.VM
{
    public sealed class TroopRowVM(TroopListVM owner, TroopCharacter character) : ViewModel
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private TroopListVM _owner = owner;
        
        private TroopCharacter _character = character;

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