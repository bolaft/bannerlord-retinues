using TaleWorlds.Library;
using CustomClanTroops.Game.Troops.Objects;

namespace CustomClanTroops.UI.VM
{
    public sealed class TroopEditorVM(ClanManagementMixinVM owner) : ViewModel
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

        [DataSourceProperty] public TroopListVM TroopList => new(this);

        // =========================================================================
        // Refresh
        // =========================================================================

        public void Refresh()
        {
            TroopList.Refresh();

            OnPropertyChanged(nameof(TroopList));
        }
    }
}