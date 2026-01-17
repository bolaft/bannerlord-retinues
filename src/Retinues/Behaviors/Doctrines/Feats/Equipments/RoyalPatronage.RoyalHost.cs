using System.Collections.Generic;
using System.Linq;
using Retinues.Behaviors.Missions;
using Retinues.Domain;
using Retinues.Domain.Events.Models;

namespace Retinues.Behaviors.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Get 1 000 kills with custom kingdom troops.
    /// </summary>
    public sealed class Feat_RoyalPatronage_RoyalHost : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.RP_RoyalHost.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            var kingdom = Player.Kingdom;
            if (kingdom == null)
                return; // Player has no kingdom.

            int count = kills
                .Select(k =>
                    k.Killer.IsPlayerTroop // Player troop
                    && k.Killer.Character.IsFactionTroop // In custom troop tree
                    && k.Killer.Character.BelongsTo(kingdom) // Belongs to player's kingdom
                    && k.Victim.IsEnemyTroop // Enemy victim
                )
                .Count();

            Feat.Add(count);
        }
    }
}
