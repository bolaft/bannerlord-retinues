using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Loot
{
    /// <summary>
    /// Win a battle where you are not the main commander.
    /// </summary>
    public sealed class Feat_BattlefieldTithes_SecondInCommand : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_sp_second_in_command";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
                return;

            if (!battle.IsPlayerInArmy)
                return;

            if (Player.Party.IsArmyLeader)
                return;

            Progress(1);
        }
    }
}
