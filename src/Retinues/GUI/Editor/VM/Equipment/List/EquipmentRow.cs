using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Helpers;
using Retinues.Managers;
using Retinues.Utils;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
# if BL13
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
# endif

namespace Retinues.GUI.Editor.VM.Equipment.List
{
    /// <summary>
    /// ViewModel for a single equipment list row, handling selection and availability.
    /// </summary>
    [SafeClass]
    public sealed class EquipmentRowVM(
        WItem rowItem,
        bool isAvailable,
        bool isUnlocked,
        int progress
    ) : BaseListElementVM(autoRegister: false)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public readonly WItem RowItem = rowItem;
        public readonly bool IsAvailable = isAvailable;
        public readonly bool IsUnlocked = isUnlocked;
        public readonly int Progress = progress;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override Dictionary<UIEvent, string[]> EventMap =>
            new()
            {
                [UIEvent.Slot] =
                [
                    nameof(IsEnabled),
                    nameof(IsSelected),
                    nameof(Value),
                    nameof(Stock),
                    nameof(Name),
                    nameof(InStockText),
                    nameof(IsDisabledText),
                    nameof(ShowIsEquipped),
                    nameof(ShowInStockText),
                    nameof(ShowValue),
                    nameof(ImageId),
                    nameof(BannerId),
                    nameof(ImageAdditionalArgs),
                    nameof(BannerAdditionalArgs),
#if BL13
                    nameof(ImageTextureProviderName),
                    nameof(BannerTextureProviderName),
#else
                    nameof(ImageTypeCode),
                    nameof(BannerTypeCode),
#endif
                    nameof(Hint),
                    nameof(AvailableFromAnotherSet),
                ],
                [UIEvent.Equip] =
                [
                    nameof(IsSelected),
                    nameof(Stock),
                    nameof(InStockText),
                    nameof(ShowInStockText),
                    nameof(ShowValue),
                    nameof(ShowIsEquipped),
                    nameof(IsDisabledText),
                    nameof(AvailableFromAnotherSet),
                ],
                [UIEvent.Equipment] =
                [
                    nameof(IsEnabled),
                    nameof(IsDisabledText),
                    nameof(ShowInStockText),
                    nameof(ShowIsEquipped),
                    nameof(ShowValue),
                    nameof(IsSelected),
                    nameof(AvailableFromAnotherSet),
                ],
            };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WItem Item => StagedItem ?? EquippedItem;

        private WItem EquippedItem => State.Equipment?.Get(State.Slot);

        private WItem StagedItem =>
            State.EquipData?.TryGetValue(State.Slot, out var equipData) == true
                ? equipData.Equip != null
                    ? new WItem(equipData.Equip.ItemId)
                    : null
                : null;

        private bool IsEquipped => !IsEmptyRow && EquippedItem == RowItem;
        private bool IsEmptyRow => RowItem == null;
        private bool IsRequirementBlocked =>
            !IsEmptyRow && State.Troop?.MeetsItemSkillRequirements(RowItem) == false;
        private bool IsTierBlocked =>
            !IsEmptyRow
            && !DoctrineAPI.IsDoctrineUnlocked<Ironclad>()
            && (RowItem.Tier - (State.Troop?.Tier ?? 0)) > Config.AllowedTierDifference;
        private bool IsEquipmentTypeBlocked =>
            !IsEmptyRow && State.Equipment?.IsCivilian == true && RowItem?.IsCivilian == false;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ Values ━━━━━━━━ */

        [DataSourceProperty]
        public int Value => EquipmentManager.GetItemCost(RowItem, State.Troop);

        [DataSourceProperty]
        public int Stock => RowItem?.GetStock() ?? 0;

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string Name => RowItem?.Name ?? L.S("empty_item", "Empty");

        [DataSourceProperty]
        public string InStockText =>
            L.T("in_stock", "In Stock ({STOCK})").SetTextVariable("STOCK", Stock).ToString();

        [DataSourceProperty]
        public string IsDisabledText
        {
            get
            {
                if (!IsUnlocked)
                    return L.T("unlock_progress_text", "Unlocking ({PROGRESS}%)")
                        .SetTextVariable(
                            "PROGRESS",
                            (int)((float)Progress / Config.KillsForUnlock * 100)
                        )
                        .ToString();

                if (!IsAvailable)
                    return Player.CurrentSettlement == null
                        ? L.S("item_unavailable_no_settlement", "Not in town")
                        : L.T("item_unavailable_text", "Not sold in {SETTLEMENT}")
                            .SetTextVariable("SETTLEMENT", Player.CurrentSettlement?.Name)
                            .ToString();

                if (IsRequirementBlocked)
                    return L.T("skill_requirement_text", "{SKILL}: {LEVEL}")
                        .SetTextVariable("SKILL", RowItem?.RelevantSkill?.Name)
                        .SetTextVariable("LEVEL", RowItem?.Difficulty ?? 0)
                        .ToString();

                if (IsTierBlocked)
                    return L.S("item_tier_blocked_text", "Tier too high");

                if (IsEquipmentTypeBlocked)
                    return L.S("item_equipment_type_blocked_text", "Not civilian");

                return string.Empty;
            }
        }

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool ShowIsEquipped =>
            (StagedItem != null && IsEquipped)
            || (AvailableFromAnotherSet && !IsEquipmentTypeBlocked);

