using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Win by yourself against 100 or more enemies without a single death on your side.
    /// </summary>
    public sealed class Feat_Immortals_PerfectVictory : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_ret_perfect_victory";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
                return;

            if (battle.EnemyTroopCount < 100)
                return;

            foreach (var party in battle.PlayerSideParties)
            {
                if (party.IsMainParty)
                    continue;

                // Not solo if there is any other party on the player side.
                return;
            }

            var kf = Filter(victims: v => v.IsPlayerTroop);

            if (kf.Filter(kills).Count() > 0)
                return; // Any player troop deaths.

            Progress(1);
        }
    }
}
