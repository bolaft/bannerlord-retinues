using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Wrappers;

namespace Retinues.Behaviors.Troops
{
    /// <summary>
    /// Partial Cloner utilities for unlocking and managing item unlock state.
    /// </summary>
    public static partial class Cloner
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Unlocks                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Unlocks all locked items for the given troop and returns the unlocked items.
        /// </summary>
        private static List<WItem> UnlockAllItems(WCharacter troop)
        {
            var list = new List<WItem>(16);

            foreach (WItem item in troop.EquipmentRoster.Items)
            {
                if (item == null)
                    continue;

                if (item.IsUnlocked)
                    continue;

                item.Unlock();
                list.Add(item);
            }

            return list;
        }
    }
}
