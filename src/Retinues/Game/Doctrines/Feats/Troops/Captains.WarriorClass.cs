namespace Retinues.Game.Doctrines.Feats.Troops
{
    /// <summary>
    /// Max out the skills of a T6 elite troop.
    /// </summary>
    public sealed class Feat_Captains_WarriorClass : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_trp_warrior_class";

        protected override void OnDailyTick()
        {
            foreach (var troop in Player.Troops)
            {
                if (!troop.IsElite)
                    continue;

                if (troop.Tier != 6)
                    continue;

                if (troop.SkillTotalRemaining > 0)
                    continue;

                Progress(1);
                return;
            }
        }
    }
}
