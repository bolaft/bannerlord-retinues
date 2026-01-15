using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Doctrines.FeatCatalog.Troops
{
    /// <summary>
    /// Complete a quest for a town merchant notable.
    /// </summary>
    public sealed class Feat_RoadWardens_MerchantsFavor : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.DoctrineCatalog.RW_MerchantsFavor.Id;

        protected override void OnQuestCompleted(QuestBase quest, WHero giver, bool success)
        {
            if (!success)
                return; // Quest failed.

            if (!giver.Base.IsMerchant)
                return; // Not a merchant notable.

            Feat.Add();
        }
    }
}
