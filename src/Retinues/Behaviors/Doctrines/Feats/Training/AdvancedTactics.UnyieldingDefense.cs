using System.Collections.Generic;
using Retinues.Behaviors.Missions;
using Retinues.Domain.Events.Models;

namespace Retinues.Behaviors.Doctrines.Feats.Training
{
    /// <summary>
    /// Win 3 defensive battles in a row.
    /// </summary>
    public sealed class Feat_AdvancedTactics_UnyieldingDefense : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.AT_UnyieldingDefense.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (!end.IsWon)
            {
                // Player lost the battle, reset progress.
                Feat.Reset();
                return;
            }

            if (start.AttackerSide.IsPlayerSide)
                return; // Not a defensive battle.

            Feat.Add();
        }
    }
}
