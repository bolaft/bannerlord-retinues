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
        // Data Bindings
        // =========================================================================

        [DataSourceProperty]
        public string Name => _owner.SelectedTroop?.Name;

        [DataSourceProperty]
        public string Gender => _owner.SelectedTroop != null && _owner.SelectedTroop.IsFemale ? "Female" : "Male";

        // =========================================================================
        // Public API
        // =========================================================================

        public void Refresh()
        {
            Log.Debug("Refreshing Equipment Editor.");

            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Gender));
        }
    }
}