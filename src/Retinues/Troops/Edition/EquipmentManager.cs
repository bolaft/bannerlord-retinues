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
using Retinues.GUI.Editor.VM;
using Retinues.GUI.Helpers;
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
            ITroopFaction faction,
            EquipmentIndex slot,
            List<(WItem item, bool unlocked, int progress)> cache = null,
            bool crafted = false
        )
        {
            // 1) Get (item, progress) eligibility from the caller cache or build once
            if (crafted == true)
            {
                // If caller explicitly asked for crafted-only but the doctrine isn't unlocked, return empty
                if (!DoctrineAPI.IsDoctrineUnlocked<ClanicTraditions>())
                    return [];

                // Ignore cache when requesting crafted-only
                cache = null;
            }

            var eligible = cache ??= BuildEligibilityList(faction, slot, crafted);

            HashSet<WItem> availableInTown = null;

            // 2) Availability filter (skip if crafted-only or in studio)
            if (crafted == false && !EditorVM.IsStudioMode && Config.RestrictItemsToTownInventory)
                availableInTown = BuildCurrentTownAvailabilitySet();

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

            return items;
        }

        /// <summary>
        /// Builds the list of eligible items for a faction, slot, and civilian status.
        /// </summary>
        private static List<(WItem item, bool unlocked, int progress)> BuildEligibilityList(
            ITroopFaction faction,
            EquipmentIndex slot,
            bool includeCrafted = true
        )
        {
            var craftedUnlocked = DoctrineAPI.IsDoctrineUnlocked<ClanicTraditions>();
            var cultureUnlocked = DoctrineAPI.IsDoctrineUnlocked<AncestralHeritage>();

            var factionCultureId = faction?.Culture?.StringId;
            var clanCultureId = Player.Clan?.Culture?.StringId;
            var kingdomCultureId = Player.Kingdom?.Culture?.StringId;

            var allObjects = MBObjectManager.Instance.GetObjectTypeList<ItemObject>();

            var list = new List<(WItem Item, bool Unlocked, int Progress)>();

            var craftedCodes = new HashSet<string>();

            foreach (var io in allObjects)
            {
                var item = new WItem(io);

                try
                {
                    if (includeCrafted)
                    {
                        if (item.IsCrafted)
                        {
                            if (craftedUnlocked)
                                if (item.Slots.Contains(slot))
                                {
                                    if (
                                        item.CraftedCode != null
                                        && !craftedCodes.Contains(item.CraftedCode)
                                    )
                                    {
                                        list.Add((item, true, 0));
                                        craftedCodes.Add(item.CraftedCode);
                                    }

                                    continue;
                                }
                            continue;
                        }
                        else
                            continue;
                    }
                    else if (item.IsCrafted)
                        continue;

                    if (Config.AllEquipmentUnlocked)
                    {
                        if (item.Slots.Contains(slot))
                            list.Add((item, true, 0));
                        continue;
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
                        cultureUnlocked
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

        public static void EquipFromStock(
            WCharacter troop,
            EquipmentIndex slot,
            WItem item,
            int index = 0
        )
        {
            Log.Debug("[EquipFromStock] " + item?.Name + " / " + troop?.Name);

            // multiplicity-based adjustment (no purchase allowed)
            if (!AdjustOwnershipForEquip(troop, index, slot, item, allowPurchase: false))
            {
                Notifications.Popup(
                    L.T("not_enough_stock_title", "Not Enough Stock"),
                    L.T("not_enough_stock_text", "You don't have enough copies in stock.")
                );
                return;
            }

            Equip(troop, slot, item, index);
        }

        public static void EquipFromPurchase(
            WCharacter troop,
            EquipmentIndex slot,
            WItem item,
            int index = 0
        )
        {
            Log.Debug("[EquipFromPurchase] " + item?.Name + " / " + troop?.Name);

            // multiplicity-based adjustment (purchase allowed if needed)
            if (!AdjustOwnershipForEquip(troop, index, slot, item, allowPurchase: true))
            {
                Notifications.Popup(
                    L.T("not_enough_gold_title", "Not Enough Gold"),
                    L.T(
                        "not_enough_gold_text",
                        "You do not have enough gold to purchase this item."
                    )
                );
                return;
            }

            Equip(troop, slot, item, index);
        }

        /// <summary>
        /// Equips an item to a troop.
        /// </summary>
        public static void Equip(
            WCharacter troop,
            EquipmentIndex slot,
            WItem item,
            int index = 0,
            bool stock = true
        )
        {
            // If unequipping a horse, also unequip the harness
            if (slot == EquipmentIndex.Horse && item == null)
            {
                Log.Debug("Unequipping horse also unequips harness.");
                Equip(troop, EquipmentIndex.HorseHarness, null, index);
            }

            if (!EditorVM.IsStudioMode && Config.EquipmentChangeTakesTime && item != null)
                TroopEquipBehavior.StageChange(troop, slot, item, index);
            else
            {
                troop.Unequip(slot, index, stock: stock);
                troop.Equip(item, slot, index);
            }
        }

        public static void Unequip(
            WCharacter troop,
            EquipmentIndex slot,
            int index = 0,
            bool stock = false
        )
        {
            var loadout = troop?.Loadout;
            var eq = loadout?.Get(index);
            var oldItem = eq?.Get(slot);
            if (oldItem == null)
            {
                troop.Equip(null, slot, index); // no-op but keeps cascades consistent
                return;
            }

            // Compute multiplicity delta BEFORE removing
            int oldMaxBefore = loadout.MaxCountPerSet(oldItem);
            int newCountThisSet = Math.Max(0, loadout.CountInSet(oldItem, index) - 1);
            int oldMaxAfter = Math.Max(newCountThisSet, MaxOverOtherSets(loadout, oldItem, index));
            int deltaRemove = Math.Max(0, oldMaxBefore - oldMaxAfter); // copies freed

            // Actually unequip (runs formation/requirements updates)
            troop.Equip(null, slot, index);

            // Refund freed copies if caller asked to stock them
            if (stock && deltaRemove > 0)
                for (int i = 0; i < deltaRemove; i++)
                    oldItem.Stock();
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

            if (EditorVM.IsStudioMode)
                return 0; // No cost in studio mode

            int baseValue = item?.Value ?? 0;

            return (int)(baseValue * Config.EquipmentPriceModifier);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //           Ownership delta / multiplicity core          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static int GetStockCount(WItem item)
        {
            // If you expose StockCount on WItem, use it; otherwise emulate with IsStocked bool -> 0/1.
            try
            {
                return item?.GetStock() ?? (item?.IsStocked == true ? 1 : 0);
            }
            catch
            {
                return item?.IsStocked == true ? 1 : 0;
            }
        }

        /// <summary>
        /// Adjusts inventory (stock / payment) for replacing a slot with 'newItem'.
        /// Applies the multiplicity rule: required copies = max per single set.
        /// Returns true if adjustment succeeded (enough stock or gold when needed).
        /// </summary>
        private static bool AdjustOwnershipForEquip(
            WCharacter troop,
            int setIndex,
            EquipmentIndex slot,
            WItem newItem,
            bool allowPurchase
        )
        {
            var loadout = troop?.Loadout;
            if (loadout == null)
                return true;

            var eq = loadout.Get(setIndex);
            var oldItem = eq?.Get(slot);

            // No change → no adjustment
            if (oldItem == newItem)
                return true;

            // REMOVAL DELTA (old item)
            int oldMaxBefore = loadout.MaxCountPerSet(oldItem);
            // If we remove oldItem from this slot, what's the new max?
            int newCountThisSetOldItem =
                oldItem != null ? Math.Max(0, loadout.CountInSet(oldItem, setIndex) - 1) : 0;
            int oldMaxAfter =
                oldItem != null
                    ? Math.Max(newCountThisSetOldItem, MaxOverOtherSets(loadout, oldItem, setIndex))
                    : 0;
            int deltaRemove = Math.Max(0, oldMaxBefore - oldMaxAfter); // copies freed

            // ADDITION DELTA (new item)
            int newMaxBefore = loadout.MaxCountPerSet(newItem);
            int newCountThisSetNewItem =
                newItem != null ? loadout.CountInSet(newItem, setIndex) + 1 : 0;
            int newMaxAfter =
                newItem != null
                    ? Math.Max(newCountThisSetNewItem, MaxOverOtherSets(loadout, newItem, setIndex))
                    : 0;
            int deltaAdd = Math.Max(0, newMaxAfter - newMaxBefore); // extra copies needed

            // Apply: first add (need copies), then remove (refund freed)
            if (deltaAdd > 0 && newItem != null)
            {
                // try to consume from stock first
                int stock = GetStockCount(newItem);
                int fromStock = Math.Min(stock, deltaAdd);
                for (int i = 0; i < fromStock; i++)
                    newItem.Unstock();

                int stillNeeded = deltaAdd - fromStock;
                if (stillNeeded > 0)
                {
                    if (!allowPurchase || Config.PayForEquipment == false)
                        return false; // blocked — not allowed to buy here

                    int unitCost = GetItemCost(newItem, troop);
                    int totalCost = unitCost * stillNeeded;
                    if (Player.Gold < totalCost)
                        return false;

                    // purchase 'stillNeeded' copies: stock then immediately consume
                    if (unitCost > 0)
                        Player.ChangeGold(-totalCost);
                    for (int i = 0; i < stillNeeded; i++)
                    {
                        newItem.Stock();
                        newItem.Unstock();
                    }
                }
            }

            // Refund freed copies to stock for oldItem
            if (deltaRemove > 0 && oldItem != null)
            {
                for (int i = 0; i < deltaRemove; i++)
                    oldItem.Stock();
            }

            return true;
        }

        /// <summary>
        /// Helper: max count of 'item' considering all sets except the given one.
        /// </summary>
        private static int MaxOverOtherSets(WLoadout loadout, WItem item, int excludingSet)
        {
            if (item == null)
                return 0;
            int max = 0;
            for (int i = 0; i < loadout.Equipments.Count; i++)
            {
                if (i == excludingSet)
                    continue;
                int c = loadout.CountInSet(item, i);
                if (c > max)
                    max = c;
            }
            return max;
        }
    }
}
