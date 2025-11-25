using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Features.Staging;
using Retinues.Features.Unlocks;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Managers
{
    /// <summary>
    /// Equipment rules and action flows: validation, affordability, multiplicity deltas,
    /// staging decisions and application. No UI here.
    /// </summary>
    [SafeClass]
    public static class EquipmentManager
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Public Result Types                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public enum EquipFailReason
        {
            None = 0,
            NotAllowed = 1,
            NotEnoughStock = 2,
            NotEnoughGold = 3,
            NotCivilian = 4,
        }

        /// <summary>
        /// Non-mutating cost/behavior preview for equipping an item.
        /// </summary>
        public sealed class EquipQuote
        {
            public bool IsChange; // false if old == new
            public int DeltaAdd; // physical copies to add
            public int DeltaRemove; // physical copies to destroy/refund
            public int CopiesFromStock; // how many of DeltaAdd can be taken from stock
            public int CopiesToBuy; // how many of DeltaAdd must be purchased
            public int GoldCost; // total gold cost for CopiesToBuy (0 in studio or PayForEquipment=false)
            public bool WouldStage; // true iff DeltaAdd>0 and Config.EquipmentChangeTakesTime and not studio
        }

        /// <summary>
        /// Mutating result for equip/unequip operations.
        /// </summary>
        public sealed class EquipResult
        {
            public bool Ok;
            public bool Staged; // true if a staged task was created (loadout not yet changed)
            public EquipFailReason Reason;
            public int GoldDelta; // negative when gold was spent, positive on refunds (if you choose to support)
            public int AddedCopies; // consumed/bought now
            public int RefundedCopies; // returned to stock now
        }

        /// <summary>
        /// Non-mutating quote for deleting a set.
        /// </summary>
        public sealed class DeleteSetQuote
        {
            public Dictionary<WItem, int> Refunds; // item -> copies to refund
        }

        /// <summary>
        /// Mutating result for deleting a set.
        /// </summary>
        public sealed class DeleteSetResult
        {
            public bool Ok;
            public Dictionary<WItem, int> Refunded; // item -> copies refunded
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Availability                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Collects all available items for a troop, faction, and slot, considering unlocks, doctrines, and config.
        /// </summary>
        public static List<(
            WItem item,
            bool isAvailable,
            bool isUnlocked,
            int progress
        )> CollectAvailableItems(
            BaseFaction faction,
            EquipmentIndex slot,
            List<(WItem item, bool unlocked, int progress)> cache = null,
            bool craftedOnly = false
        )
        {
            if (cache == null)
                Log.Info($"Building equipment eligibility cache for slot {slot}");
            else
                Log.Info($"Using provided equipment eligibility cache for slot {slot}");
            if (craftedOnly)
            {
                if (!DoctrineAPI.IsDoctrineUnlocked<ClanicTraditions>())
                    return [];
                cache = null; // ignore caller cache for crafted-only
            }

            var eligible = cache ?? BuildEligibilityList(faction, slot, craftedOnly: craftedOnly);

            HashSet<string> availableInTown = null;
            if (!craftedOnly && !ClanScreen.IsStudioMode && Config.RestrictItemsToTownInventory)
                availableInTown = BuildCurrentTownAvailabilitySet();

            var items = new List<(WItem, bool, bool, int)>(eligible.Count);
            foreach (var (item, unlocked, progress) in eligible)
            {
                bool okTown = availableInTown == null || availableInTown.Contains(item.StringId);
                items.Add((item, okTown, unlocked, progress));
            }
            return items;
        }

        /// <summary>
        /// Builds a list of items eligible for equipping into the given slot,
        /// considering unlocks and config, but ignoring town stock.
        /// </summary>
        private static List<(WItem item, bool unlocked, int progress)> BuildEligibilityList(
            BaseFaction faction,
            EquipmentIndex slot,
            bool craftedOnly
        )
        {
            bool craftedUnlocked = DoctrineAPI.IsDoctrineUnlocked<ClanicTraditions>();
            bool cultureUnlocked = DoctrineAPI.IsDoctrineUnlocked<AncestralHeritage>();

            var factionCultureId = faction?.Culture?.StringId;
            var clanCultureId = Player.Clan?.Culture?.StringId;
            var kingdomCultureId = Player.Kingdom?.Culture?.StringId;

            var allObjects = MBObjectManager.Instance.GetObjectTypeList<ItemObject>();
            var list = new List<(WItem, bool, int)>();
            var craftedCodes = new HashSet<string>();

            foreach (var io in allObjects)
            {
                var item = new WItem(io);

                try
                {
                    if (craftedOnly)
                    {
                        if (item.IsCrafted)
                        {
                            if (!craftedUnlocked)
                                continue;
                            if (!item.Slots.Contains(slot))
                                continue;

                            if (
                                item.CraftedCode != null
                                && !craftedCodes.Contains(item.CraftedCode)
                            )
                            {
                                list.Add((item, true, 0));
                                craftedCodes.Add(item.CraftedCode);
                            }
                        }
                        continue;
                    }
                    else
                    {
                        if (item.IsCrafted)
                            continue;
                    }

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

                    if (Config.AllCultureEquipmentUnlocked && itemCultureId == factionCultureId)
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
                        Config.UnlockItemsFromKills
                        && UnlocksBehavior.Instance.ProgressByItemId.TryGetValue(
                            item.StringId,
                            out var prog
                        )
                    )
                    {
                        if (prog >= Config.RequiredKillsPerItem)
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
        /// Builds a set of items available in the current town's inventory.
        /// Returns null if no restriction is to be applied.
        /// </summary>
        private static HashSet<string> BuildCurrentTownAvailabilitySet()
        {
            if (Player.CurrentSettlement == null)
                return null;

            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (item, count) in Player.CurrentSettlement.ItemCounts())
                if (count > 0)
                    set.Add(item.StringId);
            return set;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Pure Checks / Quoting                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Can this troop equip the item into the given slot of the set, ignoring stock/gold?
        /// </summary>
        public static bool CanEquip(WCharacter troop, WItem item)
        {
            if (troop == null)
                return false;

            // Example minimal checks:
            if (item != null && item.RelevantSkill != null)
                if (!MeetsItemSkillRequirements(troop, item))
                    return false;

            // No horse rule
            if (Config.DisallowMountsForT1Troops && !troop.IsHero)
                if (troop.Tier <= 1 && item != null && item.IsHorse)
                    return false;

            // Tier cap rule
            if ((item?.Tier ?? 0) - troop.Tier > Config.AllowedTierDifference && !troop.IsHero)
                return DoctrineAPI.IsDoctrineUnlocked<Ironclad>();

            return true;
        }

        /// <summary>
        /// Returns true if the troop meets the skill requirements for the specified item.
        /// </summary>
        public static bool MeetsItemSkillRequirements(WCharacter troop, WItem item)
        {
            if (item == null)
                return true;
            if (item.RelevantSkill == null)
                return true;
            return item.Difficulty <= troop.GetSkill(item.RelevantSkill);
        }

        /// <summary>
        /// Non-mutating preview for an equip change, including multiplicity deltas and whether it would stage.
        /// </summary>
        public static EquipQuote QuoteEquip(
            WCharacter troop,
            int setIndex,
            EquipmentIndex slot,
            WItem newItem
        )
        {
            var q = new EquipQuote();

            var loadout = troop.Loadout;
            var eq = loadout.Get(setIndex);
            var oldItem = eq?.Get(slot);

            q.IsChange = oldItem != newItem;

            // No change -> trivial quote
            if (!q.IsChange)
            {
                q.DeltaAdd = 0;
                q.DeltaRemove = 0;
                q.CopiesFromStock = 0;
                q.CopiesToBuy = 0;
                q.GoldCost = 0;
                q.WouldStage = false;
                return q;
            }

            // Determine the paired captain/base troop (if any)
            WCharacter counterpart = null;
            if (troop != null)
            {
                if (troop.IsCaptain && troop.BaseTroop != null)
                    counterpart = troop.BaseTroop;
                else if (!troop.IsCaptain && troop.Captain != null)
                    counterpart = troop.Captain;
            }

            int GlobalMaxCountPerSet(WItem item)
            {
                if (item == null)
                    return 0;

                int max = loadout.MaxCountPerSet(item);
                if (counterpart != null)
                {
                    var otherLoadout = counterpart.Loadout;
                    int otherMax = otherLoadout.MaxCountPerSet(item);
                    if (otherMax > max)
                        max = otherMax;
                }
                return max;
            }

            int GlobalRequiredAfterForItem(WItem item)
            {
                if (item == null)
                    return 0;

                // This troop after the hypothetical change
                int thisAfter = loadout.RequiredAfterForItem(item, setIndex, slot, newItem);

                // Counterpart stays unchanged, we just take its current max-per-set
                if (counterpart == null)
                    return thisAfter;

                int otherMax = counterpart.Loadout.MaxCountPerSet(item);
                return thisAfter > otherMax ? thisAfter : otherMax;
            }

            // Compute deltas using the GLOBAL "what-if" helpers (troop + captain/base)
            int beforeOld = GlobalMaxCountPerSet(oldItem);
            int afterOld = oldItem != null ? GlobalRequiredAfterForItem(oldItem) : 0;

            int beforeNew = GlobalMaxCountPerSet(newItem);
            int afterNew = newItem != null ? GlobalRequiredAfterForItem(newItem) : 0;

            q.DeltaRemove = Math.Max(0, beforeOld - afterOld);
            q.DeltaAdd = Math.Max(0, afterNew - beforeNew);

            // Figure out how many of DeltaAdd we can take from stock
            int stock = newItem != null ? newItem.GetStock() : 0;
            q.CopiesFromStock = Math.Min(stock, q.DeltaAdd);
            q.CopiesToBuy = Math.Max(0, q.DeltaAdd - q.CopiesFromStock);

            // Cost preview
            int unitCost = GetItemCost(newItem);
            q.GoldCost = unitCost * q.CopiesToBuy;

            // Staging decision - only if adding physical copies
            bool stagingPossible = Config.EquippingTroopsTakesTime && !ClanScreen.IsStudioMode;
            q.WouldStage = stagingPossible && q.DeltaAdd > 0;

            return q;
        }

        /// <summary>
        /// Check affordability of an equip quote.
        /// </summary>
        private static EquipFailReason CheckAffordability(in EquipQuote q, bool allowPurchase)
        {
            if (q.DeltaAdd <= 0)
                return EquipFailReason.None;

            // Free mode: no stock or gold gating.
            if (!Config.EquippingTroopsCostsGold)
                return EquipFailReason.None;

            if (q.CopiesFromStock < q.DeltaAdd && !allowPurchase)
                return EquipFailReason.NotEnoughStock;

            if (q.CopiesToBuy > 0 && Player.Gold < q.GoldCost)
                return EquipFailReason.NotEnoughGold;

            return EquipFailReason.None;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Mutating Flows                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Try to equip an item into a slot. Handles affordability, stock, multiplicity, staging,
        /// and applies to the loadout immediately or via staging as per config.
        /// </summary>
        public static EquipResult TryEquip(
            WCharacter troop,
            int setIndex,
            EquipmentIndex slot,
            WItem newItem,
            bool allowPurchase = true
        )
        {
            var res = new EquipResult
            {
                Ok = false,
                Staged = false,
                Reason = EquipFailReason.None,
            };

            if (troop.Loadout.Get(setIndex).IsCivilian)
            {
                if (newItem?.IsCivilian == false)
                {
                    res.Reason = EquipFailReason.NotCivilian;
                    return res;
                }
            }

            if (!CanEquip(troop, newItem))
            {
                res.Reason = EquipFailReason.NotAllowed;
                return res;
            }

            if (ClanScreen.IsStudioMode)
                return TryEquip_Studio(troop, setIndex, slot, newItem, res);
            else
                return TryEquip_Custom(troop, setIndex, slot, newItem, res, allowPurchase);
        }

        public static EquipResult TryEquip_Studio(
            WCharacter troop,
            int setIndex,
            EquipmentIndex slot,
            WItem newItem,
            EquipResult res
        )
        {
            var loadoutStudio = troop.Loadout;
            var eqStudio = loadoutStudio.Get(setIndex);
            var oldItemStudio = eqStudio?.Get(slot);

            // If no change, just succeed
            if (oldItemStudio == newItem)
            {
                res.Ok = true;
                return res;
            }

            // Instant apply to the loadout.
            // Horse rule: removing horse also removes harness.
            ApplyStructureWithHorseRule(troop, setIndex, slot, newItem);

            // No stock/gold/staging deltas in studio
            res.Ok = true;
            res.Staged = false;
            res.GoldDelta = 0;
            res.AddedCopies = 0;
            res.RefundedCopies = 0;
            return res;
        }

        public static EquipResult TryEquip_Custom(
            WCharacter troop,
            int setIndex,
            EquipmentIndex slot,
            WItem newItem,
            EquipResult res,
            bool allowPurchase = true
        )
        {
            var loadout = troop.Loadout;
            var eq = loadout.Get(setIndex);
            var oldItem = eq?.Get(slot);

            var q = QuoteEquip(troop, setIndex, slot, newItem);

            if (!q.IsChange)
            {
                res.Ok = true;
                return res;
            }

            // Affordability
            if (q.DeltaAdd > 0)
            {
                var reason = CheckAffordability(q, allowPurchase);
                if (reason != EquipFailReason.None)
                {
                    res.Reason = reason;
                    return res;
                }
            }

            // Apply acquisitions now (stock consumption and purchases)
            if (q.DeltaAdd > 0 && newItem != null && Config.EquippingTroopsCostsGold)
            {
                // consume from stock
                for (int i = 0; i < q.CopiesFromStock; i++)
                    newItem.Unstock();

                // buy remainder (stock then consume)
                if (q.CopiesToBuy > 0)
                {
                    int unitCost = GetItemCost(newItem);
                    if (unitCost > 0)
                        Player.ChangeGold(-unitCost * q.CopiesToBuy);

                    for (int i = 0; i < q.CopiesToBuy; i++)
                    {
                        newItem.Stock();
                        newItem.Unstock();
                    }

                    res.GoldDelta = -unitCost * q.CopiesToBuy;
                }

                res.AddedCopies = q.DeltaAdd;
            }

            // Apply reductions immediately (return freed copies to stock now)
            if (q.DeltaRemove > 0 && oldItem != null && Config.EquippingTroopsCostsGold)
            {
                for (int i = 0; i < q.DeltaRemove; i++)
                    oldItem.Stock();

                res.RefundedCopies = q.DeltaRemove;
            }

            // Decide staging vs instant
            bool shouldStage = q.WouldStage; // only DeltaAdd>0 under config and not studio

            if (shouldStage)
            {
                // Stage the change. Acquisition already done now.
                EquipStagingBehavior.Stage(troop, slot, newItem, setIndex);
                res.Ok = true;
                res.Staged = true;
                return res;
            }

            // Instant apply to the loadout.
            // Horse rule: removing horse also removes harness (structure-only)
            ApplyStructureWithHorseRule(troop, setIndex, slot, newItem);

            res.Ok = true;
            res.Staged = false;
            return res;
        }

        /// <summary>
        /// Try to unequip a slot (apply immediately, never staged).
        /// Handles multiplicity reduction refunds.
        /// </summary>
        public static EquipResult TryUnequip(WCharacter troop, int setIndex, EquipmentIndex slot)
        {
            var res = new EquipResult
            {
                Ok = false,
                Staged = false,
                Reason = EquipFailReason.None,
            };

            var loadout = troop.Loadout;
            var eq = loadout.Get(setIndex);
            var oldItem = eq?.Get(slot);
            if (oldItem == null)
            {
                res.Ok = true;
                return res;
            }

            // Determine paired captain/base troop for global pooling
            WCharacter counterpart = null;
            if (troop != null)
            {
                if (troop.IsCaptain && troop.BaseTroop != null)
                    counterpart = troop.BaseTroop;
                else if (!troop.IsCaptain && troop.Captain != null)
                    counterpart = troop.Captain;
            }

            int GlobalMaxCountPerSet(WItem item)
            {
                if (item == null)
                    return 0;

                int max = loadout.MaxCountPerSet(item);
                if (counterpart != null)
                {
                    var otherLoadout = counterpart.Loadout;
                    int otherMax = otherLoadout.MaxCountPerSet(item);
                    if (otherMax > max)
                        max = otherMax;
                }
                return max;
            }

            int GlobalRequiredAfterForItem(WItem item)
            {
                if (item == null)
                    return 0;

                int thisAfter = loadout.RequiredAfterForItem(item, setIndex, slot, null);
                if (counterpart == null)
                    return thisAfter;

                int otherMax = counterpart.Loadout.MaxCountPerSet(item);
                return thisAfter > otherMax ? thisAfter : otherMax;
            }

            int beforeOld = GlobalMaxCountPerSet(oldItem);
            int afterOld = GlobalRequiredAfterForItem(oldItem);
            int deltaRemove = Math.Max(0, beforeOld - afterOld);

            // Horse rule: unequipping horse also clears harness (and may refund its copies)
            if (slot == EquipmentIndex.Horse)
            {
                var harness = eq.Get(EquipmentIndex.HorseHarness);
                if (harness != null)
                {
                    int beforeHarness = loadout.MaxCountPerSet(harness);
                    int afterHarness = loadout.RequiredAfterForItem(
                        harness,
                        setIndex,
                        EquipmentIndex.HorseHarness,
                        null
                    );
                    int deltaHarness = Math.Max(0, beforeHarness - afterHarness);

                    // Apply structure first for harness
                    loadout.Apply(setIndex, EquipmentIndex.HorseHarness, null);
                    for (int i = 0; i < deltaHarness; i++)
                        harness.Stock();

                    res.RefundedCopies += deltaHarness;
                }
            }

            // Apply structure
            loadout.Apply(setIndex, slot, null);

            // Refund freed copies
            for (int i = 0; i < deltaRemove; i++)
                oldItem.Stock();

            res.RefundedCopies += deltaRemove;
            res.Ok = true;
            return res;
        }

        /// <summary>
        /// Quote delete-set refunds: item -> copies that would be returned to stock if the set is removed.
        /// </summary>
        public static DeleteSetQuote QuoteDeleteSet(WCharacter troop, int setIndex)
        {
            var q = new DeleteSetQuote { Refunds = [] };
            var preview = troop.Loadout.PreviewDeleteSet(setIndex);
            foreach (var kv in preview)
            {
                if (kv.Value.deltaRemove > 0)
                    q.Refunds[kv.Key] = kv.Value.deltaRemove;
            }
            return q;
        }

        /// <summary>
        /// Remove a set. Cancels staged ops for the set (if you maintain such a list) and refunds only
        /// the copies whose required count drops because this set disappears. No staging here.
        /// </summary>
        public static DeleteSetResult TryDeleteSet(WCharacter troop, int setIndex)
        {
            var res = new DeleteSetResult { Ok = false, Refunded = [] };

            // If you track staged ops per set, cancel them here and revert their acquisitions.
            // TroopEquipBehavior.CancelSetStages(troop, setIndex); // optional, implement if needed

            var preview = troop.Loadout.PreviewDeleteSet(setIndex);
            foreach (var kv in preview)
            {
                var item = kv.Key;
                int deltaRemove = kv.Value.deltaRemove;
                if (deltaRemove > 0)
                {
                    for (int i = 0; i < deltaRemove; i++)
                        item.Stock();
                    res.Refunded[item] = deltaRemove;
                }
            }

            // Structure removal
            var eq = troop.Loadout.Get(setIndex);
            troop.Loadout.Remove(eq);

            res.Ok = true;
            return res;
        }

        /// <summary>
        /// Immediate structural application helper for staging completion callbacks.
        /// Does not perform any stock/gold work; that was already done at stage creation.
        /// </summary>
        public static void ApplyImmediate(
            WCharacter troop,
            int setIndex,
            EquipmentIndex slot,
            WItem newItem
        )
        {
            ApplyStructureWithHorseRule(troop, setIndex, slot, newItem);
        }

        /// <summary>
        /// Immediate structural application helper for staging completion callbacks,
        /// applying the "horse removal also removes harness" rule.
        /// </summary>
        private static void ApplyStructureWithHorseRule(
            WCharacter t,
            int setIndex,
            EquipmentIndex slot,
            WItem item
        )
        {
            var loadout = t.Loadout;
            if (slot == EquipmentIndex.Horse && item == null)
                loadout.Apply(setIndex, EquipmentIndex.HorseHarness, null);
            loadout.Apply(setIndex, slot, item);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Pricing / Stock Helpers                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Price for one copy of the item for this troop, after config modifiers.
        /// </summary>
        public static int GetItemCost(WItem item)
        {
            if (item == null)
                return 0;
            if (!Config.EquippingTroopsCostsGold)
                return 0;
            if (ClanScreen.IsStudioMode)
                return 0;
            int baseValue = item.Value;
            return (int)(baseValue * Config.EquipmentCostMultiplier);
        }

        /// <summary>
        /// Roll back the stock/gold side effects of a staged equip change
        /// without touching the troop loadout. Used when the player cancels
        /// a pending change in the editor.
        /// </summary>
        public static void RollbackStagedEquip(
            WCharacter troop,
            int setIndex,
            EquipmentIndex slot,
            WItem stagedItem
        )
        {
            if (troop == null)
                return;

            if (stagedItem == null)
                return;

            var loadout = troop.Loadout;
            var eq = loadout.Get(setIndex);
            var oldItem = eq?.Get(slot);

            // Recompute the same quote used when staging.
            var q = QuoteEquip(troop, setIndex, slot, stagedItem);
            if (!q.IsChange)
                return;

            // 1) Give back all copies that were acquired when we staged.
            //    - Copies that came from stock: this reverses the Unstock() calls.
            //    - Copies that were bought: these now end up as extra stock
            if (q.DeltaAdd > 0)
            {
                for (int i = 0; i < q.DeltaAdd; i++)
                    stagedItem.Stock();
            }

            // 2) Remove the early refund of freed old-item copies.
            if (q.DeltaRemove > 0 && oldItem != null)
            {
                for (int i = 0; i < q.DeltaRemove; i++)
                    oldItem.Unstock();
            }

            EquipStagingBehavior.Unstage(troop, slot, setIndex);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Copy / Paste                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class PasteResult
        {
            public bool Ok;
            public EquipFailReason Reason;
        }

        public static PasteResult TryPasteEquipment(WEquipment source, WEquipment target)
        {
            var res = new PasteResult { Ok = false, Reason = EquipFailReason.None };
            if (source == null || target == null)
                return res;

            var troop = target.Loadout.Troop;
            if (troop == null)
                return res;

            bool studio = ClanScreen.IsStudioMode;

            // Precompute cost
            int totalCost = QuotePasteGoldCost(source, target);

            if (
                !studio
                && Config.EquippingTroopsCostsGold
                && totalCost > 0
                && Player.Gold < totalCost
            )
            {
                res.Reason = EquipFailReason.NotEnoughGold;
                return res;
            }

            // Deduct global gold once
            if (!studio && Config.EquippingTroopsCostsGold && totalCost > 0)
                Player.ChangeGold(-totalCost);

            // Apply slot-by-slot (same as before)
            foreach (EquipmentIndex slot in WEquipment.Slots)
            {
                var srcItem = source.Get(slot);
                var tgtItem = target.Get(slot);
                if (srcItem == tgtItem)
                    continue;
                if (!CanEquip(troop, srcItem))
                {
                    res.Reason = EquipFailReason.NotAllowed;
                    return res;
                }

                if (target.IsCivilian)
                {
                    if (srcItem?.IsCivilian == false)
                    {
                        res.Reason = EquipFailReason.NotCivilian;
                        return res;
                    }
                }

                var q = QuoteEquip(troop, target.Index, slot, srcItem);
                if (!q.IsChange)
                    continue;

                if (!studio && Config.EquippingTroopsCostsGold)
                {
                    for (int i = 0; i < q.CopiesFromStock; i++)
                        srcItem.Unstock();
                    for (int i = 0; i < q.CopiesToBuy; i++)
                    {
                        srcItem.Stock();
                        srcItem.Unstock();
                    }
                }

                if (
                    q.DeltaRemove > 0
                    && tgtItem != null
                    && !studio
                    && Config.EquippingTroopsCostsGold
                )
                {
                    for (int i = 0; i < q.DeltaRemove; i++)
                        tgtItem.Stock();
                }

                if (q.WouldStage && !studio)
                    EquipStagingBehavior.Stage(troop, slot, srcItem, target.Index);
                else
                    ApplyStructureWithHorseRule(troop, target.Index, slot, srcItem);
            }

            res.Ok = true;
            return res;
        }

        /// <summary>
        /// Non-mutating preview: returns the total gold cost required to paste.
        /// </summary>
        public static int QuotePasteGoldCost(WEquipment source, WEquipment target)
        {
            if (source == null || target == null)
                return 0;

            var troop = target.Loadout.Troop;
            if (troop == null)
                return 0;

            bool studio = ClanScreen.IsStudioMode;
            if (studio || !Config.EquippingTroopsCostsGold)
                return 0;

            int total = 0;
            foreach (EquipmentIndex slot in WEquipment.Slots)
            {
                var srcItem = source.Get(slot);
                var tgtItem = target.Get(slot);
                if (srcItem == tgtItem)
                    continue;

                if (!CanEquip(troop, srcItem))
                    continue;

                var q = QuoteEquip(troop, target.Index, slot, srcItem);
                if (!q.IsChange)
                    continue;

                total += q.GoldCost;
            }
            return total;
        }
    }
}
