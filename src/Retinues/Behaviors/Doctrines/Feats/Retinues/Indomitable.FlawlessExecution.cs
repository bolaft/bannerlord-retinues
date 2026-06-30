using System.Collections.Generic;
using Retinues.Behaviors.Missions;
using Retinues.Domain.Events.Models;
using TaleWorlds.Core;

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
            // Tally the whole battle first: a single retinue death anywhere breaks the "flawless"
            // run, so credit the battle's kills only if no retinue actually died. (Iterating and
            // calling Reset/Add inline would let kills that land after a death still add progress.)
            int casualties = 0;
            int validKills = 0;

            foreach (var kill in kills)
            {
                if (
                    kill.State == AgentState.Killed // Actually died (knocked-out survivors don't count)
                    && kill.Victim.IsPlayerTroop // Victim is a player troop
                    && kill.Victim.Character.IsRetinue // Victim is a retinue troop
                )
                {
                    casualties++;
                }
                else if (
                    kill.Killer.IsPlayerTroop // Killer is a player troop
                    && kill.Killer.Character.IsRetinue // Killer is a retinue troop
                    && kill.Victim.IsEnemyTroop // Victim is an enemy troop
                    && kill.Killer.Character.Tier <= kill.Victim.Character.Tier // Equivalent tier or higher
                )
                {
                    validKills++;
                }
            }

            if (casualties > 0)
                Feat.Reset(); // A retinue fell — the flawless run resets.
            else if (validKills > 0)
                Feat.Add(validKills);
        }
    }
}
