using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Training
{
    /// <summary>
    /// Defeat a party twice your size using only faction troops.
    /// </summary>
    public sealed class Feat_IronDiscipline_DisciplinedVictory : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_tr_disciplined_victory";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
                return;

            if (battle.FriendlyTroopCount * 2 > battle.EnemyTroopCount)
                return;

            if (Player.Party.CustomRatio < 1f)
                return;

            Progress(1);
        }
    }
}
