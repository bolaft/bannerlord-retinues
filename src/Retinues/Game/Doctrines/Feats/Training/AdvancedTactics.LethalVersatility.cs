using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Training
{
    /// <summary>
    /// In a single battle, get a kill using five different weapon classes.
    /// </summary>
    public sealed class Feat_AdvancedTactics_LethalVersatility : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_tr_lethal_versatility";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            var classes = new HashSet<int>();

            foreach (var kill in kills)
            {
                if (!kill.VictimIsEnemyTroop)
                    continue;

                if (!kill.KillerIsPlayer)
                    continue;

                classes.Add(kill.WeaponClass);
            }

            if (classes.Count < 5)
                return;

            Progress(1);
        }
    }
}
