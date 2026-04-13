using System.Collections.Generic;
using Retinues.Behaviors.Missions;
using Retinues.Domain.Events.Models;

namespace Retinues.Behaviors.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Save a caravan or villager party from an enemy attack.
    /// </summary>
    public sealed class Feat_BoundByHonor_SafeTravels : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.BH_SafeTravels.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (!end.IsWon)
                return;

            foreach (var party in start.PlayerSide.Parties)
            {
                if (party.IsCaravan || party.IsVillager)
                {
                    Feat.Add(); // Saved a caravan or villager party.
                    break;
                }
            }
        }
    }
}
