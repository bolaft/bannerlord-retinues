using Retinues.Domain;

namespace Retinues.Behaviors.Doctrines.Feats.Training
{
    /// <summary>
    /// Max out the skills of 10 faction troops.
    /// </summary>
    public sealed class Feat_SteadfastSoldiers_PeakPerformance : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.SS_PeakPerformance.Id;

        protected override void OnDailyTick()
        {
            int count = 0;

            foreach (var troop in Player.Troops)
            {
                if (troop.SkillTotalRemaining > 0)
                    continue; // Troop not maxed out.

                count++; // Count maxed out troop.
            }
        }
    }
}
