using System.Collections.Generic;
using System.Linq;
using Retinues.Behaviors.Missions;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Events.Models;
using Retinues.Domain.Factions.Wrappers;

namespace Retinues.Behaviors.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Get 100 kills in battle with troops wearing no foreign gear.
    /// </summary>
    public sealed class Feat_CulturalPride_ProudAndStrong : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.CP_ProudAndStrong.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (end.IsLost)
                return; // Player lost the battle.

            /// <summary>
            /// Returns true if the equipment contains any items not of the specified culture.
            /// </summary>
            static bool ContainsForeignGear(MEquipment equipment, WCulture culture)
            {
                foreach (var item in equipment.Items)
                {
                    if (item.Culture != culture)
                        return true; // Found foreign gear.
                }
                return false; // All gear is of the specified culture.
            }

            int count = kills.Count(k =>
                k.Victim.IsEnemyTroop // Enemy victim
                && k.Killer.IsPlayerTroop // Friendly killer
                && ContainsForeignGear(k.KillerEquipment, k.Killer.Character.Culture) == false // No foreign gear
            );

            Feat.Add(count);
        }
    }
}
