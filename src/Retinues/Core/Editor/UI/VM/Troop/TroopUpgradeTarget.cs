using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.Library;

namespace Retinues.Core.Editor.UI.VM.Troop
{
    /// <summary>
    /// ViewModel for a troop upgrade target. Handles display and refresh logic.
    /// </summary>
    [SafeClass]
    public sealed class TroopUpgradeTargetVM(WCharacter troop) : ViewModel
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Name => Troop?.Name;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter Troop { get; } = troop;

        public void Refresh()
        {
            OnPropertyChanged(nameof(Name));
        }
    }
}
