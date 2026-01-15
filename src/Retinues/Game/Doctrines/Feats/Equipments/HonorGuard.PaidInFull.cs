namespace Retinues.Game.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Pay 50 000 denars in troop wages.
    /// </summary>
    public sealed class Feat_HonorGuard_PaidInFull : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.HG_PaidInFull.Id;

        protected override void OnDailyTick()
        {
            // Get total daily troop wages.
            var wages = Player.Party.TotalWage;

            Feat.Add(wages);
        }
    }
}
