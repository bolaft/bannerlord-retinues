using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Doctrines.Feats.Troops
{
    /// <summary>
    /// Complete a quest for a village landowner.
    /// </summary>
    public sealed class Feat_ArmedPeasantry_LandownersRequest : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_trp_landowners_request";

        protected override void OnQuestCompleted(QuestBase quest, WHero giver, bool success)
        {
            if (!success)
                return;

            if (!giver.Base.IsRuralNotable && !giver.Base.IsHeadman)
                return;

            Progress(1);
        }
    }
}
