using System;
using System.Collections.Generic;
using Retinues.Domain.Equipments.Wrappers;
using TaleWorlds.Core;

namespace Retinues.Editor.Services.Equipments
{
    public static class EquipPlanner
    {
        /// <summary>
        /// Builds a plan by applying proposed changes in order, using rules to skip invalid ones,
        /// and tracking a "planned" equipment snapshot used for limit checks.
        /// </summary>
        public static EquipPlan BuildPlan(
            EquipContext ctx,
            Func<EquipmentIndex, WItem> getCurrent,
            IEnumerable<(EquipmentIndex Slot, WItem Item)> changes
        )
        {
            if (ctx?.Equipment == null)
                return null;

            int slotCount = (int)EquipmentIndex.NumEquipmentSetSlots;

            var current = new WItem[slotCount];
            var planned = new WItem[slotCount];

            for (int i = 0; i < slotCount; i++)
            {
                var idx = (EquipmentIndex)i;
                var it = getCurrent(idx);
                current[i] = it;
                planned[i] = it;
            }

            WItem GetPlanned(EquipmentIndex idx)
            {
                int ii = (int)idx;
                if (ii < 0 || ii >= slotCount)
                    return null;
                return planned[ii];
            }

            var plan = new EquipPlan();

            foreach (var (slot, item) in changes)
            {
                int i = (int)slot;
                if (i < 0 || i >= slotCount)
                    continue;

                var from = planned[i];
                var to = item;

                bool same =
                    (from == null && to == null)
                    || (from != null && to != null && from.StringId == to.StringId);

                if (same)
                    continue;

                var decision = EquipRules.CanSetItem(ctx, GetPlanned, slot, to);

                if (!decision.Allowed)
                {
                    plan.AddSkip(decision.Reason);
                    continue;
                }

                plan.Changes[slot] = to;

                if (to == null)
                    plan.UnequipOps++;
                else
                    plan.EquipOps++;

                planned[i] = to;

                // Keep harness consistent when horse changes.
                if (slot == EquipmentIndex.Horse)
                {
                    var harness = planned[(int)EquipmentIndex.HorseHarness];
                    if (harness != null && to != null && !to.IsCompatibleWith(harness))
                    {
                        planned[(int)EquipmentIndex.HorseHarness] = null;
                        plan.Changes[EquipmentIndex.HorseHarness] = null;
                        plan.UnequipOps++;
                    }
                }
            }

            return plan;
        }
    }
}
