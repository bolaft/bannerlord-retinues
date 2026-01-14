using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.Feats.Training
{
    /// <summary>
    /// Win a siege defense fielding only faction troops.
    /// </summary>
    public sealed class Feat_SteadfastSoldiers_HoldTheWalls : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_tr_hold_the_walls";

        static bool IsCustomOnly;

        protected override void OnBattleStart(MMapEvent battle)
        {
            // Check if the player's party is custom-only at the start of the battle.
            IsCustomOnly = Player.Party.CustomRatio == 1f;
        }

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (end.IsLost)
                return; // Player lost the battle.

            if (!IsCustomOnly)
                return; // Not all custom troops.

            if (!start.IsSiegeBattle)
                return; // Not a siege battle.

            if (!start.DefenderSide.IsPlayerSide)
                return; // Player is not defending.

            Progress();
        }
    }
}
