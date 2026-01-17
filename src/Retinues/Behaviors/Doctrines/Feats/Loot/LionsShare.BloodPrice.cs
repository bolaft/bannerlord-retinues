using System.Collections.Generic;
using System.Linq;
using Retinues.Behaviors.Missions;
using Retinues.Domain.Events.Models;

namespace Retinues.Behaviors.Doctrines.Feats.Loot
{
    /// <summary>
    /// Personally defeat 25 enemies in one battle.
    /// </summary>
    public sealed class Feat_LionsShare_BloodPrice : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.LS_BloodPrice.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            int count = kills.Count(k =>
                k.Killer.IsPlayer // Killer is the player
                && k.Victim.IsEnemyTroop // Victim is an enemy troop
            );

            Feat.Set(count, bestOnly: true);
        }
    }
}
