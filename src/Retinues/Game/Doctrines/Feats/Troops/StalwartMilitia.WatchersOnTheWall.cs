namespace Retinues.Game.Doctrines.FeatCatalog.Troops
{
    public sealed class Feat_StalwartMilitia_WatchersOnTheWalls : FeatCampaignBehavior
    {
        /// <summary>
        /// Raise the militia value of a fief to 400.
        /// </summary>
        protected override string FeatId => Catalogs.DoctrineCatalog.SM_WatchersOnTheWalls.Id;

        protected override void OnDailyTick()
        {
            int highest = 0;

            foreach (var fief in Player.Clan.Fiefs)
            {
                var militia = fief.Militia;

                // Track the highest militia value among all fiefs.
                if (militia > highest)
                    highest = (int)militia;
            }

            Feat.Set(highest, bestOnly: true);
        }
    }
}
