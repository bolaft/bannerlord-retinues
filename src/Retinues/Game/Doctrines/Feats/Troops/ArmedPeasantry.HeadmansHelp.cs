using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Doctrines.FeatCatalog.Troops
{
    /// <summary>
    /// Complete a quest for a village headman.
    /// </summary>
    public sealed class Feat_ArmedPeasantry_HeadmansHelp : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.DoctrineCatalog.AP_HeadmansHelp.Id;

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
