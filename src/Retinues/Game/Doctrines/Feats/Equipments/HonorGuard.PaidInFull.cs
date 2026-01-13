namespace Retinues.Game.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Pay 50 000 denars in troop wages.
    /// </summary>
    public sealed class Feat_HonorGuard_PaidInFull : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_eq_paid_in_full";

        protected override void OnDailyTick()
        {
            var wages = Player.Party.TotalWage;
            if (wages <= 0)
                return;

            Progress(wages);
        }
    }
}
