using System.Collections.Generic;
using Retinues.Behaviors.Missions;
using Retinues.Domain;
using Retinues.Domain.Events.Models;

namespace Retinues.Behaviors.Doctrines.Feats.Loot
{
    /// <summary>
    /// Win a battle where you are not the main commander.
    /// </summary>
    public sealed class Feat_BattlefieldTithes_SecondInCommand : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.BT_SecondInCommand.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (!end.IsWon)
                return; // Player lost the battle.

            if (!start.IsPlayerInArmy)
                return; // Player must be in an army.

            if (Player.Party.IsArmyLeader)
                return; // Player must not be the army leader.

            Feat.Add();
        }
    }
}
