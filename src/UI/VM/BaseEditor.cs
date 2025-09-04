using TaleWorlds.Library;
using CustomClanTroops.Wrappers.Objects;

namespace CustomClanTroops.UI.VM
{
    // Generic base editor to unify editor VMs and avoid repetition
    public abstract class BaseEditor<TSelf>(UI.ClanScreen screen) : ViewModel
        where TSelf : BaseEditor<TSelf>
    {
        // =========================================================================
        // Fields
        // =========================================================================

        protected readonly UI.ClanScreen _screen = screen;

        public UI.ClanScreen Screen => _screen;

        // =========================================================================
        // Public API
        // =========================================================================

        public WCharacter Troop => _screen.SelectedTroop;
    }
}