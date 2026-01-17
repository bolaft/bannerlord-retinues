using System.Collections.Generic;
using Retinues.Behaviors.Missions;
using Retinues.Domain;
using Retinues.Domain.Events.Models;

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
            if (end.IsLost)
                return; // Player lost the battle.

            if (!start.IsEnemyInArmy)
                return; // Enemy is not an army.

            foreach (var party in start.PlayerSide.Parties)
                if (party != Player.Party)
                    return; // Must be the main party only.

            var culture = start.EnemySide.LeaderParty.Leader.Culture;
            if (culture != Player.Clan.Culture)
                return; // Different culture.

            Feat.Add();
        }
    }
}
