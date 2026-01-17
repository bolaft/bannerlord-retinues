using System.Collections.Generic;
using System.Linq;
using Retinues.Behaviors.Missions;
using Retinues.Domain.Events.Models;

namespace Retinues.Behaviors.Doctrines.Feats.Training
{
    /// <summary>
    /// Get 1000 kills with elite faction troops.
    /// </summary>
    public sealed class Feat_MastersAtArms_BattleHardened : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.MA_BattleHardened.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            int count = kills.Count(k =>
                k.Killer.IsPlayerTroop // Player troop killer
                && k.Killer.Character.IsFactionTroop // Faction troop
                && k.Killer.Character.IsElite // Elite troop
                && k.Victim.IsEnemyTroop // Enemy victim
            );

            Feat.Add(count);
        }
    }
}
