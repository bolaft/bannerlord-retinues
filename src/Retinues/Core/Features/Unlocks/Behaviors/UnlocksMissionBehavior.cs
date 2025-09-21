using System.Collections.Generic;
using Retinues.Core.Features.Doctrines;
using Retinues.Core.Features.Doctrines.Catalog;
using Retinues.Core.Game;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem.AgentOrigins;
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
            var party = (agent?.Origin as PartyAgentOrigin)?.Party;
            return party?.MobileParty?.StringId == Player.Party?.StringId;
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
                else
                    Log.Info("Counting allied casualty due to Pragmatic Scavengers.");

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
            var seen = new HashSet<ItemObject>();
            foreach (var item in EnumerateEquippedItems(victim))
            {
                if (item == null || !seen.Add(item))
                    continue;

                _battleCounts[item] = _battleCounts.TryGetValue(item, out var c)
                    ? c + updateValue
                    : updateValue;
            }
        }

        protected override void OnEndMission()
        {
            Log.Debug("OnEndMission event triggered.");

            if (Mission.Current?.MissionResult?.PlayerVictory == true)
            {
                // Hand the batch to the campaign behavior only on victory
                _owner.AddBattleCounts(_battleCounts);
            }

            _battleCounts.Clear();
        }

        private static IEnumerable<ItemObject> EnumerateEquippedItems(Agent agent)
        {
            var eq = agent?.Equipment;
            if (eq == null)
                yield break;

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
