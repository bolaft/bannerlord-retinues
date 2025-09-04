using TaleWorlds.Library;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM.Equipment
{
    public sealed class EquipmentEditorVM(ClanScreen screen) : BaseEditor<EquipmentEditorVM>(screen), IView
    {
        // =========================================================================
        // Public API
        // =========================================================================

        public void Refresh()
        {
            Log.Debug("Refreshing Equipment Editor.");
        }
    }
}