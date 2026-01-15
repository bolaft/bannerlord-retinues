using System.Linq;
using Retinues.Domain;

namespace Retinues.Game.Doctrines.Feats.Troops
{
    /// <summary>
    /// Own three caravans at the same time.
    /// </summary>
    public sealed class Feat_RoadWardens_TradeNetwork : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.RW_TradeNetwork.Id;

        protected override void OnDailyTick()
        {
            // Count the number of caravans owned by the player's clan.
            int count = Player.Clan.Parties.Count(party => party.IsCaravan);

            Feat.Set(count, bestOnly: true);
        }
    }
}
