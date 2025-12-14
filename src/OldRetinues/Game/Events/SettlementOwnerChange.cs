using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem.Actions;

namespace OldRetinues.Game.Events
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
