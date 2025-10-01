using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Core.Game.Events
{
    [SafeClass]
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

        public WCharacter OldOwner = oldOwner ?? null;
        public WCharacter NewOwner = newOwner ?? null;

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
