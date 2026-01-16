using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Wrappers;

namespace Retinues.Game.Troops
{
    public static partial class Cloner
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Unlocks                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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
