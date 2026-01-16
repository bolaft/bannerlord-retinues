using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;

namespace Retinues.Editor.Services.Equipments
{
    public static class EquipEconomy
    {
        public static int ComputeEquipCost(WItem item)
        {
            if (item == null)
                return 0;

            double multiplier = Settings.EquipmentCostMultiplier.Value;
            double raw = item.Value * multiplier;

            int cost = (int)Math.Round(raw, MidpointRounding.AwayFromZero);
            return Math.Max(cost, 0);
        }

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

            Dictionary<string, int> CountItems(WItem[] items)
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

            foreach (var id in ids)
            {
                int beforeThis = beforeCounts.TryGetValue(id, out var b) ? b : 0;
                int afterThis = afterCounts.TryGetValue(id, out var a) ? a : 0;

                int otherMax = roster.GetMaxCountExcludingEquipment(target.Base, id);

                int requiredBefore = Math.Max(otherMax, beforeThis);
                int requiredAfter = Math.Max(otherMax, afterThis);

                int delta = requiredAfter - requiredBefore;
                if (delta <= 0)
                    continue;

                var item = after.FirstOrDefault(x => x != null && x.StringId == id);
                if (item == null)
                    continue;

                int stockUse = Math.Min(item.Stock, delta);
                int purchase = delta - stockUse;

                if (stockUse > 0)
                    plan.StockUseById[id] = stockUse;

                if (purchase > 0)
                    plan.PurchaseById[id] = purchase;

                if (purchase > 0)
                    plan.TotalCost += ComputeEquipCost(item) * purchase;
            }
        }
    }
}
