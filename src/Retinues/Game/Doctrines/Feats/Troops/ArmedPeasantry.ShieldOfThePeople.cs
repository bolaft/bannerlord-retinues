using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.Feats.Troops
{
    /// <summary>
    /// Defend a village against an enemy raid.
    /// </summary>
    public sealed class Feat_ArmedPeasantry_ShieldOfThePeople : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_trp_shield_of_the_people";

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (end.IsLost)
                return; // Player lost the battle.

            if (!start.IsRaid)
                return; // Not a raid.

            if (!start.DefenderSide.IsPlayerSide)
                return; // Player is not defending.

            Progress();
        }
    }
}
