using Retinues.Core.Game.Wrappers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Core.Game.Events
{
    public class SettlementOwnerChange(
        Settlement settlement,
        ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail,
        WCharacter oldOwner,
        WCharacter newOwner
    )
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail _detail =
            detail;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Info                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public Settlement Settlement = settlement;

        public WCharacter OldOwner = oldOwner;
        public WCharacter NewOwner = newOwner;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool WasCaptured =>
            _detail is ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege;
        public bool WasBartered =>
            _detail is ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByBarter;
        public bool WasGranted =>
            _detail is ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByKingDecision;
    }
}
