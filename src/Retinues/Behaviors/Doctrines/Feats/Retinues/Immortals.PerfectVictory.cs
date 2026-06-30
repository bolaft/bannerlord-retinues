using System.Collections.Generic;
using System.Linq;
using Retinues.Behaviors.Missions;
using Retinues.Domain.Events.Models;
using TaleWorlds.Core;

namespace Retinues.Behaviors.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Win by yourself against 100 or more enemies without a single death on your side.
    /// </summary>
    public sealed class Feat_Immortals_PerfectVictory : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.IM_PerfectVictory.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (!end.IsWon)
                return; // Player lost the battle.

            if (start.EnemySide.HealthyTroops < 100)
                return; // Not enough enemies.

            foreach (var party in start.PlayerSide.Parties)
                if (!party.IsMainParty)
                    return; // Not winning by yourself.

            if (kills.Count(k => k.Victim.IsPlayerTroop && k.State == AgentState.Killed) > 0)
                return; // A player troop actually died (knocked-out survivors don't count).

            Feat.Add();
        }
    }
}