        [DataSourceProperty]
        public bool ShowInStockText =>
            !State.IsStudioMode
            && IsEnabled
            && !IsSelected
            && !IsEquipped
            && !AvailableFromAnotherSet
            && RowItem?.IsStocked == true;

        [DataSourceProperty]
        public bool ShowValue =>
            !State.IsStudioMode
            && IsEnabled
            && !IsSelected
            && !IsEquipped
            && !AvailableFromAnotherSet
            && !ShowInStockText
            && Value > 0;

        [DataSourceProperty]
        public override bool IsSelected => RowItem == Item;

        [DataSourceProperty]
        public override bool IsEnabled =>
            IsEmptyRow
            || (
                IsUnlocked
                && IsAvailable
                && !IsRequirementBlocked
                && !IsTierBlocked
                && !IsEquipmentTypeBlocked
            );

        [DataSourceProperty]
        public bool AvailableFromAnotherSet
        {
            get
            {
                if (RowItem == null || State.Troop == null || State.Equipment == null)
                    return false;

                var loadout = State.Troop.Loadout;
                // current set has 0 of this item…
                int inThisSet = loadout.CountInSet(RowItem, State.Equipment.Index);
                if (inThisSet > 0)
                    return false;

                // …and at least one other set has >= 1 (i.e., free to share one)
                return loadout.MaxCountPerSet(RowItem) >= 1;
            }
        }

        /* ━━━━━━━━━ Image ━━━━━━━━ */

        [DataSourceProperty]
        public string ImageId => RowItem?.Image?.Id;

        [DataSourceProperty]
        public string BannerId => RowItem?.Culture?.Image?.Id;

        [DataSourceProperty]
        public string ImageAdditionalArgs => RowItem?.Image?.AdditionalArgs;

        [DataSourceProperty]
        public string BannerAdditionalArgs => RowItem?.Culture?.Image?.AdditionalArgs;

#if BL13
        [DataSourceProperty]
        public string ImageTextureProviderName => RowItem?.Image?.TextureProviderName;

        [DataSourceProperty]
        public string BannerTextureProviderName => RowItem?.Culture?.Image?.TextureProviderName;
#else
        [DataSourceProperty]
        public int ImageTypeCode => RowItem?.Image?.ImageTypeCode ?? 0;

        [DataSourceProperty]
        public int BannerTypeCode => RowItem?.Culture?.Image?.ImageTypeCode ?? 0;
#endif

        /* ━━━━━━━━ Tooltip ━━━━━━━ */

