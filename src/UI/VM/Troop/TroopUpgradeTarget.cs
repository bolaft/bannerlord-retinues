using System.Linq;
using TaleWorlds.Library;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Logic;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM.Troop
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