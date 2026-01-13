using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Get 1 000 kills with custom kingdom troops.
    /// </summary>
    public sealed class Feat_RoyalPatronage_RoyalHost : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_eq_royal_host";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            var kingdom = Player.Kingdom;
            if (kingdom == null)
                return;

            var kf = Filter(
                killers: a =>
                    a.IsPlayerTroop && a.Character.InCustomTree && a.Character.BelongsTo(kingdom),
                victims: v => v.IsEnemyTroop
            );

            int count = kf.Filter(kills).Count();

            Progress(count);
        }
    }
}
