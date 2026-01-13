using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Loot 25000 denars on the battlefield.
    /// </summary>
    public sealed class Feat_HonorGuard_BloodMoney : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_eq_blood_money";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
                return;

            Progress(battle.GoldReward);
        }
    }
}
