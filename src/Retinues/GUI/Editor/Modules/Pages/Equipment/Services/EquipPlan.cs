using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Equipments.Wrappers;
using TaleWorlds.Core;

namespace Retinues.GUI.Editor.Modules.Pages.Equipment.Services
{
    public sealed class EquipPlan
    {
        public Dictionary<EquipmentIndex, WItem> Changes { get; } = [];

        public Dictionary<EquipSkipReason, int> SkippedByReason { get; } = [];
        public Dictionary<EquipSkipReason, List<string>> SkippedItemNamesByReason { get; } = [];

        public Dictionary<string, int> StockUseById { get; } = [];
        public Dictionary<string, int> PurchaseById { get; } = [];

        public int EquipOps { get; set; }
        public int UnequipOps { get; set; }
        public int SkippedOps { get; set; }

        public int TotalCost { get; set; }

        public int StockUseTotal => StockUseById.Values.Sum();
        public int PurchaseTotal => PurchaseById.Values.Sum();

        public int SkippedOf(EquipSkipReason r) =>
            SkippedByReason.TryGetValue(r, out var v) ? v : 0;

        public IReadOnlyList<string> SkippedNamesOf(EquipSkipReason r)
        {
            if (SkippedItemNamesByReason.TryGetValue(r, out var list))
                return list;
            return [];
        }

        public void AddSkip(EquipSkipReason reason, WItem item)
        {
            SkippedOps++;

            if (!SkippedByReason.ContainsKey(reason))
                SkippedByReason[reason] = 0;

            SkippedByReason[reason]++;

            if (item == null)
                return;

            var name = item.Name?.ToString();
            if (string.IsNullOrEmpty(name))
                return;

            if (!SkippedItemNamesByReason.TryGetValue(reason, out var list))
            {
                list = [];
                SkippedItemNamesByReason[reason] = list;
            }

            if (!list.Contains(name))
                list.Add(name);
        }
    }
}
