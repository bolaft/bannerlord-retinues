using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Loot
{
    /// <summary>
    /// Turn the tide of a battle involving an allied army.
    /// </summary>
    public sealed class Feat_BattlefieldTithes_TurnTheTide : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_sp_turn_the_tide";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
                return;

            if (!Player.Party.IsInArmy)
                return;

            if (Player.Party.IsArmyLeader)
                return;

            if (battle.AllyStrength * 1.25 > battle.EnemyStrength)
                return;

            Progress(1);
        }
    }
}
