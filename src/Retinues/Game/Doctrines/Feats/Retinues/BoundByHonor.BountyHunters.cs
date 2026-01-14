using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Eliminate 5 bandit parties.
    /// </summary>
    public sealed class Feat_BoundByHonor_BountyHunters : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_ret_bounty_hunters";

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (end.IsLost)
                return; // Player lost the battle.

            foreach (var party in start.EnemySide.Parties)
                if (party.IsBandit)
                    Progress(); // Progress for each bandit party defeated.
        }
    }
}
