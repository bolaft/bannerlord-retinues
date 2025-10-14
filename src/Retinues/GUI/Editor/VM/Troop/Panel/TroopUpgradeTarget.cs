using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop.Panel
{
    /// <summary>
    /// ViewModel for a troop upgrade target. Handles display and refresh logic.
    /// </summary>
    [SafeClass]
    public sealed class TroopUpgradeTargetVM(WCharacter upgrade) : BaseComponent
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Name => Format.Crop(upgrade.Name, 40);
    }
}
