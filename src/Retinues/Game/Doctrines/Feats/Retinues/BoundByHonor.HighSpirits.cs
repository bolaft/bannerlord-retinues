namespace Retinues.Game.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Maintain a retinue-only party's morale above 90 for 15 days.
    /// </summary>
    public sealed class Feat_BoundByHonor_HighSpirits : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_ret_high_spirits";

        protected override void OnDailyTick()
        {
            if (Player.Party.RetinueRatio < 1f)
            {
                Reset(); // Party is not retinue-only.
                return;
            }

            if (Player.Party.Morale <= 90f)
            {
                Reset(); // Morale dropped too low.
                return;
            }

            Progress(); // Progress for the day with high morale.
        }
    }
}
