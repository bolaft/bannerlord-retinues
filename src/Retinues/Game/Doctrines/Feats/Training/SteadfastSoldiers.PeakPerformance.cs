namespace Retinues.Game.Doctrines.Feats.Training
{
    /// <summary>
    /// Max out the skills of 10 faction troops.
    /// </summary>
    public sealed class Feat_SteadfastSoldiers_PeakPerformance : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_tr_peak_performance";

        protected override void OnDailyTick()
        {
            var count = 0;

            foreach (var troop in Player.Troops)
            {
                if (troop.SkillTotalRemaining > 0)
                    continue;

                count++;
            }
        }
    }
}
