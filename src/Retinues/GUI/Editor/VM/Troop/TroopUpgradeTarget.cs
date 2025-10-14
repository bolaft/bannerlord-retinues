using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop
{
    /// <summary>
    /// ViewModel for a troop upgrade target. Handles display and refresh logic.
    /// </summary>
    [SafeClass]
    public sealed class TroopUpgradeTargetVM(WCharacter troop) : BaseComponent
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Name => Format.Crop(troop.Name, 40);
    }
}
