using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Helpers;
using Retinues.Troops.Edition;
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
                ],
                [UIEvent.Equipment] =
                [
                    nameof(IsEnabled),
                    nameof(IsDisabledText),
                    nameof(ShowInStockText),
                    nameof(ShowIsEquipped),
                    nameof(ShowValue),
                    nameof(IsSelected),
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
            !IsEmptyRow && State.Troop?.MeetsItemRequirements(RowItem) == false;
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
        public bool IsCrafted => RowItem?.IsCrafted == true;

        [DataSourceProperty]
        public bool ShowIsEquipped => StagedItem != null && IsEquipped;

        [DataSourceProperty]
        public bool ShowInStockText =>
            IsEnabled && !IsSelected && !IsEquipped && RowItem?.IsStocked == true;

        [DataSourceProperty]
        public bool ShowValue =>
            IsEnabled && !IsSelected && !IsEquipped && !ShowInStockText && Value > 0;

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

        [DataSourceMethod]
        /// <summary>
        /// Handle selection of this equipment row (equip/buy/unstage logic).
        /// </summary>
        public void ExecuteSelect()
        {
            if (RowItem != null && !RowItem.Slots.Contains(State.Slot))
                return; // Invalid slot for this item

            if (Config.EquipmentChangeTakesTime == false) // Only check in instant equip mode
                if (
                    TroopRules.IsAllowedInContextWithPopup(
                        State.Troop,
                        L.S("action_modify", "modify")
                    ) == false
                )
                    return; // Modification not allowed in current context

            var equippedItem = State.Equipment.Get(State.Slot);
            var stagedItem = State.Equipment.GetStaged(State.Slot);

            bool staging = stagedItem != null;
            bool selectionIsNull = RowItem == null;
            bool selectionIsEquipped = RowItem != null && RowItem == equippedItem;
            bool selectionIsStaged = RowItem != null && RowItem == stagedItem;

            // Selecting the already staged item
            if (selectionIsStaged)
            {
                Log.Debug("[ExecuteSelect] Already staged item selected, no-op.");
            }
            // Something unstaged
            else
            {
                // Case 1: Selection is null (unequip)
                if (selectionIsNull)
                {
                    Log.Debug("[ExecuteSelect] Selection is null (unequip).");

                    // Case 1a: Nothing is equipped (no-op)
                    if (equippedItem == null)
                    {
                        Log.Debug("[ExecuteSelect] Nothing is equipped.");

                        if (staging)
                        {
                            Log.Debug("[ExecuteSelect] Something is staged, unstaging.");
                            // Unstage if something is staged
                            State.Troop.Unstage(State.Slot, stock: true);
                            // Select the row
                            State.UpdateEquipData();
                        }
                        else
                        {
                            Log.Debug("[ExecuteSelect] Nothing is staged, no-op.");
                        }
                    }
                    // Case 1b: An item is equipped
                    else
                    {
                        Log.Debug("[ExecuteSelect] An item is equipped, proceeding to unequip.");
                        // Warn if unequipping will take time
                        if (Config.EquipmentChangeTakesTime)
                        {
                            Log.Debug(
                                "[ExecuteSelect] Equipment change takes time, showing warning inquiry."
                            );
                            InformationManager.ShowInquiry(
                                new InquiryData(
                                    titleText: L.S("warning", "Warning"),
                                    text: L.T(
                                            "unequip_warning_text",
                                            "You are about to unequip {ITEM}, it will take time to re-equip a new one. Continue anyway?"
                                        )
                                        .SetTextVariable("ITEM", equippedItem.Name)
                                        .ToString(),
                                    isAffirmativeOptionShown: true,
                                    isNegativeOptionShown: true,
                                    affirmativeText: L.S("continue", "Continue"),
                                    negativeText: L.S("cancel", "Cancel"),
                                    affirmativeAction: () =>
                                    {
                                        Log.Debug("[ExecuteSelect] Applying unequip.");
                                        // Unstage if something is staged
                                        State.Troop.Unstage(State.Slot, stock: true);
                                        // Apply unequip
                                        EquipmentManager.Equip(
                                            State.Troop,
                                            State.Slot,
                                            RowItem,
                                            State.Equipment.Index
                                        );
                                        // Select the row
                                        State.UpdateEquipData();
                                    },
                                    negativeAction: () => { } // Give the player an out
                                )
                            );
                        }
                        // No warning needed, just unequip
                        else
                        {
                            Log.Debug("[ExecuteSelect] Applying unequip without warning.");
                            // Unstage if something is staged
                            State.Troop.Unstage(State.Slot, stock: true);
                            // Apply unequip
                            EquipmentManager.Equip(
                                State.Troop,
                                State.Slot,
                                RowItem,
                                State.Equipment.Index
                            );
                            // Select the row
                            State.UpdateEquipData();
                        }
                    }
                }
                // Case 2: Selection is already equipped
                else if (selectionIsEquipped)
                {
                    Log.Debug("[ExecuteSelect] Selection is already equipped.");

                    // Unstage if something is staged
                    State.Troop.Unstage(State.Slot, stock: true);
                    // Select the row
                    State.UpdateEquipData();

                    return; // No-op if already equipped after unstaging
                }
                // Case 3: Selection is something else (equip)
                else
                {
                    Log.Debug("[ExecuteSelect] Selection is a new item, proceeding to equip.");
                    // Case 3a: Item has a cost
                    if (Value > 0)
                    {
                        Log.Debug("[ExecuteSelect] Item has a cost: " + Value);
                        // Case 3a1: Item is in stock
                        if (Stock > 0)
                        {
                            Log.Debug("[ExecuteSelect] Item is in stock, equipping from stock.");
                            // Unstage if something is staged
                            State.Troop.Unstage(State.Slot, stock: true);
                            // Apply the item change
                            EquipmentManager.EquipFromStock(
                                State.Troop,
                                State.Slot,
                                RowItem,
                                State.Equipment.Index
                            );
                            // Select the row
                            State.UpdateEquipData();
                        }
                        // Case 3a2: Item is out of stock
                        else
                        {
                            Log.Debug("[ExecuteSelect] Item is out of stock.");
                            // Case 3a2i: Player can afford
                            if (Player.Gold >= Value)
                            {
                                Log.Debug(
                                    "[ExecuteSelect] Player can afford the item, showing inquiry."
                                );
                                InformationManager.ShowInquiry(
                                    new InquiryData(
                                        titleText: L.S("buy_item", "Buy Item"),
                                        text: L.T(
                                                "buy_item_text",
                                                "Are you sure you want to buy {ITEM_NAME} for {ITEM_VALUE} gold?"
                                            )
                                            .SetTextVariable("ITEM_NAME", RowItem.Name)
                                            .SetTextVariable("ITEM_VALUE", Value)
                                            .ToString(),
                                        isAffirmativeOptionShown: true,
                                        isNegativeOptionShown: true,
                                        affirmativeText: L.S("yes", "Yes"),
                                        negativeText: L.S("no", "No"),
                                        affirmativeAction: () =>
                                        {
                                            Log.Debug(
                                                "[ExecuteSelect] Player confirmed purchase, equipping from purchase."
                                            );
                                            // Unstage if something is staged
                                            State.Troop.Unstage(State.Slot, stock: true);
                                            // Apply the item change
                                            EquipmentManager.EquipFromPurchase(
                                                State.Troop,
                                                State.Slot,
                                                RowItem,
                                                State.Equipment.Index
                                            );
                                            // Select the row
                                            State.UpdateEquipData();
                                        },
                                        negativeAction: () =>
                                        {
                                            Log.Debug(
                                                "[ExecuteSelect] Player cancelled purchase, no-op."
                                            );
                                        }
                                    )
                                );
                            }
                            // Case 3a2ii: Player cannot afford
                            else
                            {
                                Log.Debug(
                                    "[ExecuteSelect] Player cannot afford the item, showing popup."
                                );
                                Popup.Display(
                                    L.T("not_enough_gold", "Not enough gold"),
                                    L.T(
                                        "not_enough_gold_text",
                                        "You do not have enough gold to purchase this item."
                                    )
                                );
                            }
                        }
                    }
                    // Case 3b: Item is free
                    else
                    {
                        Log.Debug("[ExecuteSelect] Item is free, equipping.");
                        // Unstage if something is staged
                        State.Troop.Unstage(State.Slot, stock: true);
                        // Apply the item change
                        EquipmentManager.Equip(
                            State.Troop,
                            State.Slot,
                            RowItem,
                            State.Equipment.Index
                        );
                        // Select the row
                        State.UpdateEquipData();
                    }
                }
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
        }
    }
}
