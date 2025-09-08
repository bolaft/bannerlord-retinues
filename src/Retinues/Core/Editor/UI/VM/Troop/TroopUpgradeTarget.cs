using TaleWorlds.Library;
using Retinues.Core.Game.Wrappers;

namespace Retinues.Core.Editor.UI.VM.Troop
{
    public sealed class TroopUpgradeTargetVM(WCharacter troop) : ViewModel, IView
    {
        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty]
        public string Name => Troop?.Name;

        // =========================================================================
        // Public API
        // =========================================================================

        public WCharacter Troop { get; } = troop;

        public void Refresh()
        {
            OnPropertyChanged(nameof(Name));
        }
    }
}