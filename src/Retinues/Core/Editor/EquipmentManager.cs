using System.Linq;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Game.Features.Unlocks;
using Retinues.Core.Utils;

namespace Retinues.Core.Editor
{
    public static class EquipmentManager
    {
        public static List<WItem> CollectAvailableItems(WCharacter troop, WFaction faction, EquipmentIndex slot)
        {
            // Initialize item list
            var items = new List<WItem>();
            
            // Load items
            foreach (var item in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
            {
                if (Config.GetOption<bool>("AllEquipmentUnlocked"))
                    items.Add(new WItem(item));  // All items
                else
                {
                    var wItem = new WItem(item);  // Wrap item

                    if (Config.GetOption<int>("AllowedTierDifference") < (wItem.Tier - troop.Tier))
                        continue; // Skip items that exceed the allowed tier difference
                    else if (UnlocksManager.UnlockedItems.Contains(wItem))
                        items.Add(wItem);  // Unlocked items
                    else if (Config.GetOption<bool>("UnlockFromCulture"))
                        if (item.Culture?.StringId == faction?.Culture?.StringId)
                            items.Add(wItem);  // Items of the faction's culture
                }
            }

            // Filter by selected slot
            items = [.. items.Where(i => i is null || i.Slots.Contains(slot))];

            // Sort by type, then name
            items = [.. items.OrderBy(i => i.Type).ThenBy(i => i.Name)];

            // Empty item to allow unequipping
            items.Insert(0, null);

            return items;
        }

        public static void EquipFromStock(WCharacter troop, EquipmentIndex slot, WItem item)
        {
            item.Unstock();  // Reduce stock by 1
            Equip(troop, slot, item);
        }

        public static void EquipFromPurchase(WCharacter troop, EquipmentIndex slot, WItem item)
        {
            Player.ChangeGold(-item.Value); // Deduct cost
            Equip(troop, slot, item);
        }

        public static void Equip(WCharacter troop, EquipmentIndex slot, WItem item)
        {
            // If equipping a new item, unequip the old one (if any)
            var oldItem = troop.Unequip(slot);

            // If the old item had a value, restock it
            if (oldItem != null && oldItem.Value > 0)
                oldItem.Stock();

            // If unequipping a horse, also unequip the harness
            if (slot == EquipmentIndex.Horse)
            {
                var harnessItem = troop.Equipment.GetItem(EquipmentIndex.HorseHarness);
                if (harnessItem != null)
                {
                    var oldHarness = troop.Unequip(EquipmentIndex.HorseHarness);
                    if (oldHarness != null && oldHarness.Value > 0)
                        oldHarness.Stock();
                }
            }

            // Equip item to selected troop
            troop.Equip(item, slot);
        }

        public static void UnequipAll(WCharacter troop)
        {
            foreach (var item in troop.UnequipAll())
            {
                // If the item had a value, restock it
                if (item != null && item.Value > 0)
                    item.Stock();
            }
        }
    }
}
