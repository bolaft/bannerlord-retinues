using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Helpers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.Services;
using Retinues.Game;
using Retinues.UI.Services;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.Editor.Controllers.Equipment
{
    public class ItemController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Economy                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool EconomyEnabled =>
            !PreviewController.Enabled
            && State.Mode == EditorMode.Player
            && Settings.EquipmentCostsMoney;

        /// <summary>
        /// Indicates whether the editor economy is currently active.
        /// </summary>
        public static bool EconomyActive => EconomyEnabled;

        /// <summary>
        /// Runs the given action while tracking roster requirement removals and
        /// converting them into stock increases.
        /// </summary>
        public static void TrackRosterStock(Action action)
        {
            if (!EconomyEnabled)
            {
                action?.Invoke();
                return;
            }

            StocksHelper.TrackRosterStock(State.Character?.EquipmentRoster, action);
        }

        /// <summary>
        /// Stocks all items described by the given required-count map.
        /// Intended for destructive operations (eg deleting a unit) where the
        /// roster requirement becomes zero.
        /// </summary>
        public static void StockItems(Dictionary<string, int> requiredCountsById)
        {
            if (!EconomyEnabled)
                return;

            StocksHelper.StockItems(requiredCountsById);
        }

        /// <summary>
        /// Stocks all items required by the given character's equipment roster.
        /// Uses the roster's conceptual requirement count (max-per-equipment).
        /// </summary>
        public static void StockCharacterRoster(WCharacter character)
        {
            if (character == null)
                return;

            if (!EconomyEnabled)
                return;

            StocksHelper.StockCharacterRoster(character);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Limits                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool LimitsEnabled => State.Mode == EditorMode.Player;

        private static bool WeightLimitActive => LimitsEnabled && Settings.EquipmentWeightLimit;

        private static bool ValueLimitActive => LimitsEnabled && Settings.EquipmentValueLimit;

        // Keep these public methods to avoid touching VMs that call ItemController.
        public static float GetEquipmentWeightLimit(int tier)
        {
            return EquipmentLimitsHelper.GetWeightLimit(
                tier,
                Settings.EquipmentWeightLimitMultiplier
            );
        }

        public static int GetEquipmentValueLimit(int tier)
        {
            return EquipmentLimitsHelper.GetValueLimit(
                tier,
                Settings.EquipmentValueLimitMultiplier
            );
        }

        private static bool WithinWeightLimit(WItem item)
        {
            if (!WeightLimitActive)
                return true;

            if (State.Equipment == null)
                return true;

            var slot = EditorState.Instance.Slot;

            var currentItem = PreviewController.GetItem(slot);

            var current = EquipmentLimitsHelper.GetTotals(
                idx => PreviewController.GetItem(idx),
                slot,
                currentItem
            );

            var next = EquipmentLimitsHelper.GetTotals(
                idx => PreviewController.GetItem(idx),
                slot,
                item
            );

            int tier = State.Character?.Tier ?? 0;
            float limit = GetEquipmentWeightLimit(tier);

            return EquipmentLimitsHelper.FitsWeight(
                current,
                next,
                limit,
                allowNonIncreasingWhenOver: true
            );
        }

        private static TextObject WeightLimitTooltip(WItem item)
        {
            var slot = EditorState.Instance.Slot;

            var next = EquipmentLimitsHelper.GetTotals(
                idx => PreviewController.GetItem(idx),
                slot,
                item
            );

            int tier = State.Character?.Tier ?? 0;
            float limit = GetEquipmentWeightLimit(tier);

            return L.T("cant_equip_reason_weight_limit", "Too heavy")
                .SetTextVariable("WEIGHT", next.Weight.ToString("0.0"))
                .SetTextVariable("LIMIT", limit.ToString("0.0"));
        }

        private static bool WithinValueLimit(WItem item)
        {
            if (!ValueLimitActive)
                return true;

            if (State.Equipment == null)
                return true;

            var slot = EditorState.Instance.Slot;

            var currentItem = PreviewController.GetItem(slot);

            var current = EquipmentLimitsHelper.GetTotals(
                idx => PreviewController.GetItem(idx),
                slot,
                currentItem
            );

            var next = EquipmentLimitsHelper.GetTotals(
                idx => PreviewController.GetItem(idx),
                slot,
                item
            );

            int tier = State.Character?.Tier ?? 0;
            int limit = GetEquipmentValueLimit(tier);

            return EquipmentLimitsHelper.FitsValue(
                current,
                next,
                limit,
                allowNonIncreasingWhenOver: true
            );
        }

        private static TextObject ValueLimitTooltip(WItem item)
        {
            var slot = EditorState.Instance.Slot;

            var next = EquipmentLimitsHelper.GetTotals(
                idx => PreviewController.GetItem(idx),
                slot,
                item
            );

            int tier = State.Character?.Tier ?? 0;
            int limit = GetEquipmentValueLimit(tier);

            return L.T("cant_equip_reason_value_limit", "Too valuable")
                .SetTextVariable("VALUE", next.Value)
                .SetTextVariable("LIMIT", limit);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Equip                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<WItem> Equip { get; } =
            Action<WItem>("EquipItem")
                .RequireValidEditingContext()
                .AddCondition(
                    item => item != null,
                    _ => L.T("cant_equip_reason_null", "Invalid item")
                )
                .AddCondition(UnlockAllowed, UnlockTooltip)
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
                .AddCondition(WithinWeightLimit, WeightLimitTooltip)
                .AddCondition(WithinValueLimit, ValueLimitTooltip)
                .ExecuteWith(EquipItem);

        private static bool IsCompatibleWithCurrentEquipment(WItem item)
        {
            if (State.Equipment == null)
                return true;

            var slot = EditorState.Instance.Slot;

            if (slot == EquipmentIndex.HorseHarness)
            {
                var horse = PreviewController.GetItem(EquipmentIndex.Horse);
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
            var equipped = PreviewController.GetItem(slot);

            if (equipped == item)
                return;

            // Safety: if something bypasses conditions, still prevent incompatible harness equip.
            if (slot == EquipmentIndex.HorseHarness)
            {
                var horse = State.Equipment.Get(EquipmentIndex.Horse);
                if (horse != null && item != null && !horse.IsCompatibleWith(item))
                    return;
            }

            if (PreviewController.Enabled)
            {
                // Safety: still prevent incompatible harness equip.
                if (slot == EquipmentIndex.HorseHarness)
                {
                    var horse = PreviewController.GetItem(EquipmentIndex.Horse);
                    if (horse != null && item != null && !horse.IsCompatibleWith(item))
                        return;
                }

                PreviewController.SetItem(slot, item);
                return;
            }

            // Economy rules only apply in Player mode and when the setting is enabled.
            if (EconomyEnabled)
            {
                if (
                    !State.Equipment.IsAvailableInRoster(slot, item)
                    && item != null
                    && item.Stock > 0
                )
                {
                    item.DecreaseStock(1);
                    TrackRosterStock(() => EquipItemCore(slot, item));
                    EventManager.Fire(UIEvent.Item);
                    return;
                }

                if (TryGetPurchaseCost(slot, item, out int cost))
                {
                    ShowPurchaseInquiry(slot, item, cost);
                    return;
                }

                TrackRosterStock(() => EquipItemCore(slot, item));
                EventManager.Fire(UIEvent.Item);
                return;
            }

            // Default: equip directly. When economy is enabled, still track roster removals -> stock.
            if (EconomyEnabled)
                TrackRosterStock(() => EquipItemCore(slot, item));
            else
                EquipItemCore(slot, item);

            EventManager.Fire(UIEvent.Item);
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

        private static bool UnlockAllowed(WItem item)
        {
            if (item == null)
                return true;

            // Preview mode should ignore unlock restrictions.
            if (PreviewController.Enabled)
                return true;

            // Only enforce unlock gating in Player mode.
            if (State.Mode != EditorMode.Player)
                return true;

            // Crafted items are handled by the crafted filter; don't gate them here.
            if (item.IsCrafted)
                return true;

            // Fully unlocked is fine.
            if (item.IsUnlocked)
                return true;

            // Locked (0%) or partially unlocking (1-99%) => not equipable (disabled in list).
            return false;
        }

        private static TextObject UnlockTooltip(WItem item)
        {
            if (item == null)
                return L.T("cant_equip_reason_null", "Invalid item");

            // Be defensive about types/ranges. Compute percent as proportion
            // of UnlockProgress to UnlockThreshold (as percentage).
            double progress = Convert.ToDouble(item.UnlockProgress);
            double percentDouble = progress / WItem.UnlockThreshold * 100.0;
            int percent = (int)Math.Round(percentDouble, MidpointRounding.AwayFromZero);

            if (percent < 0)
                percent = 0;
            if (percent > 100)
                percent = 100;

            if (percent > 0)
            {
                return L.T("cant_equip_reason_unlocking", "Unlocking {PERCENT}%")
                    .SetTextVariable("PERCENT", percent);
            }

            // Fully locked items are normally filtered out by the list builder,
            // but keep a sane reason in case something tries to equip them anyway.
            return L.T("cant_equip_reason_locked", "Locked");
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
                    title: L.T("cant_afford_title", "Not Enough Money"),
                    description: L.T(
                            "cant_afford_desc",
                            "You need {COST} denars to buy {ITEM}, but you only have {GOLD}."
                        )
                        .SetTextVariable("COST", required)
                        .SetTextVariable("ITEM", item?.Name)
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
                    .SetTextVariable("ITEM", item?.Name)
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
                        && item != null
                        && !State.Equipment.IsAvailableInRoster(slot, item)
                        && item.Stock > 0
                    )
                    {
                        item.DecreaseStock(1);
                        TrackRosterStock(() => EquipItemCore(slot, item));
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

                    TrackRosterStock(() => EquipItemCore(slot, item));
                    EventManager.Fire(UIEvent.Item);
                }
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Unequip                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<EquipmentIndex> Unequip { get; } =
            Action<EquipmentIndex>("UnequipItem")
                .RequireValidEditingContext()
                .AddCondition(
                    slot => State.Equipment != null && PreviewController.GetItem(slot) != null,
                    L.T("cant_unequip_reason_empty", "Nothing to unequip.")
                )
                .DefaultTooltip(value =>
                    State.Equipment.IsStaged(value)
                        ? L.T("unstage_item_tooltip", "Unstage")
                        : L.T("unequip_item_tooltip", "Unequip")
                )
                .ExecuteWith(UnequipItem)
                .Fire(UIEvent.Item);

        private static void UnequipItem(EquipmentIndex slot)
        {
            if (State.Equipment == null)
                return;

            var equipped = PreviewController.GetItem(slot);
            if (equipped == null)
                return;

            if (PreviewController.Enabled)
            {
                // PreviewController.ClearItem already fires UIEvent.Item
                PreviewController.ClearItem(slot);
                return;
            }

            void Apply()
            {
                State.Equipment.Set(slot, null);

                if (slot == EquipmentIndex.Horse)
                    State.Equipment.Set(EquipmentIndex.HorseHarness, null);
            }

            if (EconomyEnabled)
                TrackRosterStock(Apply);
            else
                Apply();

            EventManager.Fire(UIEvent.Item);
        }
    }
}
