using Retinues.Domain;

namespace Retinues.Game.Doctrines.Feats.Training
{
    /// <summary>
    /// Lead an army for 10 days in a row.
    /// </summary>
    public sealed class Feat_IronDiscipline_General : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.ID_General.Id;

        protected override void OnDailyTick()
        {
            if (!Player.Party.IsArmyLeader)
                Feat.Add(); // Progress for the day as army leader.
            else
                Feat.Reset(); // Reset progress if not army leader.
        }
    }
}
