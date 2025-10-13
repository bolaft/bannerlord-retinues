using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
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
            EquipmentIndex slot,
            bool civilianOnly = false
        )
        {
            Log.Debug(
                $"Collecting available items for troop {troop?.Name} (tier {troop?.Tier}), faction {faction?.Name}, slot {slot}."
            );

            // Initialize item list
            var items = new List<(WItem, int?, bool)>();

            // Doctrines
            var hasClanicTraditions = DoctrineAPI.IsDoctrineUnlocked<ClanicTraditions>();
            var hasIronclad = DoctrineAPI.IsDoctrineUnlocked<Ironclad>();
            var hasAncestral = DoctrineAPI.IsDoctrineUnlocked<AncestralHeritage>();

            // Ids
            var factionCultureId = faction?.Culture?.StringId;
            var clanCultureId = Player.Clan?.Culture?.StringId;
            var kingdomCultureId = Player.Kingdom?.Culture?.StringId;

            var allObjects = MBObjectManager.Instance.GetObjectTypeList<ItemObject>();
            var lastCraftedIndex = BuildLastCraftedIndex(allObjects, onlyPlayerCrafted: true);

            // Load items
            foreach (var item in allObjects.Select(i => new WItem(i)))
            {
                if (civilianOnly && !item.IsCivilian)
                    continue; // Skip non-civilian items if filtering for civilian only

                bool isAvailable =
                    item.IsStocked
                    || Config.RestrictItemsToTownInventory == false
                    || CurrentTownHasItem(item);

                try
                {
                    // All equipment unlocked: take everything
                    if (Config.AllEquipmentUnlocked)
                    {
                        items.Add((item, null, isAvailable));
                        continue;
                    }

                    // 1) Crafted items gated by Clanic Traditions
                    if (item.IsCrafted)
                    {
                        if (!hasClanicTraditions)
                            continue;
                        else if (
                            !IncludeCraftedThisOne(
                                item.Base,
                                lastCraftedIndex,
                                onlyPlayerCrafted: true
                            )
                        )
                            continue;
                        else if (!item.IsUnlocked)
                            item.Unlock(); // unlock now
                    }

                    // 2) Tier constraint unless Ironclad is unlocked
                    var tierDelta = item.Tier - troop.Tier;
                    if (!hasIronclad && tierDelta > Config.AllowedTierDifference)
                        continue;

                    // 3) Already unlocked
                    if (item.IsUnlocked)
                    {
                        items.Add((item, null, isAvailable));
                        continue;
                    }

                    // 4) Culture-based unlocks
                    var itemCultureId = item.Culture?.StringId;

                    if (Config.UnlockFromCulture && itemCultureId == factionCultureId)
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
                        Config.UnlockFromKills
                        && UnlocksBehavior.Instance.ProgressByItemId.TryGetValue(
                            item.StringId,
                            out var progress
                        )
                    )
                    {
                        if (progress >= Config.KillsForUnlock)
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
        public static void EquipFromStock(
            WCharacter troop,
            EquipmentIndex slot,
            WItem item,
            WLoadout.Category category,
            int index = 0
        )
        {
            Log.Debug(
                "[EquipFromStock] Called for item " + item?.Name + " and troop " + troop?.Name
            );

            Log.Debug("[EquipFromStock] Unstocking item.");
            item.Unstock(); // Reduce stock by 1

            Log.Debug("[EquipFromStock] Calling Equip.");
            Equip(troop, slot, item, category, index);
        }

        /// <summary>
        /// Purchases and equips an item to a troop, deducting gold.
        /// </summary>
        public static void EquipFromPurchase(
            WCharacter troop,
            EquipmentIndex slot,
            WItem item,
            WLoadout.Category category,
            int index = 0
        )
        {
            Log.Debug(
                "[EquipFromPurchase] Called for item " + item?.Name + " and troop " + troop?.Name
            );

            Log.Debug("[EquipFromPurchase] Deducting gold: " + GetItemValue(item, troop));
            Player.ChangeGold(-GetItemValue(item, troop)); // Deduct cost

            Log.Debug("[EquipFromPurchase] Calling Equip.");
            Equip(troop, slot, item, category, index);
        }

        /// <summary>
        /// Equips an item to a troop.
        /// </summary>
        public static void Equip(
            WCharacter troop,
            EquipmentIndex slot,
            WItem item,
            WLoadout.Category category,
            int index = 0
        )
        {
            // If unequipping a horse, also unequip the harness
            if (slot == EquipmentIndex.Horse && item == null)
            {
                Log.Debug("Unequipping horse also unequips harness.");
                Equip(troop, EquipmentIndex.HorseHarness, null, category, index);
            }

            if (Config.EquipmentChangeTakesTime && item != null)
                TroopEquipBehavior.StageEquipmentChange(troop, slot, item, category, index);
            else
                ApplyEquip(troop, slot, item, category, index);
        }

        /// <summary>
        /// Equips an item to a troop, unequipping old item and handling horse/harness logic.
        /// </summary>
        public static void ApplyEquip(
            WCharacter troop,
            EquipmentIndex slot,
            WItem item,
            WLoadout.Category category,
            int index = 0
        )
        {
            Log.Debug(
                $"Applying equip of item {item?.Name} to troop {troop?.Name} in slot {slot}."
            );
            troop.Unequip(slot, category, index)?.Stock();
            troop.Equip(item, slot, category, index);
        }

        /// <summary>
        /// Unequips all items from a troop and restocks them.
        /// </summary>
        public static void UnequipAll(WCharacter troop, WLoadout.Category category, int index = 0)
        {
            Log.Debug($"Unequipping all items from troop {troop?.Name}.");

            foreach (var item in troop.UnequipAll(category, index))
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
                baseValue * (1.0f - rebate) * Config.EquipmentPriceModifier
            );
        }

        private static bool IncludeCraftedThisOne(
            ItemObject obj,
            Dictionary<string, ItemObject> lastCraftedIndex,
            bool onlyPlayerCrafted = true
        )
        {
            if (obj == null)
                return false;

            // Non-crafted items always pass through
            if (!obj.IsCraftedWeapon || obj.WeaponDesign == null)
                return true;

            if (onlyPlayerCrafted && !obj.IsCraftedByPlayer)
                return true; // not a player-crafted smith, keep normal path

            var hash = obj.WeaponDesign.HashedCode;
            if (string.IsNullOrEmpty(hash))
                return true; // no hash to dedup on, let it through

            // Keep only the "last" instance for this hash
            return lastCraftedIndex.TryGetValue(hash, out var last) && ReferenceEquals(last, obj);
        }

        private static Dictionary<string, ItemObject> BuildLastCraftedIndex(
            IEnumerable<ItemObject> items,
            bool onlyPlayerCrafted = true
        )
        {
            var map = new Dictionary<string, ItemObject>(StringComparer.Ordinal);
            foreach (var obj in items)
            {
                if (obj == null)
                    continue;
                if (!obj.IsCraftedWeapon || obj.WeaponDesign == null)
                    continue;
                if (onlyPlayerCrafted && !obj.IsCraftedByPlayer)
                    continue;

                var hash = obj.WeaponDesign.HashedCode;
                if (string.IsNullOrEmpty(hash))
                    continue;

                // later items overwrite earlier ones -> "last wins"
                map[hash] = obj;
            }
            return map;
        }
    }
}
