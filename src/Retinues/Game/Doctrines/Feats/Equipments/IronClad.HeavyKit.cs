using Retinues.Domain;
using Retinues.Domain.Events.Models;

namespace Retinues.Game.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Field a unit wearing equipment weighing over 60 kg.
    /// </summary>
    public sealed class Feat_Ironclad_HeavyKit : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.IR_HeavyKit.Id;

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

                    if (eq.Weight <= 60)
                        continue; // Not heavy enough.

                    Feat.Add();
                    return;
                }
            }
        }
    }
}
