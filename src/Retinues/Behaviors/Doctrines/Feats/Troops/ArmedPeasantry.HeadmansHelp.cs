using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Behaviors.Doctrines.Feats.Troops
{
    /// <summary>
    /// Complete a quest for a village headman.
    /// </summary>
    public sealed class Feat_ArmedPeasantry_HeadmansHelp : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.AP_HeadmansHelp.Id;

        protected override void OnQuestCompleted(QuestBase quest, WHero giver, bool success)
        {
            if (!success)
                return; // Quest failed.

            if (!giver.Base.IsHeadman)
                return; // Giver is not a headman.

            Feat.Add();
        }
    }
}
