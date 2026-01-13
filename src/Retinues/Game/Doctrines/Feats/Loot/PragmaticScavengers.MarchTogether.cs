using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Loot
{
    /// <summary>
    /// Win a battle while part of an allied lord's army.
    /// </summary>
    public sealed class Feat_PragmaticScavengers_MarchTogether : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_sp_march_together";

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
