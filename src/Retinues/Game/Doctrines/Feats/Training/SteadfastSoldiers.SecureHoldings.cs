namespace Retinues.Game.Doctrines.Feats.Training
{
    /// <summary>
    /// Raise the security value of a fief to 60.
    /// </summary>
    public sealed class Feat_SteadfastSoldiers_SecureHoldings : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_tr_secure_holdings";

        protected override void OnDailyTick()
        {
            float highest = 0f;

            foreach (var fief in Player.Clan.Fiefs)
            {
                var security = fief.Security;

                if (security > highest)
                    highest = security;
            }

            SetProgress((int)highest);
        }
    }
}
