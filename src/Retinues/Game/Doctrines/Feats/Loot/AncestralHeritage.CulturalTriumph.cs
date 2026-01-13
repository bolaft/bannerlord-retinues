using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Events.Models;
using Retinues.Domain.Factions.Wrappers;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Loot
{
    /// <summary>
    /// Single-handedly win a battle against an enemy army of a different culture.
    /// </summary>
    public sealed class Feat_AncestralHeritage_CulturalTriumph : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_sp_cultural_triumph";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
                return;

            if (!battle.IsEnemyAnArmy)
                return;

            foreach (var party in battle.PlayerSideParties)
                if (party != Player.Party)
                    return; // Must be the main party only.

            var army = battle.EnemySideParties.FirstOrDefault()?.Army;
            var culture = WCulture.Get(army?.LeaderParty.LeaderHero.Culture);

            if (culture != Player.Clan.Culture)
                return;

            Progress(1);
        }
    }
}
