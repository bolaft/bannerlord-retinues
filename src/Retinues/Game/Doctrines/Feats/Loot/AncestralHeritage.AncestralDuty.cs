using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Doctrines.Feats.Loot
{
    /// <summary>
    /// Complete a quest for a lord of your clan's culture.
    /// </summary>
    public sealed class Feat_AncestralHeritage_AncestralDuty : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_sp_ancestral_duty";

        protected override void OnQuestCompleted(QuestBase quest, WHero giver, bool success)
        {
            if (!success)
                return;

            if (giver == null)
                return;

            if (!giver.IsLord)
                return;

            if (giver.Culture != Player.Clan.Culture)
                return;

            Progress(1);
        }
    }
}
