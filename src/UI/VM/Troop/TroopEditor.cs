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
        // Public API
        // =========================================================================

        public void Refresh()
        {
            Log.Debug("Refreshing Troop Editor.");
        }
    }
}