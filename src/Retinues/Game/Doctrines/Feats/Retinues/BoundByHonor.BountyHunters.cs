using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Eliminate 5 bandit parties.
    /// </summary>
    public sealed class Feat_BoundByHonor_BountyHunters : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_ret_bounty_hunters";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
                return;

            foreach (var party in battle.EnemySideParties)
                if (party.IsBandit)
                    Progress(1);
        }
    }
}
