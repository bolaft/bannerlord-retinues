using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Loot 25000 denars on the battlefield.
    /// </summary>
    public sealed class Feat_HonorGuard_BloodMoney : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_eq_blood_money";

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (end.IsLost)
                return; // Player lost the battle.

            Progress(end.GoldReward); // Progress by the amount of gold looted.
        }
    }
}
