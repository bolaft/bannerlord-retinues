using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.Feats.Troops
{
    /// <summary>
    /// Personally slay 50 assailants during a siege defense.
    /// </summary>
    public sealed class Feat_StalwartMilitia_TheyShallNotPass : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_trp_they_shall_not_pass";

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (!start.IsSiegeBattle)
                return; // Not a siege battle.

            if (!start.DefenderSide.IsPlayerSide)
                return; // Player is not defending.

            int count = kills.Count(k => k.Killer.IsPlayer && k.Victim.IsEnemyTroop);

            SetProgress(count);
        }
    }
}
