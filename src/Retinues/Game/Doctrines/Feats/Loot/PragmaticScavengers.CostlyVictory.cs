using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Loot
{
    /// <summary>
    /// Win a battle in which allies suffer over 100 casualties.
    /// </summary>
    public sealed class Feat_PragmaticScavengers_CostlyVictory : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_sp_costly_victory";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
                return;

            var kf = Filter(victims: v => v.IsAllyTroop);

            int count = kf.Filter(kills).Count();

            SetProgress(count);
        }
    }
}
