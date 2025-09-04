using System.Collections.Generic;
using TaleWorlds.Library;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM.Equipment
{
    public sealed class EquipmentEditorVM(ClanScreen screen) : BaseEditor<EquipmentEditorVM>(screen), IView
    {

        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty]
        public bool CanUnequip
        {
            get
            {
                if (Screen.IsDefaultMode)
                    return false; // Only show in equipment mode
                if (Troop.Equipment.Items.Count == 0)
                    return false; // No equipment to unequip

                return true;
            }
        }

        // =========================================================================
        // Public API
        // =========================================================================

        public void Refresh()
        {
            Log.Debug("Refreshing Equipment Editor.");

            OnPropertyChanged(nameof(CanUnequip));
        }
    }
}