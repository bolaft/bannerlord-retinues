using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop.Panel
{
    /// <summary>
    /// ViewModel for a troop upgrade target. Handles display and refresh logic.
    /// </summary>
    [SafeClass]
    public sealed class TroopUpgradeTargetVM : BaseComponent
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public readonly WCharacter Upgrade;

        public TroopUpgradeTargetVM(WCharacter upgrade)
        {
            Log.Info("Building TroopUpgradeTargetVM...");

            Upgrade = upgrade;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Name => Format.Crop(Upgrade?.Name, 40);
    }
}
