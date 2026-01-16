using System.Collections.Generic;
using Retinues.Domain;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;
using TaleWorlds.Core;

namespace Retinues.Game.Doctrines.Feats.Training
{
    /// <summary>
    /// Win a battle against over 100 enemies using a party evenly split among infantry, cavalry and ranged clan troops.
    /// </summary>
    public sealed class Feat_AdvancedTactics_CombinedArms : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.AT_CombinedArms.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (end.IsLost)
                return; // Player lost the battle.

            if (start.EnemySide.HealthyTroops <= 100)
                return; // Not enough enemies.

            var party = Player.Party;

            /// <summary>
            /// Check if the party has at least 25% of its members in the specified formations.
            /// </summary>
            bool HasValidRatio(List<FormationClass> formations)
            {
                float ratio = 0f;

                foreach (var formation in formations)
                    ratio += party.ComputeMemberRatio(t => t.FormationClass == formation);

                return ratio >= 0.25f;
            }

            if (!HasValidRatio([FormationClass.Infantry]))
                return; // Not enough infantry.

            if (!HasValidRatio([FormationClass.Cavalry, FormationClass.HorseArcher]))
                return; // Not enough cavalry.

            if (!HasValidRatio([FormationClass.Ranged]))
                return; // Not enough ranged troops.

            Feat.Add();
        }
    }
}
