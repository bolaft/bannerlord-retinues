using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Editor.Events;
using Retinues.Game;
using Retinues.UI.Services;
using TaleWorlds.Core;

namespace Retinues.Editor.Controllers.Equipment
{
    public class ItemController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Economy                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool EconomyEnabled =>
            State.Mode == EditorMode.Player && Settings.EquipmentCostsGold.Value;

        private static Dictionary<string, WItem> _itemById;

        private static WItem GetItemById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            _itemById ??= BuildItemByIdCache();

            if (_itemById.TryGetValue(id, out var item))
                return item;

            // Fallback: crafted / uncommon items may not be in Equipments.
            foreach (var it in WItem.All)
            {
                if (it != null && it.StringId == id)
                {
                    _itemById[id] = it;
                    return it;
                }
            }

            return null;
        }

        private static Dictionary<string, WItem> BuildItemByIdCache()
        {
            Dictionary<string, WItem> map = [];

            foreach (var item in WItem.Equipments)
            {
                var id = item?.StringId;
                if (string.IsNullOrEmpty(id))
                    continue;

                if (!map.ContainsKey(id))
                    map[id] = item;
            }

            return map;
        }

        private static Dictionary<string, int> ComputeRosterRequiredCounts()
        {
            var character = State.Character;
            if (character == null)
                return [];

            var roster = character.EquipmentRoster;
            var equipments = roster?.Equipments ?? [];

            Dictionary<string, int> maxById = [];

            foreach (var equipment in equipments)
            {
                if (equipment == null)
                    continue;

                Dictionary<string, int> counts = [];

                foreach (var item in equipment.Items)
                {
                    var id = item?.StringId;
                    if (string.IsNullOrEmpty(id))
                        continue;

                    counts[id] = counts.TryGetValue(id, out var n) ? n + 1 : 1;
                }

                foreach (var kv in counts)
                {
                    if (!maxById.TryGetValue(kv.Key, out var prev) || kv.Value > prev)
                        maxById[kv.Key] = kv.Value;
                }
            }

            return maxById;
        }

        /// <summary>
        /// Applies stock changes based on the roster requirement delta.
        /// Only active when economy is enabled.
        /// </summary>
        private static void WithRosterStockTracking(Action action)
        {
            if (!EconomyEnabled)
            {
                action?.Invoke();
                return;
            }

            var before = ComputeRosterRequiredCounts();

            action?.Invoke();

            var after = ComputeRosterRequiredCounts();
            ApplyRosterRemovalsToStock(before, after);
        }

        private static void ApplyRosterRemovalsToStock(
            Dictionary<string, int> before,
            Dictionary<string, int> after
        )
        {
            if (before == null || before.Count == 0)
                return;

            after ??= [];

            foreach (var kv in before)
            {
                string id = kv.Key;
                int oldRequired = kv.Value;
                int newRequired = after.TryGetValue(id, out var n) ? n : 0;

                int removed = oldRequired - newRequired;
                if (removed <= 0)
                    continue;

                var item = GetItemById(id);
                if (item == null)
                    continue;

                item.IncreaseStock(removed);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Equip                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<WItem> Equip { get; } =
            Action<WItem>("EquipItem")
                .AddCondition(
                    _ => State.Equipment != null,
                    L.T("cant_equip_reason_no_equipment", "No equipment set")
                )
                .AddCondition(
                    item => item != null,
                    _ => L.T("cant_equip_reason_null", "Invalid item")
                )
                .AddCondition(
                    item => item.IsEquippableByCharacter(State.Character),
                    item =>
                        L.T("cant_equip_reason_skill", "{SKILL} {VALUE}")
                            .SetTextVariable("VALUE", item.Difficulty)
                            .SetTextVariable("SKILL", item.RelevantSkill?.Name)
                )
                .AddCondition(
                    item => State.Equipment?.IsCivilian == false || item.IsCivilian,
                    _ => L.T("cant_equip_reason_civilian", "Not civilian")
                )
                .AddCondition(
                    IsCompatibleWithCurrentEquipment,
                    _ => L.T("cant_equip_reason_mount_compat", "Incompatible")
                )
                .ExecuteWith(EquipItem)
                .Fire(UIEvent.Item);

        private static bool IsCompatibleWithCurrentEquipment(WItem item)
        {
            if (State.Equipment == null)
                return true;

            var slot = EditorState.Instance.Slot;

            // Only enforce when equipping a harness onto an already-equipped horse.
            // (Equipping a horse will auto-clear an incompatible harness instead.)
            if (slot == EquipmentIndex.HorseHarness)
            {
                var horse = State.Equipment.Get(EquipmentIndex.Horse);
                if (horse == null)
                    return true;

                return horse.IsCompatibleWith(item);
            }

            return true;
        }

        private static void EquipItem(WItem item)
        {
            if (State.Equipment == null)
                return;

            var slot = EditorState.Instance.Slot;
            var equipped = State.Equipment.Get(slot);

            if (equipped == item)
                return;

            // Safety: if something bypasses conditions, still prevent incompatible harness equip.
            if (slot == EquipmentIndex.HorseHarness)
            {
                var horse = State.Equipment.Get(EquipmentIndex.Horse);
                if (horse != null && !horse.IsCompatibleWith(item))
                    return;
            }

            // Economy rules only apply in Player mode and when the setting is enabled.
            if (EconomyEnabled)
            {
                // If equipping this item requires increasing the roster requirement,
                // try consuming stock first (no popup, no gold).
                if (!State.Equipment.IsAvailableInRoster(slot, item) && item.Stock > 0)
                {
                    item.DecreaseStock(1);
                    WithRosterStockTracking(() => EquipItemCore(slot, item));
                    return;
                }

                // If stock is empty and a new copy is required, purchase flow.
                if (TryGetPurchaseCost(slot, item, out int cost))
                {
                    ShowPurchaseInquiry(slot, item, cost);
                    return;
                }
            }

            // Default: equip directly. When economy is enabled, still track roster removals -> stock.
            if (EconomyEnabled)
                WithRosterStockTracking(() => EquipItemCore(slot, item));
            else
                EquipItemCore(slot, item);
        }

        private static void EquipItemCore(EquipmentIndex slot, WItem item)
        {
            if (State.Equipment == null)
                return;

            State.Equipment.Set(slot, item);

            // If a new horse makes the currently equipped harness incompatible, clear the harness.
            if (slot == EquipmentIndex.Horse)
            {
                var harness = State.Equipment.Get(EquipmentIndex.HorseHarness);
                if (harness != null && item != null && !item.IsCompatibleWith(harness))
                    State.Equipment.Set(EquipmentIndex.HorseHarness, null);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Purchasing                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool TryGetPurchaseCost(EquipmentIndex slot, WItem item, out int cost)
        {
            cost = 0;

            if (!EconomyEnabled)
                return false;

            if (item == null)
                return false;

            if (State.Equipment == null)
                return false;

            // If the roster already has enough copies (slot-aware), no purchase needed.
            if (State.Equipment.IsAvailableInRoster(slot, item))
                return false;

            // Stock handled by EquipItem(). If we still have stock, treat as free.
            if (item.Stock > 0)
                return false;

            cost = ComputeEquipCost(item);
            return cost > 0;
        }

        private static int ComputeEquipCost(WItem item)
        {
            if (item == null)
                return 0;

            double multiplier = Settings.EquipmentCostMultiplier.Value;
            double raw = item.Value * multiplier;

            int cost = (int)Math.Round(raw, MidpointRounding.AwayFromZero);
            return Math.Max(cost, 0);
        }

        private static void ShowPurchaseInquiry(EquipmentIndex slot, WItem item, int cost)
        {
            // Snapshot to avoid equipping into the wrong set if selection changes mid-popup.
            var equipmentRef = State.Equipment?.Base;

            void NotEnoughGoldPopup(int required)
            {
                Inquiries.Popup(
                    title: L.T("cant_afford_title", "Not Enough Gold"),
                    description: L.T(
                            "cant_afford_desc",
                            "You need {COST} denars to buy {ITEM}, but you only have {GOLD}."
                        )
                        .SetTextVariable("COST", required)
                        .SetTextVariable("ITEM", item.Name)
                        .SetTextVariable("GOLD", Player.Gold)
                );
            }

            if (Player.Gold < cost)
            {
                NotEnoughGoldPopup(cost);
                return;
            }

            Inquiries.Popup(
                title: L.T("purchase_item_title", "Purchase Item"),
                description: L.T("purchase_item_desc", "Buy {ITEM} for {COST} denars?")
                    .SetTextVariable("ITEM", item.Name)
                    .SetTextVariable("COST", cost),
                onConfirm: () =>
                {
                    // Selection changed? Abort silently.
                    if (
                        State.Equipment == null
                        || !ReferenceEquals(State.Equipment.Base, equipmentRef)
                    )
                        return;

                    // If roster requirement changed, stock might now be available.
                    if (
                        EconomyEnabled
                        && !State.Equipment.IsAvailableInRoster(slot, item)
                        && item.Stock > 0
                    )
                    {
                        item.DecreaseStock(1);
                        WithRosterStockTracking(() => EquipItemCore(slot, item));
                        EventManager.Fire(UIEvent.Item);
                        return;
                    }

                    // Recompute purchase requirement and final cost.
                    if (TryGetPurchaseCost(slot, item, out int newCost))
                    {
                        if (Player.Gold < newCost)
                        {
                            NotEnoughGoldPopup(newCost);
                            return;
                        }

                        if (!Player.TrySpendGold(newCost))
                        {
                            NotEnoughGoldPopup(newCost);
                            return;
                        }
                    }

                    WithRosterStockTracking(() => EquipItemCore(slot, item));
                    EventManager.Fire(UIEvent.Item);
                }
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Unequip                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<EquipmentIndex> Unequip { get; } =
            Action<EquipmentIndex>("UnequipItem")
                .AddCondition(
                    slot => State.Equipment != null && State.Equipment.Get(slot) != null,
                    L.T("cant_unequip_reason_empty", "Nothing to unequip.")
                )
                .DefaultTooltip(L.T("unequip_item_tooltip", "Unequip this item."))
                .ExecuteWith(UnequipItem)
                .Fire(UIEvent.Item);

        private static void UnequipItem(EquipmentIndex slot)
        {
            if (State.Equipment == null)
                return;

            var equipped = State.Equipment.Get(slot);
            if (equipped == null)
                return;

            void Apply()
            {
                State.Equipment.Set(slot, null);

                if (slot == EquipmentIndex.Horse)
                {
                    // No behavior change: unequipping a horse also clears harness.
                    State.Equipment.Set(EquipmentIndex.HorseHarness, null);
                }
            }

            if (EconomyEnabled)
                WithRosterStockTracking(Apply);
            else
                Apply();
        }
    }
}
