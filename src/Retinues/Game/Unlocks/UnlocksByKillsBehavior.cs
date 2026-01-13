using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Events.Models;
using Retinues.Framework.Behaviors;
using Retinues.Utilities;
using TaleWorlds.MountAndBlade;

namespace Retinues.Game.Unlocks
{
    /// <summary>
    /// Applies equipment unlock progress from mission outcomes (kills).
    /// </summary>
    public sealed class UnlocksByKillsBehavior : BaseCampaignBehavior
    {
        private static Mission _lastAppliedMission;

        /// <summary>
        /// Called when a mission ends.
        /// </summary>
        protected override void OnMissionEnded(MMission mission)
        {
            if (!Settings.EquipmentNeedsUnlocking || !Settings.UnlockItemsThroughKills)
                return;

            try
            {
                if (MMapEvent.Current.IsLost)
                    return; // No progress on lost battles

                // Keep the legacy behavior as a fallback path (for cases where
                // the post-battle scoreboard patch did not run).
                _ = ApplyProgressFromMissionKills(mission, notify: true);
            }
            catch (Exception e)
            {
                Log.Exception(e, "Item unlock progress failed on mission end.");
            }
        }

        /// <summary>
        /// Applies unlock progress based on kills recorded in the given mission.
        /// Returns the list of newly-unlocked items (if any).
        /// </summary>
        internal static IReadOnlyList<WItem> ApplyProgressFromMissionKills(
            MMission mission,
            bool notify
        )
        {
            var mm = mission ?? MMission.Current;
            if (mm == null)
                return [];

            var mbMission = mission?.Base;
            if (mbMission != null && ReferenceEquals(_lastAppliedMission, mbMission))
                return [];

            var kills = mm.Kills;
            if (kills == null || kills.Count == 0)
                return [];

            var required = (int)Settings.RequiredKillsToUnlock;
            if (required <= 0)
                return [];

            var perKill = Math.Max(1, WItem.UnlockThreshold / required);

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var i = 0; i < kills.Count; i++)
            {
                var k = kills[i];

                if (k.KillerIsPlayerSide == false)
                    if (!Settings.CountAllyCasualties || !k.VictimIsAllyTroop)
                        continue; // Only consider player side kills

                if (!Settings.CountAllyKills && k.KillerIsAllyTroop)
                    continue; // Skip ally kills if configured

                AccumulateFromEquipment(k.KillerEquipment, counts);
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

            if (unlocked.Count > 0 && notify)
                UnlockNotifier.ItemsUnlocked(UnlockNotifier.UnlockMethod.Kills, unlocked);

            if (mbMission != null)
                _lastAppliedMission = mbMission;

            if (Settings.DebugMode && itemsTouched > 0)
            {
                Log.Debug(
                    $"[Unlocks] Mission kill progress applied: items={itemsTouched}, newlyUnlocked={unlocked.Count}, totalAdded={totalAdded}."
                );
            }

            return unlocked;
        }

        private static void AccumulateFromEquipment(MEquipment eq, Dictionary<string, int> counts)
        {
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
