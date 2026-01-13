using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Win a battle of 100 or more combatants using only your retinues.
    /// </summary>
    public sealed class Feat_Vanguard_ShockAssault : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_ret_shock_assault";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
                return;

            if (battle.TotalTroopCount < 100)
                return;

            if (Player.Party.RetinueRatio < 1f)
                return;

            Progress(1);
        }
    }
}
