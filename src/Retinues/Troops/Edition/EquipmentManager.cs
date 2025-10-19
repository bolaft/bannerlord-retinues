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
    /// </summary>
    [SafeClass]
    public static class EquipmentManager
    {
        /// <summary>
        /// Collects all available items for a troop, faction, and slot, considering unlocks, doctrines, and config.
        /// </summary>
        public static List<(
            WItem item,
            bool isAvailable,
            bool isUnlocked,
            int progress
        )> CollectAvailableItems(
            WFaction faction,
            EquipmentIndex slot,
            List<(WItem item, bool unlocked, int progress)> cache = null
        )
        {
            Log.Debug($"[CollectAvailableItems] Called for faction {faction?.Name}, slot {slot}");

            // 1) Get (item, progress) eligibility from the caller cache or build once
            var eligible = cache ??= BuildEligibilityList(faction, slot); // same heavy scan you already have

            // 2) Availability filter (only build when restriction applies & you are in a town)
            var availableInTown = Config.RestrictItemsToTownInventory
                ? BuildCurrentTownAvailabilitySet()
                : null;

            var items = eligible
                .Select(p =>
                {
                    return (
                        item: p.item,
                        isAvailable: availableInTown == null || availableInTown.Contains(p.item),
                        isUnlocked: p.unlocked,
                        progress: p.progress
                    );
                })
                .ToList();

            // Keep your existing sort semantics:
            items =
            [
                .. items
                    .OrderBy(t =>
                        (
                            t.isUnlocked ? 0 : 1,
                            t.isUnlocked ? 0 : -t.progress,
                            t.isUnlocked ? (t.isAvailable ? 0 : 1) : 0
                        )
                    )
                    .ThenBy(t => t.item?.Type)
                    .ThenBy(t => t.item?.Name),
            ];

            Log.Debug(
                $"[CollectAvailableItems] Returning {items.Count} items for faction {faction?.Name}, slot {slot}"
            );

            return items;
        }

        /// <summary>
        /// Builds the list of eligible items for a faction, slot, and civilian status.
        /// </summary>
        private static List<(WItem item, bool unlocked, int progress)> BuildEligibilityList(
            WFaction faction,
            EquipmentIndex slot
        )
        {
            var hasClanicTraditions = DoctrineAPI.IsDoctrineUnlocked<ClanicTraditions>();
            var hasAncestral = DoctrineAPI.IsDoctrineUnlocked<AncestralHeritage>();

            var factionCultureId = faction?.Culture?.StringId;
            var clanCultureId = Player.Clan?.Culture?.StringId;
            var kingdomCultureId = Player.Kingdom?.Culture?.StringId;

            var allObjects = MBObjectManager.Instance.GetObjectTypeList<ItemObject>();
            var lastCraftedIndex = BuildLastCraftedIndex(allObjects, onlyPlayerCrafted: true);

            var list = new List<(WItem Item, bool Unlocked, int Progress)>();

            foreach (var io in allObjects)
            {
                var item = new WItem(io);

                try
                {
                    if (Config.AllEquipmentUnlocked)
                    {
                        if (item.Slots.Contains(slot))
                            list.Add((item, true, 0));
                        continue;
                    }

                    if (item.IsCrafted)
                    {
                        if (!hasClanicTraditions)
                            continue;
                        if (
                            !IsValidCraftedItem(
                                item.Base,
                                lastCraftedIndex,
                                onlyPlayerCrafted: true
                            )
                        )
                            continue;
                        if (!item.IsUnlocked)
                            item.Unlock();
                    }

                    if (item.IsUnlocked)
                    {
                        if (item.Slots.Contains(slot))
                            list.Add((item, true, 0));
                        continue;
                    }

                    var itemCultureId = item.Culture?.StringId;

                    if (Config.UnlockFromCulture && itemCultureId == factionCultureId)
                    {
                        if (item.Slots.Contains(slot))
                            list.Add((item, true, 0));
                        continue;
                    }

                    if (
                        hasAncestral
                        && (itemCultureId == clanCultureId || itemCultureId == kingdomCultureId)
                    )
                    {
                        if (item.Slots.Contains(slot))
                            list.Add((item, true, 0));
                        continue;
                    }

                    if (
                        Config.UnlockFromKills
                        && UnlocksBehavior.Instance.ProgressByItemId.TryGetValue(
                            item.StringId,
                            out var prog
                        )
                    )
                    {
                        if (prog >= Config.KillsForUnlock)
                        {
                            item.Unlock();
                            if (item.Slots.Contains(slot))
                                list.Add((item, true, 0));
                        }
                        else
                        {
                            if (item.Slots.Contains(slot))
                                list.Add((item, false, prog));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }

            return list;
        }

        /// <summary>
        /// Builds a set of items currently available in the player's town inventory, if applicable.
        /// </summary>
        private static HashSet<WItem> BuildCurrentTownAvailabilitySet()
        {
            if (Player.CurrentSettlement == null)
                return [];

            // WItem equality here should be by StringId otherwise switch to a HashSet<string> of item ids.
            var set = new HashSet<WItem>();
            foreach (var (item, count) in Player.CurrentSettlement.ItemCounts())
                if (count > 0)
                    set.Add(item);
            return set;
        }

        /// <summary>
        /// Returns true if the item is a valid crafted item to include, filtering out duplicates.
        /// </summary>
<<<<<<< HEAD
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

            return (int)(baseValue * (1.0f - rebate) * Config.EquipmentPriceModifier);
        }

        private static bool IncludeCraftedThisOne(
=======
        private static bool IsValidCraftedItem(
>>>>>>> wip/vm-refactor
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

        /// <summary>
        /// Builds an index of the last crafted item per design hash from a collection of items.
        /// </summary>
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

        /// <summary>
        /// Equips an item from stock to a troop, reducing stock by 1.
        /// </summary>
        public static void EquipFromStock(
            WCharacter troop,
            EquipmentIndex slot,
            WItem item,
            int index = 0
        )
        {
            Log.Debug(
                "[EquipFromStock] Called for item " + item?.Name + " and troop " + troop?.Name
            );

            Log.Debug("[EquipFromStock] Unstocking item.");
            item.Unstock(); // Reduce stock by 1

            Log.Debug("[EquipFromStock] Calling Equip.");
            Equip(troop, slot, item, index);
        }

        /// <summary>
        /// Purchases and equips an item to a troop, deducting gold.
        /// </summary>
        public static void EquipFromPurchase(
            WCharacter troop,
            EquipmentIndex slot,
            WItem item,
            int index = 0
        )
        {
            Log.Debug(
                "[EquipFromPurchase] Called for item " + item?.Name + " and troop " + troop?.Name
            );

            Log.Debug("[EquipFromPurchase] Deducting gold: " + GetItemCost(item, troop));
            Player.ChangeGold(-GetItemCost(item, troop)); // Deduct cost

            Log.Debug("[EquipFromPurchase] Calling Equip.");
            Equip(troop, slot, item, index);
        }

        /// <summary>
        /// Equips an item to a troop.
        /// </summary>
        public static void Equip(WCharacter troop, EquipmentIndex slot, WItem item, int index = 0)
        {
            // If unequipping a horse, also unequip the harness
            if (slot == EquipmentIndex.Horse && item == null)
            {
                Log.Debug("Unequipping horse also unequips harness.");
                Equip(troop, EquipmentIndex.HorseHarness, null, index);
            }

            if (Config.EquipmentChangeTakesTime && item != null)
                TroopEquipBehavior.StageChange(troop, slot, item, index);
            else
                troop.Equip(item, slot, index);
        }

        /// <summary>
        /// Gets the value of an item for a troop, applying doctrine rebates.
        /// </summary>
        public static int GetItemCost(WItem item, WCharacter troop)
        {
            if (item == null || troop == null)
                return 0;

            if (Config.PayForEquipment == false)
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

            return (int)(baseValue * (1.0f - rebate) * Config.EquipmentPriceModifier);
        }
    }
}
