using Retinues.Domain.Parties.Wrappers;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Framework.Behaviors;

namespace Retinues.Behaviors.Volunteers
{
    /// <summary>
    /// Handles player-specific recruit-related lifecycle events for volunteer snapshot management.
    /// </summary>
    internal sealed class PlayerVolunteersBehavior : BaseCampaignBehavior
    {
        /// <summary>
        /// Restores player volunteer snapshot when the main party leaves a settlement.
        /// </summary>
        protected override void OnSettlementLeft(WParty party, WSettlement settlement)
        {
            if (!party.IsMainParty)
                return;

            PlayerVolunteerSwapState.RestoreIfActive();
        }

        /// <summary>
        /// Ensures any active volunteer snapshot is restored before saving the game.
        /// </summary>
        protected override void OnBeforeSave()
        {
            PlayerVolunteerSwapState.RestoreIfActive();
        }
    }
}
