using System.Collections.Generic;
using Retinues.Behaviors.Missions;
using Retinues.Domain;
using Retinues.Domain.Events.Models;

namespace Retinues.Behaviors.Doctrines.Feats.Loot
{
    /// <summary>
    /// Win a battle while part of an allied lord's army.
    /// </summary>
    public sealed class Feat_PragmaticScavengers_MarchTogether : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.PR_MarchTogether.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (!end.IsWon)
                return; // Player lost the battle.

            if (!Player.Party.IsInArmy)
                return; // Player must be in an army.

            if (Player.Party.IsArmyLeader)
                return; // Player must not be the army leader.

            Feat.Add();
        }
    }
}
