using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Save a caravan or villager party from an enemy attack.
    /// </summary>
    public sealed class Feat_BoundByHonor_SafeTravels : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_ret_safe_travels";

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (end.IsLost)
                return;

            foreach (var party in start.PlayerSide.Parties)
            {
                if (party.IsCaravan || party.IsVillager)
                {
                    Progress(); // Saved a caravan or villager party.
                    break;
                }
            }
        }
    }
}
