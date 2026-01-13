using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Win a defensive battle with a retinue-only party.
    /// </summary>
    public sealed class Feat_Indomitable_HoldTheLine : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_ret_hold_the_line";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
                return;

            if (!battle.IsPlayerDefender)
                return;

            if (Player.Party.RetinueRatio < 1f)
                return;

            Progress(1);
        }
    }
}
