using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Loot
{
    /// <summary>
    /// Capture a fief of your own culture from an enemy kingdom.
    /// </summary>
    public sealed class Feat_AncestralHeritage_Homecoming : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_sp_homecoming";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
                return;

            if (!battle.IsSiege)
                return;

            if (!battle.IsPlayerAttacker)
                return;

            if (Player.Party.IsInArmy && !Player.Party.IsArmyLeader)
                return; // Must be the army leader or not in an army.

            if (battle.Settlement?.Culture != Player.Culture)
                return;

            Progress(1);
        }
    }
}
