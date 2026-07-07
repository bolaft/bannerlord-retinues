using Retinues.Behaviors.Volunteers.Models;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Framework.Behaviors;
using TaleWorlds.CampaignSystem;

namespace Retinues.Behaviors.Volunteers
{
    /// <summary>
    /// Handles player-specific recruit-related lifecycle events for volunteer snapshot management.
    /// </summary>
    internal sealed class PlayerVolunteersBehavior : BaseCampaignBehavior
    {
        /// <summary>
        /// Once the campaign is running (all mods' models registered), make sure the active
        /// VolunteerModel is wrapped by ours — catches models registered via the generic AddModel
        /// overload (e.g. Adonnay's Troop Changer) that our AddModel(GameModel) patch can't see.
        /// </summary>
        protected override void OnSessionLaunched(CampaignGameStarter starter)
        {
            CustomVolunteerModel.EnsureWrapsActiveModel();
        }

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
