using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using CustomClanTroops.Utils;

namespace CustomClanTroops.Behaviors
{
    public sealed class EquipmentUnlockMissionBehavior : MissionBehavior
    {
        private readonly EquipmentUnlockBehavior _owner;

        // per-battle “how many defeated enemies wore item X”
        private readonly Dictionary<ItemObject, int> _battleCounts = [];

        public EquipmentUnlockMissionBehavior(EquipmentUnlockBehavior owner) => _owner = owner;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState state, KillingBlow blow)
        {
            Log.Info("OnAgentRemoved event triggered.");

            // Count only real enemies, humans, that went down (killed or knocked unconscious)
            if (affectedAgent == null || !affectedAgent.IsHuman) return;
            if (state != AgentState.Killed && state != AgentState.Unconscious) return;

            var playerTeam = Mission?.PlayerTeam;
            if (playerTeam == null) return;

            // Enemy team only
            if (affectedAgent.Team == null || !affectedAgent.Team.IsEnemyOf(playerTeam)) return;

            // Player-side kill only
            if (affectorAgent?.Team == null || !affectorAgent.Team.IsFriendOf(playerTeam))
                return;

            // Count each equipped item once per fallen agent
            var seen = new HashSet<ItemObject>();
            foreach (var item in EnumerateEquippedItems(affectedAgent))
            {
                if (item == null || !seen.Add(item)) continue;
                _battleCounts[item] = _battleCounts.TryGetValue(item, out var c) ? c + 1 : 1;
            }
        }

        protected override void OnEndMission()
        {
            Log.Info("OnEndMission event triggered.");

            // Hand the batch to the campaign behavior (it’ll persist + unlock as needed)
            _owner.AddBattleCounts(_battleCounts);
            _battleCounts.Clear();
        }

        private static IEnumerable<ItemObject> EnumerateEquippedItems(Agent agent)
        {
            var eq = agent?.Equipment;
            if (eq == null) yield break;

            // Only iterate the slots you care about, but always use SafeGet to avoid OOB.
            // (Order doesn’t matter; keep it consistent for readability.)
            yield return SafeGet(eq, EquipmentIndex.Weapon0);
            yield return SafeGet(eq, EquipmentIndex.Weapon1);
            yield return SafeGet(eq, EquipmentIndex.Weapon2);
            yield return SafeGet(eq, EquipmentIndex.Weapon3);

            yield return SafeGet(eq, EquipmentIndex.Head);
            yield return SafeGet(eq, EquipmentIndex.Body);
            yield return SafeGet(eq, EquipmentIndex.Leg);
            yield return SafeGet(eq, EquipmentIndex.Gloves);
            yield return SafeGet(eq, EquipmentIndex.Cape);

            // Mount slots can be absent in some agent equipment layouts; still safe via SafeGet
            yield return SafeGet(eq, EquipmentIndex.Horse);
            yield return SafeGet(eq, EquipmentIndex.HorseHarness);
        }

        private static ItemObject SafeGet(MissionEquipment eq, EquipmentIndex idx)
        {
            try
            {
                // If the slot exists, this returns an EquipmentElement whose .Item can be null.
                return eq[idx].Item;
            }
            catch
            {
                // Index out of range (or any other access issue) → treat as "no item in that slot".
                return null;
            }
        }
    }
}