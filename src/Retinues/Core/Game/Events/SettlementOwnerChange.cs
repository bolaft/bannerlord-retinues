using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Core.Game.Events
{
    /// <summary>
    /// Event wrapper for settlement owner changes, provides info and flags for capture, barter, or grant.
    /// </summary>
    [SafeClass]
    public class SettlementOwnerChange(
        WSettlement settlement,
        ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail,
        WHero oldOwner,
        WHero newOwner
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

        public WSettlement Settlement = settlement;

        public WHero OldOwner = oldOwner ?? null;
        public WHero NewOwner = newOwner ?? null;

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
