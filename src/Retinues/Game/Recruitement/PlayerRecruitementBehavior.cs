using Retinues.Framework.Behaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Game.Recruitement
{
    internal sealed class PlayerRecruitementBehavior : BaseCampaignBehavior
    {
        protected override void OnSettlementLeft(MobileParty party, Settlement settlement)
        {
            if (party != MobileParty.MainParty)
                return;

            PlayerVolunteerSwapState.RestoreIfActive();
        }

        protected override void OnBeforeSave()
        {
            PlayerVolunteerSwapState.RestoreIfActive();
        }
    }
}
