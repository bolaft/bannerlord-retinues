using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Training
{
    /// <summary>
    /// Win 3 defensive battles in a row.
    /// </summary>
    public sealed class Feat_AdvancedTactics_UnyieldingDefense : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_tr_unyielding_defense";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
            {
                Reset();
                return;
            }

            if (battle.IsPlayerAttacker)
                return;

            Progress(1);
        }
    }
}
