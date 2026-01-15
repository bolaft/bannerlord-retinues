using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.Feats.Loot
{
    /// <summary>
    /// Win a battle in which allies suffer over 100 casualties.
    /// </summary>
    public sealed class Feat_PragmaticScavengers_CostlyVictory : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.PR_CostlyVictory.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (end.IsLost)
                return; // Player lost the battle.

            int count = kills.Count(k =>
                k.Victim.IsAllyTroop // Victim is an allied troop
            );

            Feat.Set(count, bestOnly: true);
        }
    }
}
