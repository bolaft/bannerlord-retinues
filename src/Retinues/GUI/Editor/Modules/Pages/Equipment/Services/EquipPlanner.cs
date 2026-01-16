using System;
using System.Collections.Generic;
using Retinues.Domain.Equipments.Wrappers;
using TaleWorlds.Core;

namespace Retinues.GUI.Editor.Modules.Pages.Equipment.Services
{
    public static class EquipPlanner
    {
        public static EquipPlan BuildPlan(
            EquipContext ctx,
            Func<EquipmentIndex, WItem> getCurrent,
            IEnumerable<(EquipmentIndex Slot, WItem Item)> changes
        )
        {
            if (ctx?.Equipment == null)
                return null;

            int slotCount = (int)EquipmentIndex.NumEquipmentSetSlots;

            var planned = new WItem[slotCount];

            for (int i = 0; i < slotCount; i++)
            {
                var idx = (EquipmentIndex)i;
                planned[i] = getCurrent(idx);
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
                    plan.AddSkip(decision.Reason, to);
                    continue;
                }

                plan.Changes[slot] = to;

                if (to == null)
                    plan.UnequipOps++;
                else
                    plan.EquipOps++;

                planned[i] = to;

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
