namespace Retinues.Game.Doctrines.FeatCatalog.Troops
{
    /// <summary>
    /// Max out the skills of a T5 basic troop.
    /// </summary>
    public sealed class Feat_Captains_Veterans : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.DoctrineCatalog.CA_Veterans.Id;

        protected override void OnDailyTick()
        {
            foreach (var troop in Player.Troops)
            {
                if (!troop.IsBasic)
                    continue; // Not a basic troop.

                if (troop.Tier != 5)
                    continue; // Not T5.

                if (troop.SkillTotalRemaining > 0)
                    continue; // Skills not maxed.

                Feat.Add();
                return;
            }
        }
    }
}
