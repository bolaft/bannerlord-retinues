using System.Collections.Generic;
using System.Linq;
using Retinues.Behaviors.Missions;
using Retinues.Domain.Events.Models;

namespace Retinues.Behaviors.Doctrines.Feats.Loot
{
    /// <summary>
    /// Personally defeat 5 tier 5+ troops in one battle.
    /// </summary>
    public sealed class Feat_LionsShare_HighValueTargets : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.LS_HighValueTargets.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (end.IsLost)
                return; // Player lost the battle.

            int count = kills.Count(k =>
                k.Killer.IsPlayer // Killer is the player
                && k.Victim.IsEnemyTroop // Victim is an enemy troop
                && !k.Victim.Character.IsHero // Victim is not a hero
                && k.Victim.Character.Tier >= 5 // Victim is tier 5 or higher
            );

            Feat.Set(count, bestOnly: true);
        }
    }
}
