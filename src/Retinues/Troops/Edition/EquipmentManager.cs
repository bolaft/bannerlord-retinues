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

            // 2) Availability filter
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

            return items;
        }

        /// <summary>
        /// Builds the list of eligible items for a faction, slot, and civilian status.
        /// </summary>
        private static List<(WItem item, bool unlocked, int progress)> BuildEligibilityList(
            WFaction faction,
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
            {
                troop.Unequip(slot, index, stock: true);
                troop.Equip(item, slot, index);
            }
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
                if (DoctrineAPI.IsDoctrineUnlocked<RoyalPatronage>())
                    if (item?.Culture == Player.Kingdom?.Culture)
                        rebate += 0.10f; // 10% rebate on items of the kingdom's culture
            }
            catch { }

            return (int)(baseValue * (1.0f - rebate) * Config.EquipmentPriceModifier);
        }
    }
}
