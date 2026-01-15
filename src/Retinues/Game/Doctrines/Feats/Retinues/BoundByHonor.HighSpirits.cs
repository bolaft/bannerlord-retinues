namespace Retinues.Game.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Maintain a retinue-only party's morale above 90 for 15 days.
    /// </summary>
    public sealed class Feat_BoundByHonor_HighSpirits : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.BH_HighSpirits.Id;

        protected override void OnDailyTick()
        {
            if (Player.Party.RetinueRatio < 1f)
            {
                Feat.Reset(); // Party is not retinue-only.
                return;
            }

            if (Player.Party.Morale <= 90f)
            {
                Feat.Reset(); // Morale dropped too low.
                return;
            }

            Feat.Add(); // Progress for the day with high morale.
        }
    }
}
