using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Framework.Runtime;

namespace Retinues.Domain.Equipments.Helpers
{
    public static class StocksHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Lookup                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static Dictionary<string, WItem> _itemById;

        private static WItem GetItemById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            _itemById ??= BuildItemByIdCache();

            if (_itemById.TryGetValue(id, out var item))
                return item;

            // Fallback: crafted / uncommon items may not be in Equipments.
            foreach (var it in WItem.All)
            {
                if (it != null && it.StringId == id)
                {
                    _itemById[id] = it;
                    return it;
                }
            }

            return null;
        }

        private static Dictionary<string, WItem> BuildItemByIdCache()
        {
            Dictionary<string, WItem> map = [];

            foreach (var item in WItem.Equipments)
            {
                var id = item?.StringId;
                if (string.IsNullOrEmpty(id))
                    continue;

                if (!map.ContainsKey(id))
                    map[id] = item;
            }

            return map;
        }

        [StaticClearAction]
        public static void ClearItemCache() => _itemById = null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Requirement Snapshots               //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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
        /// Stock items described by a required-count map.
        /// Use for destructive operations where requirement becomes 0 (eg deleting troop).
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

                var item = GetItemById(id);
                item?.IncreaseStock(count);
            }
        }

        /// <summary>
        /// Consume stock based on a required-count map.
        /// This consumes the conceptual roster requirement (max-per-equipment),
        /// so shared items across sets are only consumed once.
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

                var item = GetItemById(id);
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

                var item = GetItemById(id);
                item?.IncreaseStock(removed);
            }
        }

        /// <summary>
        /// Runs an action while tracking roster requirement removals and converting them to stock.
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
        //                      Convenience                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void StockCharacterRoster(WCharacter character)
        {
            if (character == null)
                return;

            StockItems(character.EquipmentRoster?.ItemCountsById);
        }

        public static bool ConsumeCharacterRoster(WCharacter character)
        {
            if (character == null)
                return true;

            return ConsumeStock(character.EquipmentRoster?.ItemCountsById);
        }
    }
}
