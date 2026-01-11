using Retinues.Domain.Parties.Wrappers;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Framework.Behaviors;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Game.Recruitement
{
    internal sealed class PlayerRecruitementBehavior : BaseCampaignBehavior
    {
        protected override void OnSettlementLeft(WParty party, WSettlement settlement)
        {
            if (!party.IsMainParty)
                return;

            PlayerVolunteerSwapState.RestoreIfActive();
        }

        protected override void OnBeforeSave()
        {
            PlayerVolunteerSwapState.RestoreIfActive();
        }
    }
}
