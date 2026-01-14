using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements.Workshops;

namespace Retinues.Game.Unlocks
{
    /// <summary>
    /// Applies equipment unlock progress over time based on owned workshops.
    /// Each workshop sticks to a chosen item until it is unlocked (persisted across saves).
    /// </summary>
    public sealed class UnlocksByWorkshopsBehavior : BaseCampaignBehavior
    {
        public override bool IsActive => Settings.UnlockItemsThroughWorkshops;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Persistence                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const string SyncKeyLastDay = "ret_workshop_unlock_last_day";
        private const string SyncKeyWorkshopKeys = "ret_workshop_unlock_targets_keys";
        private const string SyncKeyWorkshopItems = "ret_workshop_unlock_targets_items";

        private int _lastProcessedDay = -1;

        // workshopKey -> itemStringId
        private readonly Dictionary<string, string> _targetByWorkshopKey = new(
            StringComparer.Ordinal
        );

        /// <summary>
        /// Synchronizes persistent data.
        /// </summary>
        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData(SyncKeyLastDay, ref _lastProcessedDay);

            List<string> keys = null;
            List<string> items = null;

            if (dataStore.IsSaving)
            {
                keys = new List<string>(_targetByWorkshopKey.Count);
                items = new List<string>(_targetByWorkshopKey.Count);

                foreach (var kvp in _targetByWorkshopKey)
                {
                    if (string.IsNullOrEmpty(kvp.Key) || string.IsNullOrEmpty(kvp.Value))
                        continue;

                    keys.Add(kvp.Key);
                    items.Add(kvp.Value);
                }
            }

            dataStore.SyncData(SyncKeyWorkshopKeys, ref keys);
            dataStore.SyncData(SyncKeyWorkshopItems, ref items);

