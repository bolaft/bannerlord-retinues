using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Training
{
    /// <summary>
    /// Get 1000 kills with elite faction troops.
    /// </summary>
    public sealed class Feat_MastersAtArms_BattleHardened : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_tr_battle_hardened";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            // "Elite faction troops" = custom tree + elite (not retinues).
            var kf = Filter(
                killers: a => a.IsPlayerTroop && a.IsCustom && a.Character.IsElite,
                victims: v => v.IsEnemyTroop
            );

            int count = kf.Filter(kills).Count();

            Progress(count);
        }
    }
}
