namespace Retinues.Game.Doctrines.Feats.Troops
{
    public sealed class Feat_StalwartMilitia_WatchersOnTheWalls : FeatCampaignBehavior
    {
        /// <summary>
        /// Raise the militia value of a fief to 400.
        /// </summary>
        protected override string FeatId => "feat_trp_watchers_on_the_walls";

        protected override void OnDailyTick()
        {
            float highest = 0f;

            foreach (var fief in Player.Clan.Fiefs)
            {
                var militia = fief.Militia;

                if (militia > highest)
                    highest = militia;
            }

            SetProgress((int)highest);
        }
    }
}
