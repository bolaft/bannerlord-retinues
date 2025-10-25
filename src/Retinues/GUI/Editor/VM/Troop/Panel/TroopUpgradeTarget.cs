using System.Collections.Generic;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop.Panel
{
    /// <summary>
    /// ViewModel representing a single upgrade target for a troop.
    /// </summary>
    [SafeClass]
    public sealed class TroopUpgradeTargetVM(WCharacter upgradeTarget) : BaseVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly WCharacter UpgradeTarget = upgradeTarget;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override Dictionary<UIEvent, string[]> EventMap =>
            new()
            {
                [UIEvent.Troop] = [nameof(Name)],
                [UIEvent.Equip] = [nameof(FormationClassIcon)],
            };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string Name => Format.Crop(UpgradeTarget.Name, 40);

        /* ━━━━━━━━━ Icon ━━━━━━━━━ */

        [DataSourceProperty]
        public string FormationClassIcon
        {
            get
            {
                return (UpgradeTarget?.FormationClass) switch
                {
                    FormationClass.Infantry => @"General\TroopTypeIcons\icon_troop_type_infantry",
                    FormationClass.Ranged => @"General\TroopTypeIcons\icon_troop_type_bow",
                    FormationClass.Cavalry => @"General\TroopTypeIcons\icon_troop_type_cavalry",
                    FormationClass.HorseArcher =>
                        @"General\TroopTypeIcons\icon_troop_type_horse_archer",
                    _ => @"General\TroopTypeIcons\icon_troop_type_infantry",
                };
            }
        }
    }
}
