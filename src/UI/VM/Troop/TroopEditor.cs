using TaleWorlds.Library;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM.Troop
{
    public sealed class TroopEditorVM(ClanManagementMixinVM owner) : ViewModel, IView
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

        [DataSourceProperty] public TroopListVM TroopList => new(this);

        // =========================================================================
        // Refresh
        // =========================================================================

        public void Refresh()
        {
            Log.Debug("Refreshing Troop Editor.");

            TroopList.Refresh();

            OnPropertyChanged(nameof(SelectedTroop));
            OnPropertyChanged(nameof(TroopList));
        }
    }
}