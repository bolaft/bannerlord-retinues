using System;
using System.Collections.Generic;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Utilities;

namespace Retinues.Domain.Equipments.Models
{
    public partial class MEquipmentRoster
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Shared Item Counts                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Cache for item counts by id.
        /// </summary>
        private readonly Cache<MEquipmentRoster, Dictionary<string, int>> _itemCountsCache = new(
            r => r.ComputeItemCounts()
        );

        /// <summary>
        /// Required roster stock per item id.
        /// Rule: for each item, keep the max number of copies used by any single equipment.
        /// </summary>
        public Dictionary<string, int> ItemCountsById => _itemCountsCache.Get(this);

        /// <summary>
        /// Invalidates the item counts cache.
        /// </summary>
        public void InvalidateItemCountsCache() => _itemCountsCache.Clear();

        /// <summary>
        /// Computes the required roster stock per item id.
        /// </summary>
        private Dictionary<string, int> ComputeItemCounts()
        {
            Dictionary<string, int> result = [];

            // Use cached wrappers, not fresh wrappers
            var list = Equipments;

            for (int i = 0; i < list.Count; i++)
            {
                var me = list[i];
                if (me == null)
                    continue;

                Dictionary<string, int> per = [];

                foreach (var item in me.Items)
                {
                    if (item == null)
                        continue;

                    string id = item.StringId;
                    if (string.IsNullOrEmpty(id))
                        continue;

                    if (!per.ContainsKey(id))
                        per[id] = 0;

                    per[id]++;
                }

                foreach (var kv in per)
                {
                    if (!result.TryGetValue(kv.Key, out int current))
                        result[kv.Key] = kv.Value;
                    else
                        result[kv.Key] = Math.Max(current, kv.Value);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the maximum count of the specified item across all equipments, excluding the given equipment.
        /// </summary>
        internal int GetMaxCountExcludingEquipment(MEquipment exclude, WItem item)
        {
            if (exclude == null || item == null)
                return 0;

            int max = 0;

            var list = Equipments;
            for (int i = 0; i < list.Count; i++)
            {
                var me = list[i];
                if (me == null)
                    continue;

                if (me == exclude)
                    continue;

                int count = 0;
                foreach (var wi in me.Items)
                {
                    if (wi != null && wi == item)
                        count++;
                }

                if (count > max)
                    max = count;
            }

            return max;
        }
    }
}
