using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Features.Unlocks.Behaviors;
using Retinues.Features.Upgrade.Behaviors;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Troops.Edition
{
    /// <summary>
    /// Static helpers for managing troop equipment, available items, equipping, unequipping, and item value logic.
    /// Used by the equipment editor UI and backend.
    /// </summary>
    [SafeClass]
    public static class EquipmentManager
    {
        /// <summary>
        /// Collects all available items for a troop, faction, and slot, considering unlocks, doctrines, and config.
        /// </summary>
        public static List<(WItem, int?, bool)> CollectAvailableItems(
            WCharacter troop,
            WFaction faction,
            EquipmentIndex slot
        )
        {
            Log.Debug(
                $"Collecting available items for troop {troop?.Name} (tier {troop?.Tier}), faction {faction?.Name}, slot {slot}."
            );

            // Initialize item list
            var items = new List<(WItem, int?, bool)>();

            // Configuration
            var allUnlocked = Config.GetOption<bool>("AllEquipmentUnlocked");
            var allowFromCulture = Config.GetOption<bool>("UnlockFromCulture");
            var allowFromKills = Config.GetOption<bool>("UnlockFromKills");
            var killsForUnlock = Config.GetOption<int>("KillsForUnlock");
            var allowedTierDiff = Config.GetOption<int>("AllowedTierDifference");

            // Doctrines
            var hasClanicTraditions = DoctrineAPI.IsDoctrineUnlocked<ClanicTraditions>();
            var hasIronclad = DoctrineAPI.IsDoctrineUnlocked<Ironclad>();
            var hasAncestral = DoctrineAPI.IsDoctrineUnlocked<AncestralHeritage>();

            // Ids
            var factionCultureId = faction?.Culture?.StringId;
            var clanCultureId = Player.Clan?.Culture?.StringId;
            var kingdomCultureId = Player.Kingdom?.Culture?.StringId;

            // Load items
            foreach (
                var item in MBObjectManager
                    .Instance.GetObjectTypeList<ItemObject>()
                    .Select(i => new WItem(i))
            )
            {
                bool isAvailable =
                    item.IsStocked
                    || Config.GetOption<bool>("RestrictItemsToTownInventory") == false
                    || CurrentTownHasItem(item);

                try
                {
                    // All equipment unlocked: take everything
                    if (allUnlocked)
                    {
                        items.Add((item, null, isAvailable));
                        continue;
                    }

                    // 1) Crafted items gated by Clanic Traditions
                    if (item.IsCrafted)
                        if (!hasClanicTraditions)
                            continue;
                        else if (!item.IsUnlocked)
                            item.Unlock(); // unlock now

                    // 2) Tier constraint unless Ironclad is unlocked
                    var tierDelta = item.Tier - troop.Tier;
                    if (!hasIronclad && tierDelta > allowedTierDiff)
                        continue;

                    // 3) Already unlocked
                    if (item.IsUnlocked)
                    {
                        items.Add((item, null, isAvailable));
                        continue;
                    }

                    // 4) Culture-based unlocks
                    var itemCultureId = item.Culture?.StringId;

                    if (allowFromCulture && itemCultureId == factionCultureId)
                    {
                        items.Add((item, null, isAvailable));
                        continue;
                    }

                    if (
                        hasAncestral
                        && (itemCultureId == clanCultureId || itemCultureId == kingdomCultureId)
                    )
                    {
                        items.Add((item, null, isAvailable));
                        continue;
                    }

                    // 5) Kill-progress unlocks
                    if (
                        allowFromKills
                        && UnlocksBehavior.Instance.ProgressByItemId.TryGetValue(
                            item.StringId,
                            out var progress
                        )
                    )
                    {
                        if (progress >= killsForUnlock)
                        {
                            item.Unlock(); // unlock now
                            items.Add((item, null, isAvailable)); // now unlocked
                        }
                        else
                        {
                            items.Add((item, progress, isAvailable)); // still in progress
                        }
                    }
                    // else: not eligible this pass
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

            // Filter by selected slot
            items = [.. items.Where(pair => pair.Item1 == null || pair.Item1.Slots.Contains(slot))];

            // Sort by progress (nulls first, then descending), then by bool value (true first for nulls), then type, then name
            items =
            [
                .. items
                    .OrderBy(pair =>
                        (
                            pair.Item2 == null ? 0 : 1,
                            -(pair.Item2 ?? 0),
                            pair.Item2 == null ? (pair.Item3 ? 0 : 1) : 0
                        )
                    )
                    .ThenBy(pair => pair.Item1?.Type)
                    .ThenBy(pair => pair.Item1?.Name),
            ];

            // Empty item to allow unequipping
            items.Insert(0, (null, null, true));

            return items;
        }

        /// <summary>
        /// Checks if the current town has the specified item in its inventory.
        /// </summary>
        public static bool CurrentTownHasItem(WItem item)
        {
            var settlement = Player.CurrentSettlement;
            if (settlement == null)
                return false; // not in a settlement
            if (!settlement.IsTown)
                return false; // only towns have inventories

            return settlement.ItemCounts().Any(pair => pair.item == item && pair.count > 0);
        }

        /// <summary>
        /// Equips an item from stock to a troop, reducing stock by 1.
        /// </summary>
        public static void EquipFromStock(WCharacter troop, EquipmentIndex slot, WItem item)
        {
            Log.Debug(
                "[EquipFromStock] Called for item " + item?.Name + " and troop " + troop?.Name
            );

            Log.Debug("[EquipFromStock] Unstocking item.");
            item.Unstock(); // Reduce stock by 1

            Log.Debug("[EquipFromStock] Calling Equip.");
            Equip(troop, slot, item);
        }

        /// <summary>
        /// Purchases and equips an item to a troop, deducting gold.
        /// </summary>
        public static void EquipFromPurchase(WCharacter troop, EquipmentIndex slot, WItem item)
        {
            Log.Debug(
                "[EquipFromPurchase] Called for item " + item?.Name + " and troop " + troop?.Name
            );

            Log.Debug("[EquipFromPurchase] Deducting gold: " + GetItemValue(item, troop));
            Player.ChangeGold(-GetItemValue(item, troop)); // Deduct cost

            Log.Debug("[EquipFromPurchase] Calling Equip.");
            Equip(troop, slot, item);
        }

        /// <summary>
        /// Equips an item to a troop.
        /// </summary>
        public static void Equip(WCharacter troop, EquipmentIndex slot, WItem item)
        {
            // If unequipping a horse, also unequip the harness
            if (slot == EquipmentIndex.Horse && item == null)
            {
                Log.Debug("Unequipping horse also unequips harness.");
                Equip(troop, EquipmentIndex.HorseHarness, null);
            }

            if (Config.GetOption<bool>("EquipmentChangeTakesTime") && item != null)
                TroopEquipBehavior.StageEquipmentChange(troop, slot, item);
            else
                ApplyEquip(troop, slot, item);
        }

        /// <summary>
        /// Equips an item to a troop, unequipping old item and handling horse/harness logic.
        /// </summary>
        public static void ApplyEquip(WCharacter troop, EquipmentIndex slot, WItem item)
        {
            Log.Debug(
                $"Applying equip of item {item?.Name} to troop {troop?.Name} in slot {slot}."
            );
            troop.Unequip(slot)?.Stock();
            troop.Equip(item, slot);
        }

        /// <summary>
        /// Unequips all items from a troop and restocks them.
        /// </summary>
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

        /// <summary>
        /// Gets the value of an item for a troop, applying doctrine rebates.
        /// </summary>
        public static int GetItemValue(WItem item, WCharacter troop)
        {
            if (item == null)
                return 0;

            int baseValue = item?.Value ?? 0;
            float rebate = 0.0f;

            try
            {
                if (DoctrineAPI.IsDoctrineUnlocked<CulturalPride>())
                    if (item?.Culture == troop.Culture)
                        rebate += 0.10f; // 10% rebate on items of the clan's culture

                if (DoctrineAPI.IsDoctrineUnlocked<RoyalPatronage>())
                    if (item?.Culture == Player.Kingdom?.Culture)
                        rebate += 0.10f; // 10% rebate on items of the kingdom's culture
            }
            catch { }

            return (int)(
                baseValue * (1.0f - rebate) * Config.GetOption<float>("EquipmentPriceModifier")
            );
        }
    }
}
