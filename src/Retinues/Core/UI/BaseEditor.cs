using TaleWorlds.Library;
using CustomClanTroops.Wrappers.Objects;

namespace CustomClanTroops.UI.VM
{
    // Generic base editor to unify editor VMs and avoid repetition
    public abstract class BaseEditor<TSelf>(EditorScreenVM screen) : ViewModel
        where TSelf : BaseEditor<TSelf>
    {
        // =========================================================================
        // Fields
        // =========================================================================

        protected readonly EditorScreenVM _screen = screen;

        public EditorScreenVM Screen => _screen;

        // =========================================================================
        // Public API
        // =========================================================================

        public WCharacter SelectedTroop => _screen.SelectedTroop;
    }
}