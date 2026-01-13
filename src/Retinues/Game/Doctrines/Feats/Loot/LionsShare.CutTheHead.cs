using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Loot
{
    /// <summary>
    /// Personally defeat an enemy lord in battle.
    /// </summary>
    public sealed class Feat_LionsShare_CutTheHead : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_sp_cut_the_head";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (!battle.IsWon)
                return;

            var kf = Filter(
                killers: a => a.IsPlayerCharacter,
                victims: v => v.IsEnemyTroop && v.Character.IsHero
            );

            var count = kf.Filter(kills).Count();
            if (count == 0)
                return;

            Progress(1);
        }
    }
}
