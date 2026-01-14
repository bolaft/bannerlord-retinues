using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Equipments.Wrappers;
using TaleWorlds.Core;

namespace Retinues.Editor.Services.Equipments
{
    public sealed class EquipPlan
    {
        public Dictionary<EquipmentIndex, WItem> Changes { get; } = [];
        public Dictionary<EquipSkipReason, int> SkippedByReason { get; } = [];

        public Dictionary<string, int> StockUseById { get; } = [];

        public int EquipOps { get; set; }
        public int UnequipOps { get; set; }
        public int SkippedOps { get; set; }

        public int TotalCost { get; set; }

        public int StockUseTotal => StockUseById.Values.Sum();

        public int SkippedOf(EquipSkipReason r) =>
            SkippedByReason.TryGetValue(r, out var v) ? v : 0;

        public void AddSkip(EquipSkipReason reason)
        {
            SkippedOps++;
            if (!SkippedByReason.ContainsKey(reason))
                SkippedByReason[reason] = 0;
            SkippedByReason[reason]++;
        }
    }
}
