using Retinues.Domain;

namespace Retinues.Behaviors.Doctrines.Feats.Troops
{
    /// <summary>
    /// Max out the skills of a T6 elite troop.
    /// </summary>
    public sealed class Feat_Captains_WarriorClass : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.CA_WarriorClass.Id;

        protected override void OnDailyTick()
        {
            foreach (var troop in Player.Troops)
            {
                if (!troop.IsElite)
                    continue; // Not an elite troop.

                if (troop.Tier != 6)
                    continue; // Not T6.

                if (troop.SkillTotalRemaining > 0)
                    continue; // Skills not maxed.

                Feat.Add();
                return;
            }
        }
    }
}
