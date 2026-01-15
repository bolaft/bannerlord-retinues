using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.FeatCatalog.Loot
{
    /// <summary>
    /// Personally defeat an enemy lord in battle.
    /// </summary>
    public sealed class Feat_LionsShare_CutTheHead : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.DoctrineCatalog.LS_CutTheHead.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            int count = kills.Count(k =>
                k.Killer.IsPlayer // Killer is the player
                && k.Victim.IsEnemyTroop // Victim is an enemy troop
                && k.Victim.Character.Hero?.IsLord == true // Victim is a hero
            );

            if (count == 0)
                return; // No enemy lords defeated by player.

            Feat.Add();
        }
    }
}
