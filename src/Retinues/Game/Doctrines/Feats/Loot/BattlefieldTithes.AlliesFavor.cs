using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Doctrines.Feats.Loot
{
    /// <summary>
    /// Complete a quest for an allied lord.
    /// </summary>
    public sealed class Feat_BattlefieldTithes_AlliesFavor : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_sp_allies_favor";

        protected override void OnQuestCompleted(QuestBase quest, WHero giver, bool success)
        {
            if (!success)
                return;

            if (giver == null)
                return;

            // Must be a lord.
            if (!giver.IsLord)
                return;

            if (giver.Base.MapFaction.StringId != Player.Clan.MapFaction.StringId)
                return;

            Progress(1);
        }
    }
}
