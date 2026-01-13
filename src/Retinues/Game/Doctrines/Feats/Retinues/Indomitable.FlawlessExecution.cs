using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Have your retinues defeat 20 enemy troops of equivalent tier without a single casualty.
    /// </summary>
    public sealed class Feat_Indomitable_FlawlessExecution : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_ret_flawless_execution";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (kills == null || kills.Count == 0)
                return;

            if (
                Filter(victims: v => v.Character.IsRetinue && v.IsPlayerTroop && v.IsDead)
                    .Filter(kills)
                    .Count() > 0
            )
                return; // Retinue casualties present.

            // Count equivalent-tier defeats performed by retinues.
            var kf = Filter(
                killers: a => a.IsPlayerTroop && a.Character.IsRetinue,
                victims: v => v.IsEnemyTroop
            );

            int count = 0;

            foreach (var kill in kf.Filter(kills))
            {
                // Skip if killer's tier is higher than victim's tier.
                if (kill.Killer.Tier > kill.Victim.Tier)
                    continue;

                count++;
            }

            Progress(count);
        }
    }
}
