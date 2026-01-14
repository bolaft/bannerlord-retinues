using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.Feats.Loot
{
    /// <summary>
    /// Win a battle while part of an allied lord's army.
    /// </summary>
    public sealed class Feat_PragmaticScavengers_MarchTogether : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_sp_march_together";

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

            Progress();
        }
    }
}
