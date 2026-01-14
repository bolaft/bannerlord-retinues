using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.Feats.Loot
{
    /// <summary>
    /// Capture a fief of your own culture from an enemy kingdom.
    /// </summary>
    public sealed class Feat_AncestralHeritage_Homecoming : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_sp_homecoming";

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (end.IsLost)
                return; // Player lost the battle.

            if (!end.IsSiegeBattle)
                return; // Must be a siege battle.

            if (!end.AttackerSide.IsPlayerSide)
                return; // Player must be the attacker.

            if (Player.Party.IsInArmy && !Player.Party.IsArmyLeader)
                return; // Must be the army leader or not in an army.

            if (end.Settlement?.Culture != Player.Culture)
                return; // Settlement is not of player's culture.

            Progress();
        }
    }
}
