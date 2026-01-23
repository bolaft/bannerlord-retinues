using System;
using System.Collections.Generic;
using Retinues.Behaviors.Doctrines.Catalogs;
using Retinues.Behaviors.Missions;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Events.Models;
using Retinues.Framework.Behaviors;
using Retinues.Settings;
using Retinues.Utilities;
using TaleWorlds.MountAndBlade;

namespace Retinues.Behaviors.Unlocks
{
    /// <summary>
    /// Applies equipment unlock progress from mission outcomes (kills).
    /// </summary>
    public sealed class UnlocksByKillsBehavior : BaseCampaignBehavior
    {
        static Mission _lastAppliedMission;

        public override bool IsActive => Configuration.UnlockItemsThroughKills;

        /// <summary>
        /// Called when a mission ends.
        /// </summary>
        protected override void OnMissionEnded(MMission mission)
        {
            if (CombatBehavior.MapEvent?.IsLost == true)
                return; // No progress on lost battles.

            // Kept in case the post-battle scoreboard patch did not run.
            var unlocked = ApplyProgressFromMissionKills(mission.Base);

            Log.Debug($"Mission ended: {unlocked.Count} items unlocked from kills.");
        }

        /// <summary>
        /// Applies unlock progress based on kills recorded in the given mission.
        /// Returns the list of newly-unlocked items (if any).
        /// </summary>
        internal static IReadOnlyList<WItem> ApplyProgressFromMissionKills(Mission mission)
        {
            if (ReferenceEquals(mission, _lastAppliedMission))
            {
                Log.Debug($"[Unlocks] Already applied for this mission.");
                return []; // Already applied for this mission.
            }

            // Remember that we applied for this mission.
            _lastAppliedMission = mission;

            var kills = CombatBehavior.GetKills();
            if (kills == null || kills.Count == 0)
                return [];

            var required = (int)Configuration.RequiredKillsToUnlock;
            if (required <= 0)
                return [];

            var perKill = Math.Max(1, WItem.UnlockThreshold / required);

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var k in kills)
            {
                var killer = k.Killer;
                var victim = k.Victim;

                float multiplier = GetLootMultiplier(killer, victim);

                if (multiplier <= 0f)
                    continue; // No progress for this kill.

                AccumulateFromEquipment(k.VictimEquipment, counts);
            }

            if (counts.Count == 0)
                return [];

            var itemsTouched = 0;
            var unlocked = new List<WItem>();
            long totalAdded = 0;

            foreach (var kvp in counts)
            {
                var wItem = WItem.Get(kvp.Key);
                if (wItem == null || !wItem.IsValidEquipment)
                    continue;

                var add = perKill * kvp.Value;
                if (add <= 0)
                    continue;

                var wasUnlocked = wItem.IsUnlocked;
                var isUnlocked = wItem.IncreaseUnlockProgress(add);

                itemsTouched++;
                totalAdded += add;

                if (!wasUnlocked && isUnlocked)
                    unlocked.Add(wItem);
            }

            if (unlocked.Count > 0)
                ItemUnlockNotifier.ItemsUnlocked(ItemUnlockNotifier.UnlockMethod.Kills, unlocked);

            return unlocked;
        }

        private static float GetLootMultiplier(MAgent.Snapshot killer, MAgent.Snapshot victim)
        {
            // Case 1: Victim is player troop.
            if (victim.IsPlayerTroop)
            {
                return 0f;
            }

            // Case 2: Victim is ally troop.
            if (victim.IsAllyTroop)
            {
                // Count ally casualties if Pragmatic Scavengers is acquired.
                if (DoctrineCatalog.PragmaticScavengers.IsAcquired)
                    return 1f;
                else
                    return 0f;
            }

            // Case 3: Victim is enemy troop.
            if (victim.IsEnemyTroop)
            {
                // Case 3.a: Killer is player.
                if (killer.IsPlayer)
                    return 1f;
                // Case 3.b: Killer is player troop.
                else if (killer.IsPlayerTroop)
                {
                    // Double loot chance if Lions' Share is acquired.
                    if (DoctrineCatalog.LionsShare.IsAcquired)
                        return 2f;
                    else
                        return 1f;
                }
                // Case 3.c: Killer is ally troop.
                else if (killer.IsAllyTroop)
                {
                    // Count ally kills if Battlefield Tithes is acquired.
                    if (DoctrineCatalog.BattlefieldTithes.IsAcquired)
                        return 1f;
                    else
                        return 0f;
                }
            }

            return 0f;
        }

        /// <summary>
        /// Accumulate kill counts from the given equipment.
        /// </summary>
        private static void AccumulateFromEquipment(MEquipment eq, Dictionary<string, int> counts)
        {
            if (eq == null)
                return;

            foreach (var item in eq.Items)
            {
                if (item == null)
                    continue;

                var id = item.StringId;
                if (string.IsNullOrEmpty(id))
                    continue;

                if (counts.TryGetValue(id, out var c))
                    counts[id] = c + 1;
                else
                    counts[id] = 1;
            }
        }
    }
}
