using System.Collections.Generic;
using Retinues.Domain;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.Feats.Equipments
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
            if (end.IsLost)
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

                var materialObj = armor.MaterialType;
                var material = materialObj.ToString() ?? string.Empty;

                string[] validMaterials = ["steel", "iron", "metal"];

                foreach (var valid in validMaterials)
                {
                    if (material.IndexOf(valid, System.StringComparison.OrdinalIgnoreCase) == 0)
                        return false;
                }
            }

            return true;
        }
    }
}
