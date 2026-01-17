using System.Collections.Generic;
using System.Linq;
using Retinues.Behaviors.Doctrines.Catalogs;
using Retinues.Behaviors.Missions;
using Retinues.Domain.Events.Models;

namespace Retinues.Behaviors.Doctrines.Feats.Troops
{
    /// <summary>
    /// Personally slay 50 assailants during a siege defense.
    /// </summary>
    public sealed class Feat_StalwartMilitia_TheyShallNotPass : BaseFeatBehavior
    {
        protected override string FeatId => FeatCatalog.SM_TheyShallNotPass.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (!start.IsSiegeBattle)
                return; // Not a siege battle.

            if (!start.DefenderSide.IsPlayerSide)
                return; // Player is not defending.

            int count = kills.Count(k => k.Killer.IsPlayer && k.Victim.IsEnemyTroop);

            Feat.Set(count, bestOnly: true);
        }
    }
}
