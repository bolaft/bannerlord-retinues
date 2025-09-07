using TaleWorlds.Library;
using Retinues.Core.Wrappers.Objects;

namespace Retinues.Core.UI.VM
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