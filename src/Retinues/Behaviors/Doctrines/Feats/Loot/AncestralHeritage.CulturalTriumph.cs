using System.Collections.Generic;
using System.Linq;
using Retinues.Behaviors.Missions;
using Retinues.Domain;
using Retinues.Domain.Events.Models;
using Retinues.Domain.Factions.Wrappers;

namespace Retinues.Behaviors.Doctrines.Feats.Loot
{
    /// <summary>
    /// Single-handedly win a battle against an enemy army of a different culture.
    /// </summary>
    public sealed class Feat_AncestralHeritage_CulturalTriumph : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.AN_CulturalTriumph.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (!end.IsWon)
                return; // Player lost the battle.

            if (!start.IsEnemyInArmy)
                return; // Enemy is not an army.

            foreach (var party in start.PlayerSide.Parties)
                if (party != Player.Party)
                    return; // Must be the main party only.

            if (!HasDifferentLeaderCulture(start.EnemySide, Player.Clan?.Culture))
                return;

            Feat.Add();
        }

        internal static bool HasDifferentLeaderCulture(
            MMapEvent.SideData enemySide,
            WCulture playerCulture
        )
        {
            if (enemySide == null || string.IsNullOrEmpty(enemySide.LeaderPartyId) || playerCulture == null)
                return false;

            // Resolve the captured leader: battle results can detach the hero from the live party.
            var leader = enemySide.PartyData
                .FirstOrDefault(p => p != null && p.PartyId == enemySide.LeaderPartyId)
                ?.Hero;
            var enemyCulture = leader?.Culture;

            // Missing information cannot establish a victory over a different culture.
            return enemyCulture != null && enemyCulture != playerCulture;
        }
    }
}
