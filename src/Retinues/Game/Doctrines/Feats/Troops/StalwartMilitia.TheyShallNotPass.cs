using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Troops
{
    /// <summary>
    /// Personally slay 50 assailants during a siege defense.
    /// </summary>
    public sealed class Feat_StalwartMilitia_TheyShallNotPass : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_trp_they_shall_not_pass";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (!battle.IsSiege)
                return;

            if (!battle.IsPlayerDefender)
                return;

            var kf = Filter(killers: a => a.IsPlayerCharacter, victims: v => v.IsEnemyTroop);

            int count = kf.Filter(kills).Count();

            Progress(count);
        }
    }
}
