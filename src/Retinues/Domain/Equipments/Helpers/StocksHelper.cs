using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;

namespace Retinues.Domain.Equipments.Helpers
{
    /// <summary>
    /// Utilities for resolving items by id and managing equipment stock changes and snapshots.
    /// </summary>
    public static class StocksHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Requirement Snapshots                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Builds a snapshot of required item counts from the given roster for later comparison.
        /// </summary>
        public static Dictionary<string, int> SnapshotRequiredCounts(MEquipmentRoster roster)
        {
            if (roster == null)
                return [];

            // Important: the action that mutates the roster may do so through a different
            // wrapper instance, which means this wrapper's cached ItemCountsById may not
            // have been invalidated. Force a recompute from the current underlying state.
            roster.InvalidateItemCountsCache();

            var src = roster.ItemCountsById;
            if (src == null || src.Count == 0)
                return [];

            // Copy to avoid mutations across actions.
            Dictionary<string, int> copy = [];
            foreach (var kv in src)
                copy[kv.Key] = kv.Value;

            return copy;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Stock Changes                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Stocks items described by a required-count map.
        /// </summary>
        public static void StockItems(Dictionary<string, int> requiredCountsById)
        {
            if (requiredCountsById == null || requiredCountsById.Count == 0)
                return;

            foreach (var kv in requiredCountsById)
            {
                string id = kv.Key;
                int count = kv.Value;

                if (string.IsNullOrEmpty(id) || count <= 0)
                    continue;

                var item = WItem.Get(id);
                item?.IncreaseStock(count);
            }
        }

        /// <summary>
        /// Consume stock based on a required-count map.
        /// </summary>
        public static bool ConsumeStock(Dictionary<string, int> requiredCountsById)
        {
            if (requiredCountsById == null || requiredCountsById.Count == 0)
                return true;

            bool ok = true;

            foreach (var kv in requiredCountsById)
            {
                string id = kv.Key;
                int needed = kv.Value;

                if (string.IsNullOrEmpty(id) || needed <= 0)
                    continue;

                var item = WItem.Get(id);
                if (item == null)
                    continue;

                int take = Math.Min(item.Stock, needed);
                if (take > 0)
                    item.DecreaseStock(take);

                if (take < needed)
                    ok = false;
            }

            return ok;
        }

        /// <summary>
        /// Applies roster removal deltas back into stock.
        /// </summary>
        public static void ApplyRosterRemovalsToStock(
            Dictionary<string, int> before,
            Dictionary<string, int> after
        )
        {
            if (before == null || before.Count == 0)
                return;

            after ??= [];

            foreach (var kv in before)
            {
                string id = kv.Key;
                int oldRequired = kv.Value;
                int newRequired = after.TryGetValue(id, out var n) ? n : 0;

                int removed = oldRequired - newRequired;
                if (removed <= 0)
                    continue;

                var item = WItem.Get(id);
                item?.IncreaseStock(removed);
            }
        }

        /// <summary>
        /// Runs an action while tracking roster requirement removals and converting them into stock.
        /// </summary>
        public static void TrackRosterStock(MEquipmentRoster roster, Action action)
        {
            if (action == null)
                return;

            var before = SnapshotRequiredCounts(roster);

            action();

            var after = SnapshotRequiredCounts(roster);
            ApplyRosterRemovalsToStock(before, after);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Convenience                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Stocks items for the given character's equipment roster.
        /// </summary>
        public static void StockCharacterRoster(WCharacter character)
        {
            if (character == null)
                return;

            StockItems(character.EquipmentRoster?.ItemCountsById);
        }

        /// <summary>
        /// Consumes stock for the given character's equipment roster.
        /// </summary>
        public static bool ConsumeCharacterRoster(WCharacter character)
        {
            if (character == null)
                return true;

            return ConsumeStock(character.EquipmentRoster?.ItemCountsById);
        }
    }
}
