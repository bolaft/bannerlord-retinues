using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Features.Doctrines;
using Retinues.Core.Features.Doctrines.Catalog;
using Retinues.Core.Features.Unlocks.Behaviors;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Core.Editor
{
    [SafeClass]
    public static class EquipmentManager
    {
        public static List<(WItem, int?)> CollectAvailableItems(
            WCharacter troop,
            WFaction faction,
            EquipmentIndex slot
        )
        {
            Log.Debug(
                $"Collecting available items for troop {troop?.Name} (tier {troop?.Tier}), faction {faction?.Name}, slot {slot}."
            );

            // Check if all equipment is unlocked
            bool allEquipmentUnlocked = Config.GetOption<bool>("AllEquipmentUnlocked");

            // Initialize item list
            var items = new List<(WItem, int?)>();

            // Load items
            foreach (var item in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
            {
                try
                {
                    if (allEquipmentUnlocked)
                        items.Add((new WItem(item), null)); // All items
                    else
                    {
                        var wItem = new WItem(item); // Wrap item

                        if (wItem.IsCrafted && !DoctrineAPI.IsDoctrineUnlocked<ClanicTraditions>())
                            continue; // Skip crafted items
                        if (
                            Config.GetOption<int>("AllowedTierDifference")
                                < (wItem.Tier - troop.Tier)
                            && !DoctrineAPI.IsDoctrineUnlocked<Ironclad>()
                        )
                            continue; // Skip items that exceed the allowed tier difference unless Ironclad is unlocked

                        if (wItem.IsUnlocked)
                            items.Add((wItem, null)); // Unlocked items
                        else if (
                            Config.GetOption<bool>("UnlockFromCulture")
                            && item.Culture?.StringId == faction?.Culture?.StringId
                        )
                            items.Add((wItem, null)); // Items of the faction's culture
                        else if (
                            DoctrineAPI.IsDoctrineUnlocked<AncestralHeritage>()
                            && (
                                item.Culture?.StringId == Player.Clan?.Culture?.StringId
                                || item.Culture?.StringId == Player.Kingdom?.Culture?.StringId
                            )
                        )
                            items.Add((wItem, null)); // Items of the clan or kingdom's culture
                        else if (
                            UnlocksBehavior.IdsToProgress.ContainsKey(item.StringId)
                            && Config.GetOption<bool>("UnlockFromKills")
                        )
                        {
                            int progress = UnlocksBehavior.IdsToProgress[item.StringId];

                            if (progress >= Config.GetOption<int>("KillsForUnlock"))
                            {
                                wItem.Unlock(); // Unlock now if necessary
                                items.Add((wItem, null));
                            }
                            else
                            {
                                items.Add((wItem, progress)); // Items that are in progress
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

            // Filter by selected slot
            items = [.. items.Where(pair => pair.Item1 == null || pair.Item1.Slots.Contains(slot))];

            // Sort by int? (nulls first, then descending), then type, then name
            items =
            [
                .. items
                    .OrderBy(pair => (pair.Item2 == null ? 0 : 1, -(pair.Item2 ?? 0)))
                    .ThenBy(pair => pair.Item1?.Type)
                    .ThenBy(pair => pair.Item1?.Name),
            ];

            // Empty item to allow unequipping
            items.Insert(0, (null, null));

            return items;
        }

        public static void EquipFromStock(WCharacter troop, EquipmentIndex slot, WItem item)
        {
            Log.Debug($"Equipping item {item?.Name} from stock to troop {troop?.Name}.");

            item.Unstock(); // Reduce stock by 1
            Equip(troop, slot, item);
        }

        public static void EquipFromPurchase(WCharacter troop, EquipmentIndex slot, WItem item)
        {
            Log.Debug($"Purchasing and equipping item {item?.Name} to troop {troop?.Name}.");

            // Deduct cost and equip
            Player.ChangeGold(-GetItemValue(item, troop)); // Deduct cost
            Equip(troop, slot, item);
        }

        public static void Equip(WCharacter troop, EquipmentIndex slot, WItem item)
        {
            Log.Debug($"Equipping item {item?.Name} to troop {troop?.Name}.");

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
            Log.Debug($"Unequipping all items from troop {troop?.Name}.");

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

                if (DoctrineAPI.IsDoctrineUnlocked<RoyalPatronage>())
                    if (item?.Culture?.StringId == Player.Kingdom?.Culture?.StringId)
                        rebate += 0.10f; // 10% rebate on items of the kingdom's culture
            }
            catch { }

            return (int)(baseValue * (1.0f - rebate));
        }
    }
}
