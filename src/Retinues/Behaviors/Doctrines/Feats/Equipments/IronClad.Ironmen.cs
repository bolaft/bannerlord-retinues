using System.Collections.Generic;
using Retinues.Behaviors.Missions;
using Retinues.Domain;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Events.Models;
using TaleWorlds.Core;

namespace Retinues.Behaviors.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Win a battle fielding only troops wearing full metal armor.
    /// </summary>
    public sealed class Feat_Ironclad_Ironmen : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.IR_Ironmen.Id;

        // Tracks if all troops are wearing full metal armor.
        static bool FullMetalOnly;

        protected override void OnBattleStart(MMapEvent battle)
        {
            FullMetalOnly = false; // Assume false until proven otherwise.

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
                        continue;

                    if (!IsFullMetal(eq))
                        return; // Found non-full-metal equipment, exit early.
                }
            }

            FullMetalOnly = true; // All checked equipments are full metal.
        }

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (!end.IsWon)
                return; // Player lost the battle.

            if (!FullMetalOnly)
                return; // Not all troops were wearing full metal armor.

            Feat.Add();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Check if the equipment is full metal armor.
        /// </summary>
        private static bool IsFullMetal(MEquipment eq)
        {
            if (eq == null)
                return false;

            foreach (var item in eq.Items)
            {
                if (item == null)
                    continue;

                // Only consider armor pieces (ignore weapons, shields, etc.).
                var armor = item.ArmorComponent;
                if (armor == null)
                    continue;

                // "Full metal" = no soft armour pieces. The material enum is
                // None/Cloth/Leather/Chainmail/Plate, so a Cloth or Leather piece disqualifies.
                var material = armor.MaterialType;

                if (
                    material == ArmorComponent.ArmorMaterialTypes.Cloth
                    || material == ArmorComponent.ArmorMaterialTypes.Leather
                )
                    return false; // A cloth/leather armour piece — not full metal.
            }

            return true;
        }
    }
}