        [DataSourceProperty]
        public CharacterEquipmentItemVM Hint
        {
            get
            {
                if (RowItem == null)
                    return null;
                var vm = new CharacterEquipmentItemVM(RowItem.Base);
                return vm;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Handle selection of this equipment row.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteSelect()
        {
            // Slot sanity
            if (RowItem != null && !RowItem.Slots.Contains(State.Slot))
                return;

            // Context restriction (only for instant mode)
            if (!Config.EquipmentChangeTakesTime)
                if (
                    !ContextManager.IsAllowedInContextWithPopup(
                        State.Troop,
                        L.S("action_modify", "modify")
                    )
                )
                    return;

            var troop = State.Troop;
            var setIndex = State.Equipment.Index;
            var slot = State.Slot;

            var equippedItem = State.Equipment.Get(slot);
            var selectionIsNull = RowItem == null;
            var selectionIsEquipped = RowItem != null && RowItem == equippedItem;

            // Studio: bypass rules/costs/time
            if (State.IsStudioMode)
            {
                var res = EquipmentManager.TryEquip(
                    troop,
                    setIndex,
                    slot,
                    RowItem,
                    allowPurchase: false
                );
                // (res.Staged will be false in studio, by design)
                State.UpdateEquipData();
                return;
            }

            // Case 1: Unequip
            if (selectionIsNull)
            {
                // Only warn if reverting would later take time.
                // That’s exactly when this unequip reduces required copies (deltaRemove > 0).
                if (!State.IsStudioMode && Config.EquipmentChangeTakesTime && equippedItem != null)
                {
                    var loadout = troop.Loadout;
                    int beforeOld = loadout.MaxCountPerSet(equippedItem);
                    int afterOld = loadout.RequiredAfterForItem(equippedItem, setIndex, slot, null);
                    bool revertWouldStage = afterOld < beforeOld; // deltaRemove > 0

                    if (revertWouldStage)
                    {
                        InformationManager.ShowInquiry(
                            new InquiryData(
                                L.S("unequip_warn_title", "Unequip Item"),
                                L.T(
                                        "unequip_warning_text",
                                        "Unequipping is instant.\n\nRe-equipping {ITEM_NAME} later may take time."
                                    )
                                    .SetTextVariable("ITEM_NAME", equippedItem.Name)
                                    .ToString(),
                                true,
                                true,
                                L.S("confirm", "Confirm"),
                                L.S("cancel", "Cancel"),
                                () =>
                                {
                                    var res = EquipmentManager.TryUnequip(troop, setIndex, slot);
                                    State.UpdateEquipData();
                                },
                                () => { }
                            )
                        );
                        return;
                    }
                }

                // No warning needed: either studio, equip-changes don’t take time,
                // or this unequip does not reduce required copies.
                {
                    var res = EquipmentManager.TryUnequip(troop, setIndex, slot);
                    State.UpdateEquipData();
                    return;
                }
            }

            // Case 2: Already equipped -> no-op (but still refresh to collapse any staged visuals)
            if (selectionIsEquipped)
            {
                State.UpdateEquipData();
                return;
            }

            // Case 3: Equip a different item
            // Preview the change to drive UI flow (confirm cost if needed).
            var quote = EquipmentManager.QuoteEquip(troop, setIndex, slot, RowItem);

            // No structural change (defensive)
            if (!quote.IsChange)
            {
                State.UpdateEquipData();
                return;
            }

            // If we must buy copies (and player pays for equipment), ask for confirmation
            bool needsPurchase = quote.CopiesToBuy > 0 && quote.GoldCost > 0;
            if (needsPurchase)
            {
                InformationManager.ShowInquiry(
                    new InquiryData(
                        L.S("buy_item", "Buy Item"),
                        L.T(
                                "buy_item_text",
                                "Are you sure you want to buy {ITEM_NAME} for {ITEM_VALUE} gold?"
                            )
                            .SetTextVariable("ITEM_NAME", RowItem.Name)
                            .SetTextVariable("ITEM_VALUE", quote.GoldCost)
                            .ToString(),
                        true,
                        true,
                        L.S("yes", "Yes"),
                        L.S("no", "No"),
                        () =>
                        {
                            var res = EquipmentManager.TryEquip(
                                troop,
                                setIndex,
                                slot,
                                RowItem,
                                allowPurchase: true
                            );
                            if (
                                !res.Ok
                                && res.Reason == EquipmentManager.EquipFailReason.NotEnoughGold
                            )
                                Notifications.Popup(
                                    L.T("not_enough_gold_title", "Not Enough Gold"),
                                    L.T(
                                        "not_enough_gold_text",
                                        "You do not have enough gold to purchase this item."
                                    )
                                );
                            // res.Staged indicates if it went into staging (only DeltaAdd > 0 and option on)
                            State.UpdateEquipData();
                        },
                        () => { }
                    )
                );
                return;
            }

            // Otherwise, try equip directly (free, stocked, or cross-set share)
            {
                var res = EquipmentManager.TryEquip(
                    troop,
                    setIndex,
                    slot,
                    RowItem,
                    allowPurchase: true
                );
                State.UpdateEquipData();
                return;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Determine whether this equipment row matches the provided filter.
        /// </summary>
        public override bool FilterMatch(string filter)
        {
            if (RowItem == null)
                return true;

            var search = filter.Trim().ToLowerInvariant();
            var name = RowItem.Name.ToString().ToLowerInvariant();
            var category = RowItem.Category?.ToString().ToLowerInvariant();
            var type = RowItem.Type.ToString().ToLowerInvariant();
            var culture = RowItem.Culture?.Name?.ToString().ToLowerInvariant() ?? string.Empty;

            return name.Contains(search)
                || category.Contains(search)
                || type.Contains(search)
                || culture.Contains(search);
        }

        /// <summary>
        /// Refresh slot-related bindings when the active slot changes.
        /// </summary>
        public void OnSlotChanged()
        {
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(ShowIsEquipped));
            OnPropertyChanged(nameof(ShowInStockText));
            OnPropertyChanged(nameof(ShowValue));
            OnPropertyChanged(nameof(AvailableFromAnotherSet));
        }

        /// <summary>
        /// Refresh bindings affected by staged equip updates.
        /// </summary>
        public void OnEquipChanged()
        {
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(Stock));
            OnPropertyChanged(nameof(InStockText));
            OnPropertyChanged(nameof(ShowInStockText));
            OnPropertyChanged(nameof(ShowValue));
            OnPropertyChanged(nameof(ShowIsEquipped));
            OnPropertyChanged(nameof(IsDisabledText));
            OnPropertyChanged(nameof(AvailableFromAnotherSet));
        }

        /// <summary>
        /// Refresh bindings affected by equipment loadout changes.
        /// </summary>
        public void OnEquipmentChanged()
        {
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(IsEnabled));
            OnPropertyChanged(nameof(ShowIsEquipped));
            OnPropertyChanged(nameof(IsDisabledText));
            OnPropertyChanged(nameof(ShowInStockText));
            OnPropertyChanged(nameof(ShowValue));
            OnPropertyChanged(nameof(AvailableFromAnotherSet));
        }
    }
}
