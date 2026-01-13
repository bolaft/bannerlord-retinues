using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Doctrines.Feats.Troops
{
    /// <summary>
    /// Complete a quest for a village headman.
    /// </summary>
    public sealed class Feat_ArmedPeasantry_HeadmansHelp : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_trp_headmans_help";

        protected override void OnQuestCompleted(QuestBase quest, WHero giver, bool success)
        {
            if (!success)
                return;

            if (!giver.Base.IsHeadman)
                return;

            Progress(1);
        }
    }
}
