using System.Linq;

namespace Retinues.Game.Doctrines.Feats.Troops
{
    /// <summary>
    /// Own three caravans at the same time.
    /// </summary>
    public sealed class Feat_RoadWardens_TradeNetwork : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_trp_trade_network";

        protected override void OnDailyTick()
        {
            int count = Player.Clan.Parties.Count(party => party.IsCaravan);

            SetProgress(count);
        }
    }
}
