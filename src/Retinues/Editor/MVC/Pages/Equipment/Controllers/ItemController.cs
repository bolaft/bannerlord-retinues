using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Helpers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Pages.Equipment.Services;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Interface.Services;
using TaleWorlds.Core;

namespace Retinues.Editor.MVC.Pages.Equipment.Controllers
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
        /// Runs the given action while tracking roster requirement removals and converting them into stock increases.
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
        /// </summary>
        public static void StockItems(Dictionary<string, int> requiredCountsById)
        {
            if (!EconomyEnabled)
                return;

            StocksHelper.StockItems(requiredCountsById);
        }

        /// <summary>
        /// Stocks all items required by the given character's equipment roster.
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

        // Keep these public methods to avoid touching VMs that call ItemController.
        /// <summary>
        /// Get the equipment weight limit for the given tier.
        /// </summary>
        public static float GetEquipmentWeightLimit(int tier)
        {
            return EquipmentLimitsHelper.GetWeightLimit(
                tier,
                Settings.EquipmentWeightLimitMultiplier
            );
        }

        /// <summary>
        /// Get the equipment value limit for the given tier.
        /// </summary>
        public static int GetEquipmentValueLimit(int tier)
        {
            return EquipmentLimitsHelper.GetValueLimit(
                tier,
                Settings.EquipmentValueLimitMultiplier
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Decision                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly Dictionary<string, EquipDecision> _decisionByItemId = new();

        private static EquipmentIndex _ctxSlot = EquipmentIndex.None;
        private static EditorMode _ctxMode;
        private static bool _ctxPreview;
        private static object _ctxCharacterRef;
        private static object _ctxEquipmentRef;
        private static int _ctxBurstId;

        // Use State.Slot (avoids EditorState.Instance spam).
        private static EquipDecision DecisionForSlot(WItem item)
        {
            if (item == null)
                return default;

            var slot = State.Slot;
            var mode = State.Mode;
            var preview = PreviewController.Enabled;
            var characterRef = (object)State.Character?.Base;
            var equipmentRef = (object)State.Equipment?.Base;

            int burstId = EventManager.CurrentBurstId;

            if (
                slot != _ctxSlot
                || mode != _ctxMode
                || preview != _ctxPreview
                || !ReferenceEquals(characterRef, _ctxCharacterRef)
                || !ReferenceEquals(equipmentRef, _ctxEquipmentRef)
                || burstId != _ctxBurstId
            )
            {
                _decisionByItemId.Clear();

                _ctxSlot = slot;
                _ctxMode = mode;
                _ctxPreview = preview;
                _ctxCharacterRef = characterRef;
                _ctxEquipmentRef = equipmentRef;
                _ctxBurstId = burstId;
            }

            var id = item.StringId ?? string.Empty;
            if (_decisionByItemId.TryGetValue(id, out var cached))
                return cached;

            var ctx = new EquipContext(mode, preview, State.Character, State.Equipment);
            var decision = EquipRules.CanSetItem(ctx, PreviewController.GetItem, slot, item);

            _decisionByItemId[id] = decision;
            return decision;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Equip                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Equips the given item to the selected slot, handling economy and preview.
        /// </summary>
        public static ControllerAction<WItem> Equip { get; } =
            Action<WItem>("EquipItem")
                .RequireValidEditingContext()
                .AddCondition(
                    item => item != null,
                    _ => L.T("cant_equip_reason_null", "Invalid item")
                )
                .AddCondition(
                    item => DecisionForSlot(item).Allowed,
                    item =>
                        DecisionForSlot(item).Tooltip
                        ?? L.T("cant_equip_reason_other", "Cannot equip")
                )
                .ExecuteWith(EquipItem);

        /// <summary>
        /// Equip the given item, handling preview, economy, and roster stock.
        /// </summary>
        private static void EquipItem(WItem item)
        {
            if (State.Equipment == null)
                return;

            var slot = EditorState.Instance.Slot;
            var equipped = PreviewController.GetItem(slot);

            if (equipped == item)
                return;

            // Safety: if something bypasses conditions, still enforce the same rules.
            var decision = DecisionForSlot(item);
            if (!decision.Allowed)
                return;

            if (PreviewController.Enabled)
            {
                PreviewController.SetItem(slot, item);
                return;
            }

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

            EquipItemCore(slot, item);
            EventManager.Fire(UIEvent.Item);
        }

        /// <summary>
        /// Core equip operation that sets the item on the equipment and handles harness compatibility.
        /// </summary>
        private static void EquipItemCore(EquipmentIndex slot, WItem item)
        {
            if (State.Equipment == null)
                return;

            State.Equipment.Set(slot, item);

            if (slot == EquipmentIndex.Horse)
            {
                var harness = State.Equipment.Get(EquipmentIndex.HorseHarness);

                if (item == null)
                {
                    State.Equipment.Set(EquipmentIndex.HorseHarness, null);
                    return;
                }

                if (harness != null && !item.IsCompatibleWith(harness))
                    State.Equipment.Set(EquipmentIndex.HorseHarness, null);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Purchasing                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Determine purchase cost for equipping the item, if any.
        /// </summary>
        private static bool TryGetPurchaseCost(EquipmentIndex slot, WItem item, out int cost)
        {
            cost = 0;

            if (!EconomyEnabled)
                return false;

            if (item == null)
                return false;

            if (State.Equipment == null)
                return false;

            if (State.Equipment.IsAvailableInRoster(slot, item))
                return false;

            if (item.Stock > 0)
                return false;

            cost = EquipEconomy.ComputeEquipCost(item);
            return cost > 0;
        }

        /// <summary>
        /// Show a purchase confirmation popup and handle the purchase/equip flow.
        /// </summary>
        private static void ShowPurchaseInquiry(EquipmentIndex slot, WItem item, int cost)
        {
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
                    if (
                        State.Equipment == null
                        || !ReferenceEquals(State.Equipment.Base, equipmentRef)
                    )
                        return;

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

        /// <summary>
        /// Unequips the item at the given slot, handling economy and preview.
        /// </summary>
        public static ControllerAction<EquipmentIndex> Unequip { get; } =
            Action<EquipmentIndex>("UnequipItem")
                .RequireValidEditingContext()
                .AddCondition(
                    slot => State.Equipment != null && PreviewController.GetItem(slot) != null,
                    L.T("cant_unequip_reason_empty", "Nothing to unequip")
                )
                .DefaultTooltip(value =>
                    State.Equipment.IsStaged(value)
                        ? L.T("unstage_item_tooltip", "Unstage")
                        : L.T("unequip_item_tooltip", "Unequip")
                )
                .ExecuteWith(UnequipItem)
                .Fire(UIEvent.Item);

        /// <summary>
        /// Unequip the item at the given slot, handling economy and preview.
        /// </summary>
        private static void UnequipItem(EquipmentIndex slot)
        {
            if (State.Equipment == null)
                return;

            var equipped = PreviewController.GetItem(slot);
            if (equipped == null)
                return;

            if (PreviewController.Enabled)
            {
                PreviewController.ClearItem(slot);
                return;
            }

            void Apply()
            {
                State.Equipment.Set(slot, null);

                if (slot == EquipmentIndex.Horse)
                {
                    State.Equipment.Set(EquipmentIndex.HorseHarness, null);
                    if (State.Slot == EquipmentIndex.HorseHarness)
                        State.Slot = EquipmentIndex.Horse; // Switch away from now-invalid harness slot.
                }
            }

            if (EconomyEnabled)
                TrackRosterStock(Apply);
            else
                Apply();

            EventManager.Fire(UIEvent.Item);
        }
    }
}
