using Retinues.Domain;

namespace Retinues.Game.Doctrines.Feats.Training
{
    /// <summary>
    /// Raise the security value of a fief to 60.
    /// </summary>
    public sealed class Feat_SteadfastSoldiers_SecureHoldings : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.SS_SecureHoldings.Id;

        protected override void OnDailyTick()
        {
            int highest = 0;

            foreach (var fief in Player.Clan.Fiefs)
            {
                var security = fief.Security;

                // Track the highest security value among all fiefs.
                if (security > highest)
                    highest = (int)security;
            }

            Feat.Set(highest);
        }
    }
}
