using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Features.Doctrines;
using Retinues.Core.Features.Doctrines.Catalog;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Core.Editor
{
    public static class EquipmentManager
    {
        public static List<WItem> CollectAvailableItems(
            WCharacter troop,
            WFaction faction,
            EquipmentIndex slot
        )
        {
            // Initialize item list
            var items = new List<WItem>();

            // Load items
            foreach (var item in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
            {
                if (Config.GetOption<bool>("AllEquipmentUnlocked"))
                    items.Add(new WItem(item)); // All items
                else
                {
                    var wItem = new WItem(item); // Wrap item

                    if (wItem.IsCrafted)
                        continue; // Skip crafted items

                    if (
                        Config.GetOption<int>("AllowedTierDifference") < (wItem.Tier - troop.Tier)
                        && !DoctrineAPI.IsDoctrineUnlocked<Ironclad>()
                    )
                        continue; // Skip items that exceed the allowed tier difference unless Ironclad is unlocked
                    else if (wItem.IsUnlocked)
                        items.Add(wItem); // Unlocked items
                    else if (
                        Config.GetOption<bool>("UnlockFromCulture")
                        || DoctrineAPI.IsDoctrineUnlocked<AncestralHeritage>()
                    )
                        if (item.Culture?.StringId == faction?.Culture?.StringId)
                            items.Add(wItem); // Items of the faction's culture
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
            item.Unstock(); // Reduce stock by 1
            Equip(troop, slot, item);
        }

        public static void EquipFromPurchase(WCharacter troop, EquipmentIndex slot, WItem item)
        {
            Player.ChangeGold(GetItemValue(item, troop)); // Deduct cost
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

        public static int GetItemValue(WItem item, WCharacter troop)
        {
            if (item == null)
                return 0;

            int baseValue = item?.Value ?? 0;
            float rebate = 0.0f;

            try
            {
                if (DoctrineAPI.IsDoctrineUnlocked<CulturalPride>())
                    if (item?.Culture?.StringId == troop.Culture?.StringId)
                        rebate += 0.10f; // 10% rebate on items of the clan's culture

                if (DoctrineAPI.IsDoctrineUnlocked<ClanicTraditions>())
                    if (item?.Culture?.StringId == Player.Clan.Culture.StringId)
                        rebate += 0.10f; // 10% rebate on items of the clan's culture

                if (DoctrineAPI.IsDoctrineUnlocked<RoyalPatronage>())
                    if (item?.Culture?.StringId == Player.Kingdom?.Culture?.StringId)
                        rebate += 0.10f; // 10% rebate on items of the kingdom's culture
            }
            catch { }

            return (int)(baseValue * (1.0f - rebate));
        }
    }
}
