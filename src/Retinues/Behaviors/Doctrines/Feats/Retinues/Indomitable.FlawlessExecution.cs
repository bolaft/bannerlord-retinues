using System.Collections.Generic;
using Retinues.Behaviors.Missions;
using Retinues.Domain.Events.Models;

namespace Retinues.Behaviors.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Have your retinues defeat 20 enemy troops of equivalent tier without a single casualty.
    /// </summary>
    public sealed class Feat_Indomitable_FlawlessExecution : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.IN_FlawlessExecution.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            foreach (var kill in kills)
            {
                if (
                    kill.Victim.Character.IsRetinue // Victim is a retinue troop
                    && kill.Victim.IsPlayerTroop // Victim is a retinue troop
                )
                {
                    Feat.Reset(); // Any retinue troop deaths.
                }
                else if (
                    kill.Killer.IsPlayerTroop // Killer is a player troop
                    && kill.Killer.Character.IsRetinue // Killer is a retinue troop
                    && kill.Victim.IsEnemyTroop // Victim is an enemy troop
                    && kill.Killer.Character.Tier <= kill.Victim.Character.Tier // Equivalent tier or lower
                )
                {
                    Feat.Add(); // Progress for each valid kill.
                }
            }
        }
    }
}
