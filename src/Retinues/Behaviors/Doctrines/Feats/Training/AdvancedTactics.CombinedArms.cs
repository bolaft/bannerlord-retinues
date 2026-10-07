using System.Collections.Generic;
using Retinues.Behaviors.Missions;
using Retinues.Domain;
using Retinues.Domain.Events.Models;
using TaleWorlds.Core;

namespace Retinues.Behaviors.Doctrines.Feats.Training
{
    /// <summary>
    /// Win a battle against over 100 enemies using a party evenly split among infantry, cavalry and ranged clan troops.
    /// </summary>
    public sealed class Feat_AdvancedTactics_CombinedArms : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.AT_CombinedArms.Id;

        static bool HasCombinedArms;

        protected override void OnBattleStart(MMapEvent battle)
        {
            var party = Player.Party;

            bool HasValidRatio(List<FormationClass> formations)
            {
                float ratio = 0f;

                foreach (var formation in formations)
                    ratio += party.ComputeMemberRatio(t => t.FormationClass == formation);

                return ratio >= 0.25f;
            }

            // Player.Party is live by OnBattleOver and casualties have already changed MemberRoster, which no longer reflects the original party composition.
            // formation ratios from OnBattleStart should be kept to prevent casualties altering the feat result
            HasCombinedArms =
                HasValidRatio([FormationClass.Infantry])
                && HasValidRatio([FormationClass.Cavalry, FormationClass.HorseArcher])
                && HasValidRatio([FormationClass.Ranged]);
        }

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (!end.IsWon)
                return; // Player lost the battle.

            if (start.EnemySide.HealthyTroops <= 100)
                return; // Not enough enemies.

            if (!HasCombinedArms)
                return; // Formation ratios were not valid at battle start.

            Feat.Add();
        }
    }
}
