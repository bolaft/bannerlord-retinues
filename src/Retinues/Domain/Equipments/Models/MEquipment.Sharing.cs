using Retinues.Domain.Equipments.Wrappers;
using TaleWorlds.Core;

namespace Retinues.Domain.Equipments.Models
{
    public partial class MEquipment
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Roster                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Slot-aware: returns true if setting this slot to item does not require increasing
        /// the roster stock for this item (because another equipment already needs that many).
        /// </summary>
        public bool IsAvailableInRoster(EquipmentIndex slot, WItem item)
        {
            if (item == null)
                return true;

            if (_roster == null)
                return false;

            string id = item.StringId;
            if (string.IsNullOrEmpty(id))
                return false;

            var old = Get(slot);
            if (old != null && old.StringId == id)
            {
                // No net change for this item.
                return true;
            }

            int thisCount = CountInThisEquipment(item);
            int newCount = thisCount + 1;

            int otherMax = _roster.GetMaxCountExcludingEquipment(this, item);

            // If someone else already requires >= newCount, roster already has enough copies.
            return otherMax >= newCount;
        }

        /// <summary>
        /// Counts how many copies of the specified item are in this equipment.
        /// </summary>
        private int CountInThisEquipment(WItem item)
        {
            int count = 0;

            for (int i = 0; i < SlotCount; i++)
            {
                var w = Get((EquipmentIndex)i);
                if (w != null && w == item)
                    count++;
            }

            return count;
        }
    }
}
