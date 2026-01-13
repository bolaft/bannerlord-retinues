using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using TaleWorlds.Core;

namespace Retinues.Game.Doctrines.Feats.Training
{
    /// <summary>
    /// Win a battle against over 100 enemies using a party evenly split among infantry, cavalry and ranged clan troops.
    /// </summary>
    public sealed class Feat_AdvancedTactics_CombinedArms : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_tr_combined_arms";

        protected override void OnBattleOver(IReadOnlyList<MMission.Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
                return;

            if (battle.EnemyTroopCount <= 100)
                return;

            var party = Player.Party;

            bool HasValidRatio(List<FormationClass> formations)
            {
                float ratio = 0f;

                foreach (var formation in formations)
                    ratio += party.ComputeMemberRatio(t => t.FormationClass == formation);

                return ratio >= 0.25f;
            }

            if (!HasValidRatio([FormationClass.Infantry]))
                return;

            if (!HasValidRatio([FormationClass.Cavalry, FormationClass.HorseArcher]))
                return;

            if (!HasValidRatio([FormationClass.Ranged]))
                return;

            Progress(1);
        }
    }
}
