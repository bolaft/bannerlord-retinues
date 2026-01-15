using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.FeatCatalog.Training
{
    /// <summary>
    /// Win 3 defensive battles in a row.
    /// </summary>
    public sealed class Feat_AdvancedTactics_UnyieldingDefense : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.DoctrineCatalog.AT_UnyieldingDefense.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (end.IsLost)
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
