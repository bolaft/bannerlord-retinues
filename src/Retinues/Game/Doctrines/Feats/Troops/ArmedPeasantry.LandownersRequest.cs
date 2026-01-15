using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Doctrines.Feats.Troops
{
    /// <summary>
    /// Complete a quest for a village landowner.
    /// </summary>
    public sealed class Feat_ArmedPeasantry_LandownersRequest : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.AP_LandownersRequest.Id;

        protected override void OnQuestCompleted(QuestBase quest, WHero giver, bool success)
        {
            if (!success)
                return; // Quest failed.

            if (!giver.Base.IsRuralNotable && !giver.Base.IsHeadman)
                return; // Giver is not a landowner.

            Feat.Add();
        }
    }
}
