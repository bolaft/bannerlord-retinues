namespace Retinues.Game.Doctrines.Feats.Troops
{
    /// <summary>
    /// Max out the skills of a T5 basic troop.
    /// </summary>
    public sealed class Feat_Captains_Veterans : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_trp_veterans";

        protected override void OnDailyTick()
        {
            foreach (var troop in Player.Troops)
            {
                if (!troop.IsBasic)
                    continue;

                if (troop.Tier != 5)
                    continue;

                if (troop.SkillTotalRemaining > 0)
                    continue;

                Progress(1);
                return;
            }
        }
    }
}
