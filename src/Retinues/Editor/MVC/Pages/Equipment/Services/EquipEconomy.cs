using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;

namespace Retinues.Editor.MVC.Pages.Equipment.Services
{
    /// <summary>
    /// Service for equipment economy calculations.
    /// </summary>
    public static class EquipEconomy
    {
        /// <summary>
        /// Computes the cost to equip the given item.
        /// </summary>
        public static int ComputeEquipCost(WItem item)
        {
            if (item == null)
                return 0;

            double multiplier = Settings.EquipmentCostMultiplier.Value;
            double raw = item.Value * multiplier;

            int cost = (int)Math.Round(raw, MidpointRounding.AwayFromZero);
            return Math.Max(cost, 0);
        }

        /// <summary>
        /// Computes the economy impact of changing equipment from 'before' to 'after' on the target.
        /// </summary>
        public static void ComputeBatchEconomy(
            EquipContext ctx,
            MEquipment target,
            WItem[] before,
            WItem[] after,
            EquipPlan plan
        )
        {
            if (plan == null || target == null)
                return;

            if (!ctx.EconomyEnabled)
                return;

            var roster = ctx.Character?.EquipmentRoster;
            if (roster == null)
                return;

            static Dictionary<string, int> CountItems(WItem[] items)
            {
                Dictionary<string, int> map = [];
                for (int i = 0; i < items.Length; i++)
                {
                    var id = items[i]?.StringId;
                    if (string.IsNullOrEmpty(id))
                        continue;

                    if (!map.ContainsKey(id))
                        map[id] = 0;

                    map[id]++;
                }
                return map;
            }

            var beforeCounts = CountItems(before);
            var afterCounts = CountItems(after);

            var ids = new HashSet<string>(beforeCounts.Keys);
            ids.UnionWith(afterCounts.Keys);

            foreach (var item in ids.Select(WItem.Get))
            {
                int beforeThis = beforeCounts.TryGetValue(item.StringId, out var b) ? b : 0;
                int afterThis = afterCounts.TryGetValue(item.StringId, out var a) ? a : 0;

                int otherMax = roster.GetMaxCountExcludingEquipment(target, item);

                int requiredBefore = Math.Max(otherMax, beforeThis);
                int requiredAfter = Math.Max(otherMax, afterThis);

                int delta = requiredAfter - requiredBefore;
                if (delta <= 0)
                    continue;

                if (!after.Contains(item))
                    continue;

                int stockUse = Math.Min(item.Stock, delta);
                int purchase = delta - stockUse;

                if (stockUse > 0)
                    plan.StockUseById[item.StringId] = stockUse;

                if (purchase > 0)
                    plan.PurchaseById[item.StringId] = purchase;

                if (purchase > 0)
                    plan.TotalCost += ComputeEquipCost(item) * purchase;
            }
        }
    }
}
