using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.FeatCatalog.Troops
{
    /// <summary>
    /// Defend a city against a besieging army.
    /// </summary>
    public sealed class Feat_StalwartMilitia_DefenderOfTheCity : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.DoctrineCatalog.SM_DefenderOfTheCity.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (end.IsLost)
                return; // Player lost the battle.

            if (!start.IsSiegeBattle)
                return; // Not a siege battle.

            if (!start.DefenderSide.IsPlayerSide)
                return; // Player is not defending.

            if (!start.IsEnemyInArmy)
                return; // Enemy is not an army.

            Feat.Add();
        }
    }
}
