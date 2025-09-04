using TaleWorlds.Library;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM.Equipment
{
    public sealed class EquipmentEditorVM(ClanManagementMixinVM owner) : ViewModel, IView
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private readonly ClanManagementMixinVM _owner = owner;

        // =========================================================================
        // Selected Troop
        // =========================================================================

        public WCharacter SelectedTroop => _owner.SelectedTroop;

        // =========================================================================
        // VMs
        // =========================================================================

        [DataSourceProperty] public EquipmentListVM EquipmentList => new(this);

        // =========================================================================
        // Refresh
        // =========================================================================

        public void Refresh()
        {
            Log.Debug("Refreshing Equipment Editor.");

            EquipmentList.Refresh();

            OnPropertyChanged(nameof(SelectedTroop));
            OnPropertyChanged(nameof(EquipmentList));
        }
    }
}