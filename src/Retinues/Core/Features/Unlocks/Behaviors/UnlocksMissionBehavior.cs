using System;
using System.Collections.Generic;
using Retinues.Core.Features.Doctrines;
using Retinues.Core.Features.Doctrines.Catalog;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core.Features.Unlocks.Behaviors
{
    public sealed class UnlocksMissionBehavior(UnlocksBehavior owner) : MissionBehavior
    {
        private readonly UnlocksBehavior _owner = owner;

        private readonly Dictionary<ItemObject, int> _battleCounts = [];

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        private bool IsPlayerTroop(Agent agent)
        {
            return new WAgent(agent).IsPlayerTroop;
        }

        private bool IsAlliedTroop(Agent agent)
        {
            return agent?.Team == Mission?.PlayerTeam && !IsPlayerTroop(agent);
        }

        private bool IsEnemyTroop(Agent agent)
        {
            return agent?.Team != Mission?.PlayerTeam;
        }

        public override void OnAgentRemoved(
            Agent victim,
            Agent killer,
            AgentState state,
            KillingBlow blow
        )
        {
            try
            {
                // Count only real enemies, humans, that went down (killed or knocked unconscious)
                if (victim == null || !victim.IsHuman)
                    return;
                if (state != AgentState.Killed && state != AgentState.Unconscious)
                    return;
                if (Mission?.PlayerTeam == null)
                    return;

                if (IsPlayerTroop(victim))
                    return; // No unlock from player troop casualty
                else if (IsAlliedTroop(victim))
                    if (!DoctrineAPI.IsDoctrineUnlocked<PragmaticScavengers>())
                        return; // No unlock from ally casualty unless doctrine is enabled

                if (IsEnemyTroop(killer))
                    return; // Enemy killers don't unlock anything
                else if (IsAlliedTroop(killer))
                    if (!DoctrineAPI.IsDoctrineUnlocked<BattlefieldTithes>())
                        return; // No unlock from ally killers unless doctrine is enabled

                int updateValue = 1;

                if (DoctrineAPI.IsDoctrineUnlocked<LionsShare>())
                    if (killer.Character?.StringId == Player.Character?.StringId)
                        updateValue = 2; // Double count if player personally landed the killing blow

                // Count each equipped item once per fallen agent
                var seen = new HashSet<WItem>();

                foreach (var item in EnumerateEquippedItems(victim))
                {
                    if (item == null || !seen.Add(item))
                        continue;

                    _battleCounts[item.Base] = _battleCounts.TryGetValue(item.Base, out var c)
                        ? c + updateValue
                        : updateValue;
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        protected override void OnEndMission()
        {
            Log.Info("OnEndMission: Flushing battle counts to campaign behavior.");

            try
            {
                Log.Debug("OnEndMission event triggered.");

                if (Mission.Current?.MissionResult?.PlayerVictory == true)
                {
                    // Hand the batch to the campaign behavior only on victory
                    _owner.AddBattleCounts(_battleCounts);
                }

                _battleCounts.Clear();
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        private static IEnumerable<WItem> EnumerateEquippedItems(Agent agent)
        {
            var eq = new WEquipment(agent?.SpawnEquipment);

            if (eq.Base == null)
                yield break;

            foreach (var item in eq.Items)
            {
                if (item != null)
                    yield return item;
            }
        }
    }
}
