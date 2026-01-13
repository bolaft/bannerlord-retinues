using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Loot
{
    /// <summary>
    /// Personally defeat 25 enemies in one battle.
    /// </summary>
    public sealed class Feat_LionsShare_BloodPrice : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_sp_blood_price";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
                return;

            var kf = Filter(killers: a => a.IsPlayerCharacter, victims: v => v.IsEnemyTroop);

            int count = kf.Filter(kills).Count();

            SetProgress(count);
        }
    }
}
