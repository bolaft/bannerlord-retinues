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
        public static List<(WItem, int?, bool)> CollectAvailableItems(
            WFaction faction,
            EquipmentIndex slot,
            bool civilian = false,
            Dictionary<EquipmentIndex, List<(WItem item, int? progress)>> cache = null
        )
        {
            // 1) Get (item, progress) eligibility from the caller cache or build once
            if (cache == null || !cache.TryGetValue(slot, out var eligible))
            {
                eligible = BuildEligibilityList(faction, civilian, slot); // same heavy scan you already have
                cache?.Add(slot, eligible);
            }

            // 2) Lightweight availability: recompute every call
            HashSet<string> availableIds = null;
            if (Config.RestrictItemsToTownInventory && Player.CurrentSettlement?.IsTown == true)
            {
                availableIds =
                [
                    .. Player
                        .CurrentSettlement.ItemCounts()
                        .Where(t => t.count > 0)
                        .Select(t => t.item.StringId),
                ];
            }

            var items = eligible
                .Select(p => (p.item, p.progress, availableIds?.Contains(p.item?.StringId) == true))
                .ToList();

            // Keep your existing sort
            items =
            [
                .. items
                    .OrderBy(t =>
                        (
                            t.progress == null ? 0 : 1,
                            -(t.progress ?? 0),
                            t.progress == null ? (t.Item3 ? 0 : 1) : 0
                        )
                    )
                    .ThenBy(t => t.item?.Type)
                    .ThenBy(t => t.item?.Name),
            ];

            // “Empty” option
            items.Insert(0, (null, null, true));
            return items;
        }

        /// <summary>
        /// Builds the list of eligible items for a faction, slot, and civilian status.
        /// </summary>
        private static List<(WItem item, int? progress)> BuildEligibilityList(
            WFaction faction,
            bool civilian,
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

            var list = new List<(WItem, int?)>();

            foreach (var io in allObjects)
            {
                var item = new WItem(io);

                if (civilian && !item.IsCivilian)
                    continue;

                try
                {
                    if (Config.AllEquipmentUnlocked)
                    {
                        if (item.Slots.Contains(slot))
                            list.Add((item, null));
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
                            list.Add((item, null));
                        continue;
                    }

                    var itemCultureId = item.Culture?.StringId;

                    if (Config.UnlockFromCulture && itemCultureId == factionCultureId)
                    {
                        if (item.Slots.Contains(slot))
                            list.Add((item, null));
                        continue;
                    }

                    if (
                        hasAncestral
                        && (itemCultureId == clanCultureId || itemCultureId == kingdomCultureId)
                    )
                    {
                        if (item.Slots.Contains(slot))
                            list.Add((item, null));
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
                                list.Add((item, null));
                        }
                        else
                        {
                            if (item.Slots.Contains(slot))
                                list.Add((item, prog));
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
            if (!Config.RestrictItemsToTownInventory)
                return null;
            if (Player.CurrentSettlement == null || !Player.CurrentSettlement.IsTown)
                return null;

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
        private static bool IsValidCraftedItem(
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
            if (item == null)
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
