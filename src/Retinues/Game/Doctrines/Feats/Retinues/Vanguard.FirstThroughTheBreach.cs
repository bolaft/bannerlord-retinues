using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Have a retinue get the first melee kill in a siege assault.
    /// </summary>
    public sealed class Feat_Vanguard_FirstThroughTheBreach : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_ret_first_through_the_breach";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (!battle.IsSiege || !battle.IsPlayerAttacker)
                return;

            // First melee kill overall must be by a player retinue.
            foreach (var kill in kills)
            {
                if (kill.IsMissile)
                    continue;

                if (!kill.KillerIsPlayerTroop)
                    return;

                var killer = kill.Killer;
                if (!killer.IsRetinue)
                    return;

                Progress(1);
                return;
            }
        }
    }
}
