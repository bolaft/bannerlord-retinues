using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Doctrines.FeatCatalog.Loot
{
    /// <summary>
    /// Complete a quest for a lord of your clan's culture.
    /// </summary>
    public sealed class Feat_AncestralHeritage_AncestralDuty : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.DoctrineCatalog.AN_AncestralDuty.Id;

        protected override void OnQuestCompleted(QuestBase quest, WHero giver, bool success)
        {
            if (!success)
                return; // Quest failed.

            if (giver == null)
                return; // No giver.

            if (!giver.IsLord)
                return; // Not a lord.

            if (giver.Culture != Player.Clan.Culture)
                return; // Not the player's clan culture.

            Feat.Add();
        }
    }
}
