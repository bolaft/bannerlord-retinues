using Retinues.Domain.Events.Models;

namespace Retinues.Game.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Field a unit wearing equipment worth over 100 000 denars.
    /// </summary>
    public sealed class Feat_HonorGuard_GoldenLegion : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.HG_GoldenLegion.Id;

        protected override void OnBattleStart(MMapEvent battle)
        {
            foreach (var e in Player.Party.MemberRoster.Elements)
            {
                if (e.Number <= 0)
                    continue; // Skip empty.

                var troop = e.Troop;

                if (troop.IsHero)
                    continue; // Skip heroes.

                if (!troop.IsFactionTroop)
                    continue; // Skip non-custom troops.

                // Check each equipment.
                foreach (var eq in troop.Equipments)
                {
                    if (!IsValidForBattle(eq, battle))
                        continue; // Not valid for this battle.

                    if (eq.Value <= 100000)
                        continue; // Not expensive enough.

                    Feat.Add();
                    return;
                }
            }
        }
    }
}
