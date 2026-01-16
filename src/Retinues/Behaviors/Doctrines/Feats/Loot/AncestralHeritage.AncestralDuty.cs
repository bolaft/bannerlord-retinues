using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Doctrines.Feats.Loot
{
    /// <summary>
    /// Complete a quest for a lord of your clan's culture.
    /// </summary>
    public sealed class Feat_AncestralHeritage_AncestralDuty : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.AN_AncestralDuty.Id;

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
