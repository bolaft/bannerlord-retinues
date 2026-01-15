using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Doctrines.FeatCatalog.Loot
{
    /// <summary>
    /// Complete a quest for an allied lord.
    /// </summary>
    public sealed class Feat_BattlefieldTithes_AlliesFavor : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.DoctrineCatalog.BT_AlliesFavor.Id;

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
