using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Win a battle against overwhelming odds while fielding mostly retinues.
    /// </summary>
    public sealed class Feat_Immortals_DefyTheTide : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_ret_defy_the_tide";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
                return;

            var ally = battle.FriendlyTroopCount;
            var enemy = battle.EnemyTroopCount;

            if (ally <= 0 || enemy <= 0)
                return;

            if (enemy < ally * 3)
                return;

            if (Player.Party.RetinueRatio < 0.75f)
                return;

            Progress(1);
        }
    }
}
