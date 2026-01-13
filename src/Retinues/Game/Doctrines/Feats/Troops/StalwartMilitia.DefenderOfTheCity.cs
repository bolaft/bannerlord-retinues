using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Troops
{
    /// <summary>
    /// Defend a city against a besieging army.
    /// </summary>
    public sealed class Feat_StalwartMilitia_DefenderOfTheCity : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_trp_defender_of_the_city";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
                return;

            if (!battle.IsSiege)
                return;

            if (!battle.IsPlayerDefender)
                return;

            if (!battle.IsEnemyAnArmy)
                return;

            Progress(1);
        }
    }
}
