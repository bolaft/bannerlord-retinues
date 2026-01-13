using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Troops
{
    /// <summary>
    /// Defend a village against an enemy raid.
    /// </summary>
    public sealed class Feat_ArmedPeasantry_ShieldOfThePeople : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_trp_shield_of_the_people";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
                return;

            if (!battle.IsRaid)
                return;

            if (!battle.IsPlayerDefender)
                return;

            Progress(1);
        }
    }
}
