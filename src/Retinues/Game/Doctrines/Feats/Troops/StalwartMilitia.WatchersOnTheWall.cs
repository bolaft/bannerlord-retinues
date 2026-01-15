namespace Retinues.Game.Doctrines.Feats.Troops
{
    public sealed class Feat_StalwartMilitia_WatchersOnTheWalls : BaseFeatBehavior
    {
        /// <summary>
        /// Raise the militia value of a fief to 400.
        /// </summary>
        protected override string FeatId => Catalogs.FeatCatalog.SM_WatchersOnTheWalls.Id;

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
