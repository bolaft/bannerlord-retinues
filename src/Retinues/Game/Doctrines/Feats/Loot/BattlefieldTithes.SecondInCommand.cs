using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.FeatCatalog.Loot
{
    /// <summary>
    /// Win a battle where you are not the main commander.
    /// </summary>
    public sealed class Feat_BattlefieldTithes_SecondInCommand : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.DoctrineCatalog.BT_SecondInCommand.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (end.IsLost)
                return; // Player lost the battle.

            if (!start.IsPlayerInArmy)
                return; // Player must be in an army.

            if (Player.Party.IsArmyLeader)
                return; // Player must not be the army leader.

            Feat.Add();
        }
    }
}
