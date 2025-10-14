using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Game.Wrappers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop.Panel
{
    [SafeClass]
    public sealed class TroopConversionRowVM(WCharacter source) : BaseComponent
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly WCharacter _source = source;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Quick Access                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private TroopPanelVM Panel => Editor.TroopScreen.TroopPanel;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string OriginDisplay =>
            $"{Format.Crop(_source?.Name, 40)} ({Panel.GetVirtualCount(_source)})";

        [DataSourceProperty]
        public string TargetDisplay =>
            $"{Format.Crop(SelectedTroop?.Name, 40)} ({Panel.GetVirtualCount(SelectedTroop)}/{Panel.RetinueCap})";

        [DataSourceProperty]
        public bool CanRecruit => Panel.GetMaxStageable(_source, SelectedTroop) > 0;

        [DataSourceProperty]
        public bool CanRelease => Panel.GetVirtualCount(SelectedTroop) > 0;

        [DataSourceProperty]
        public int ConversionCost =>
            PendingAmount * TroopRules.ConversionCostPerUnit(SelectedTroop);

        [DataSourceProperty]
        public int PendingAmount => Panel.GetStagedConversions(_source, SelectedTroop);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteRecruit()
        {
            if (
                TroopRules.IsAllowedInContextWithPopup(_source, L.S("action_convert", "convert"))
                == false
            )
                return; // Conversion not allowed in current context

            Panel.StageConversion(_source, SelectedTroop, BatchInput());
        }

        [DataSourceMethod]
        public void ExecuteRelease()
        {
            if (
                TroopRules.IsAllowedInContextWithPopup(_source, L.S("action_convert", "convert"))
                == false
            )
                return; // Conversion not allowed in current context

            Panel.StageConversion(SelectedTroop, _source, BatchInput());
        }
    }
}