            if (dataStore.IsLoading)
            {
                _targetByWorkshopKey.Clear();

                if (keys != null && items != null)
                {
                    var n = Math.Min(keys.Count, items.Count);

                    for (var i = 0; i < n; i++)
                    {
                        var k = keys[i];
                        var v = items[i];

                        if (string.IsNullOrEmpty(k) || string.IsNullOrEmpty(v))
                            continue;

                        _targetByWorkshopKey[k] = v;
                    }
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Called each in-game day to apply workshop unlock progress.
        /// </summary>
        protected override void OnDailyTick()
        {
            var hero = Player.Hero?.Base;
            if (hero == null)
                return;

            var workshops = hero.OwnedWorkshops;
            if (workshops == null || workshops.Count == 0)
                return;

            var requiredDays = Math.Max(1, (int)Settings.RequiredDaysToUnlock);
            var perDay = Math.Max(
                1,
                (int)Math.Ceiling(WItem.UnlockThreshold / (double)requiredDays)
            );

            var currentDay = (int)CampaignTime.Now.ToDays;

            if (_lastProcessedDay < 0)
            {
                _lastProcessedDay = currentDay;
                return;
            }

            if (currentDay <= _lastProcessedDay)
                return;

            // Cleanup mappings for workshops we no longer own.
            CleanupTargets(workshops);

            // Safety cap to avoid huge catch-up loops.
            var maxCatchupDays = 60;
            var daysToProcess = Math.Min(currentDay - _lastProcessedDay, maxCatchupDays);
            var unlocked = new List<WItem>();
            var started = new List<UnlockNotifier.WorkshopStartInfo>();

            for (var d = 0; d < daysToProcess; d++)
            {
                var dayIndex = _lastProcessedDay + 1 + d;
                ApplyOneDay(workshops, perDay, dayIndex, unlocked, started);
            }

            UnlockNotifier.ItemsUnlocked(UnlockNotifier.UnlockMethod.Workshops, unlocked);
            UnlockNotifier.WorkshopsStarted(started);

            _lastProcessedDay += daysToProcess;

            if (currentDay - _lastProcessedDay > 0)
            {
                Log.Warn(
                    $"[Unlocks] Workshop unlock catch-up capped. SkippedDays={currentDay - _lastProcessedDay}."
                );
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Cleans up target mappings for workshops no longer owned.
        /// </summary>
        private void CleanupTargets(IReadOnlyList<Workshop> owned)
        {
            if (_targetByWorkshopKey.Count == 0)
                return;

            var keep = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < owned.Count; i++)
            {
                var w = owned[i];
                var key = GetWorkshopKey(w);
                if (!string.IsNullOrEmpty(key))
                    keep.Add(key);
            }

            if (keep.Count == 0)
            {
                _targetByWorkshopKey.Clear();
                return;
            }

            var toRemove = new List<string>();

            foreach (var k in _targetByWorkshopKey.Keys)
            {
                if (!keep.Contains(k))
                    toRemove.Add(k);
            }

            for (var i = 0; i < toRemove.Count; i++)
                _targetByWorkshopKey.Remove(toRemove[i]);
        }

        /// <summary>
        /// Applies one day worth of unlock progress to all owned workshops.
        /// </summary>
        private void ApplyOneDay(
            IReadOnlyList<Workshop> workshops,
            int perDay,
            int dayIndex,
            List<WItem> unlocked,
            List<UnlockNotifier.WorkshopStartInfo> started
        )
        {
            var itemsTouched = 0;
            long totalAdded = 0;

            for (var i = 0; i < workshops.Count; i++)
            {
                var w = workshops[i];
                if (w?.WorkshopType == null || w.Settlement == null)
                    continue;

                var workshopKey = GetWorkshopKey(w);
                if (string.IsNullOrEmpty(workshopKey))
                    continue;

                // Some workshops should not unlock equipment at all (wine press, brewery, etc).
                // If a save already contains a target mapping for them, purge it.
                if (!WorkshopUnlockSelector.CanUnlock(w))
                {
                    _targetByWorkshopKey.Remove(workshopKey);
                    continue;
                }

                // Resolve current target, or pick a new one if missing/unlocked/invalid.
                var target = GetOrAssignTarget(w, workshopKey, dayIndex, started);
                if (target == null || !target.IsValidEquipment || target.IsUnlocked)
                    continue;

                var wasUnlocked = target.IsUnlocked;
                var isUnlocked = target.IncreaseUnlockProgress(perDay);

                if (!wasUnlocked && isUnlocked)
                    unlocked.Add(target);

                itemsTouched++;
                totalAdded += perDay;

                // If it just unlocked, next day will pick a new target.
                if (isUnlocked)
                    _targetByWorkshopKey.Remove(workshopKey);
            }

            if (Settings.DebugMode && itemsTouched > 0)
            {
                Log.Debug(
                    $"[Unlocks] Workshop progress: day={dayIndex}, workshops={workshops.Count}, items={itemsTouched}, newlyUnlocked={unlocked.Count}, totalAdded={totalAdded}."
                );
            }
        }

        /// <summary>
        /// Gets or assigns the unlock target item for a workshop.
        /// </summary>
        private WItem GetOrAssignTarget(
            Workshop w,
            string workshopKey,
            int dayIndex,
            List<UnlockNotifier.WorkshopStartInfo> started
        )
        {
            if (
                _targetByWorkshopKey.TryGetValue(workshopKey, out var itemId)
                && !string.IsNullOrEmpty(itemId)
            )
            {
                var current = WItem.Get(itemId);
                if (current != null && current.IsValidEquipment && !current.IsUnlocked)
                    return current;

                // Clear stale/unlocked selection.
                _targetByWorkshopKey.Remove(workshopKey);
            }

            // Pick a new target only when needed.
            var next = WorkshopUnlockSelector.PickTargetItem(w, seed: dayIndex);
            if (next == null || !next.IsValidEquipment || next.IsUnlocked)
                return null;

            _targetByWorkshopKey[workshopKey] = next.StringId;

            // Record start (for summary notification).
            var typeName =
                w.WorkshopType?.Name?.ToString() ?? w.WorkshopType?.StringId ?? "Workshop";
            var townName = w.Settlement?.Name?.ToString() ?? w.Settlement?.StringId ?? "Unknown";

            started?.Add(
                new UnlockNotifier.WorkshopStartInfo
                {
                    WorkshopTypeName = typeName,
                    SettlementName = townName,
                    Item = next,
                }
            );

            return next;
        }

        /// <summary>
        /// Generates a stable key for a workshop to track its assigned unlock target.
        /// </summary>
        private static string GetWorkshopKey(Workshop w)
        {
            // We need a key that is stable across save/load.
            // Best cheap candidate: TownId + index within Town.Workshops list.
            var town = w?.Settlement?.Town;
            if (town == null)
                return null;

            var workshops = town.Workshops;
            if (workshops == null)
                return null;

            var idx = -1;
            for (var i = 0; i < workshops.Length; i++)
            {
                if (ReferenceEquals(workshops[i], w))
                {
                    idx = i;
                    break;
                }
            }

            if (idx < 0)
                return null;

            var townId = town.Settlement?.StringId ?? w.Settlement?.StringId ?? "unknown_town";
            var typeId = w.WorkshopType?.StringId ?? "unknown_type";

            return $"{townId}|{idx}|{typeId}";
        }
    }
}
