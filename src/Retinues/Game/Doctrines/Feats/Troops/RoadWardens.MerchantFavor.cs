using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Doctrines.Feats.Troops
{
    /// <summary>
    /// Complete a quest for a town merchant notable.
    /// </summary>
    public sealed class Feat_RoadWardens_MerchantsFavor : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_trp_merchants_favor";

        protected override void OnQuestCompleted(QuestBase quest, WHero giver, bool success)
        {
            if (!success)
                return;

            if (!giver.Base.IsMerchant)
                return;

            Progress(1);
        }
    }
}
