using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Save a caravan or villager party from an enemy attack.
    /// </summary>
    public sealed class Feat_BoundByHonor_SafeTravels : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_ret_safe_travels";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
                return;

            foreach (var party in battle.PlayerSideParties)
            {
                if (party.IsCaravan || party.IsVillager)
                {
                    Progress(1);
                    break;
                }
            }
        }
    }
}
