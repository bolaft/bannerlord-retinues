using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Doctrines.Feats.Loot
{
    /// <summary>
    /// Complete a quest for an allied lord.
    /// </summary>
    public sealed class Feat_BattlefieldTithes_AlliesFavor : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.BT_AlliesFavor.Id;

        protected override void OnQuestCompleted(QuestBase quest, WHero giver, bool success)
        {
            if (!success)
                return; // Quest failed.

            if (giver == null)
                return; // No giver.

            if (!giver.IsLord)
                return; // Giver is not a lord.

            if (giver.Base.MapFaction.StringId != Player.Clan.MapFaction.StringId)
                return; // Giver is not an ally.

            Feat.Add();
        }
    }
}
