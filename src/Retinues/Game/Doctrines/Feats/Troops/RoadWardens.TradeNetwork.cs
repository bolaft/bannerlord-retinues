using System.Linq;

namespace Retinues.Game.Doctrines.FeatCatalog.Troops
{
    /// <summary>
    /// Own three caravans at the same time.
    /// </summary>
    public sealed class Feat_RoadWardens_TradeNetwork : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.DoctrineCatalog.RW_TradeNetwork.Id;

        protected override void OnDailyTick()
        {
            // Count the number of caravans owned by the player's clan.
            int count = Player.Clan.Parties.Count(party => party.IsCaravan);

            Feat.Set(count, bestOnly: true);
        }
    }
}
