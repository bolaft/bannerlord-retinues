using System.Collections.Generic;
using Retinues.Behaviors.Missions;
using Retinues.Domain.Events.Models;

namespace Retinues.Behaviors.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Eliminate 5 bandit parties.
    /// </summary>
    public sealed class Feat_BoundByHonor_BountyHunters : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.BH_BountyHunters.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (!end.IsWon)
                return; // Player lost the battle.

            foreach (var party in start.EnemySide.Parties)
                if (party.IsBandit)
                    Feat.Add(); // Progress for each bandit party defeated.
        }
    }
}
