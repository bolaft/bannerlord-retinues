using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Training
{
    /// <summary>
    /// Win a siege defense fielding only faction troops.
    /// </summary>
    public sealed class Feat_SteadfastSoldiers_HoldTheWalls : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_tr_hold_the_walls";

        static bool CustomOnly;

        protected override void OnBattleStart(MMapEvent battle)
        {
            CustomOnly = false;

            if (!battle.IsSiege)
                return;

            if (!battle.IsPlayerDefender)
                return;

            CustomOnly = Player.Party.CustomRatio == 1f;
            ;
        }

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (CustomOnly && battle.IsWon)
                Progress(1);
        }
    }
}
