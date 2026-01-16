using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Loot 25000 denars on the battlefield.
    /// </summary>
    public sealed class Feat_HonorGuard_BloodMoney : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.HG_BloodMoney.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (end.IsLost)
                return; // Player lost the battle.

            Feat.Add(end.GoldReward); // Progress by the amount of gold looted.
        }
    }
}
