using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Outnumbered at least 2 to 1, win a battle while fielding only retinues.
    /// </summary>
    public sealed class Feat_Indomitable_AgainstAllOdds : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_ret_against_all_odds";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
                return;

            if (Player.Party.RetinueRatio < 1f)
                return;

            var friendly = battle.FriendlyTroopCount;
            var enemy = battle.EnemyTroopCount;

            if (friendly <= 0 || enemy <= 0)
                return;

            // Outnumbered at least 2:1.
            if (friendly * 2 > enemy)
                return;

            Progress(1);
        }
    }
}
