using System.Collections.Generic;
using Retinues.Behaviors.Missions;
using Retinues.Domain;
using Retinues.Domain.Events.Models;

namespace Retinues.Behaviors.Doctrines.Feats.Loot
{
    /// <summary>
    /// Turn the tide of a battle involving an allied army.
    /// </summary>
    public sealed class Feat_BattlefieldTithes_TurnTheTide : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.BT_TurnTheTide.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (end.IsLost)
                return; // Player lost the battle.

            if (!Player.Party.IsInArmy)
                return; // Player must be in an army.

            if (Player.Party.IsArmyLeader)
                return; // Player must not be the army leader.

            if (start.PlayerSide.Strength * 1.25 > start.EnemySide.Strength)
                return; // Player side was not significantly weaker at the start.

            Feat.Add();
        }
    }
}
