using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Game.Wrappers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop
{
    /// <summary>
    /// ViewModel for a troop conversion row. Handles recruiting, releasing, cost calculation, and UI refresh.
    /// </summary>
    [SafeClass]
    public sealed class TroopConversionRowVM(
        TroopPanelVM owner,
        WCharacter origin,
        WCharacter target
    ) : BaseComponent
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly TroopPanelVM _owner = owner;
        private readonly WCharacter _origin = origin;
        private readonly WCharacter _target = target;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string OriginDisplay =>
            $"{Format.Crop(_origin?.Name, 40)} ({_owner.GetVirtualCount(_origin)})";

        [DataSourceProperty]
        public string TargetDisplay =>
            $"{Format.Crop(_target?.Name, 40)} ({_owner.GetVirtualCount(_target)}/{_owner.RetinueCap})";

        [DataSourceProperty]
        public bool CanRecruit => _owner.GetMaxStageable(_origin, _target) > 0;

        [DataSourceProperty]
        public bool CanRelease => _owner.GetVirtualCount(_target) > 0;

        [DataSourceProperty]
        public int ConversionCost => PendingAmount * TroopRules.ConversionCostPerUnit(_target);

        [DataSourceProperty]
        public int PendingAmount => _owner.GetStagedConversions(_origin, _target);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteRecruit()
        {
            if (
                TroopRules.IsAllowedInContextWithPopup(
                    _origin,
                    Editor.Faction,
                    L.S("action_convert", "convert")
                ) == false
            )
                return; // Conversion not allowed in current context

            _owner.StageConversion(_origin, _target, BatchInput());
        }

        [DataSourceMethod]
        public void ExecuteRelease()
        {
            if (
                TroopRules.IsAllowedInContextWithPopup(
                    _origin,
                    Editor.Faction,
                    L.S("action_convert", "convert")
                ) == false
            )
                return; // Conversion not allowed in current context

            _owner.StageConversion(_target, _origin, BatchInput());
        }
    }
}
