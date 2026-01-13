using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using TaleWorlds.Core;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Have 20 retinues survive being struck down in battle.
    /// </summary>
    public sealed class Feat_Immortals_StillStanding : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_ret_still_standing";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            var kf = Filter(victims: a => a.IsPlayerTroop && a.Character.IsRetinue);

            int count = 0;

            foreach (var k in kf.Filter(kills))
            {
                if (k.State != AgentState.Unconscious)
                    continue;

                count++;
            }

            Progress(count);
        }
    }
}
