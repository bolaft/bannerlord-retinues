using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Features.Upgrade.Behaviors;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Helpers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Equipment
{
    /// <summary>
    /// ViewModel for an equipment row. Handles item display, equipping, stock, progress, and UI refresh logic.
    /// </summary>
    [SafeClass]
    public sealed class EquipmentRowVM(
        WItem item,
        EquipmentListVM list,
        int? progress,
        bool available
    ) : BaseRow<EquipmentListVM, EquipmentRowVM>(list)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ Basics ━━━━━━━━ */

        [DataSourceProperty]
        public string Name => Item?.Name ?? L.S("empty_item", "Empty");

        [DataSourceProperty]
        public int Value =>
            Config.GetOption<bool>("PayForEquipment")
                ? EquipmentManager.GetItemValue(Item, List?.Screen?.SelectedTroop)
                : 0;

        private readonly int? _progress = progress;

        [DataSourceProperty]
        public bool InProgress => _progress.HasValue;

        [DataSourceProperty]
        public bool NotAvailable => !available && !InProgress && !CantEquip;

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string ProgressText =>
            L.T("unlock_progress_text", "Unlocking ({PROGRESS}/{REQUIRED})")
                .SetTextVariable("PROGRESS", _progress ?? 0)
                .SetTextVariable("REQUIRED", Config.GetOption<int>("KillsForUnlock"))
                .ToString();

        [DataSourceProperty]
        public string UnavailableText =>
            Player.CurrentSettlement == null
                ? L.S("item_unavailable_no_settlement", "Not in a town")
                : L.T("item_unavailable_text", "Not sold in {SETTLEMENT}")
                    .SetTextVariable("SETTLEMENT", Player.CurrentSettlement.Name)
                    .ToString();

        [DataSourceProperty]
        public string SkillRequirementText =>
            L.T("skill_requirement_text", "Requires {SKILL}: {LEVEL}")
                .SetTextVariable("SKILL", Item?.RelevantSkill?.Name.ToString() ?? "N/A")
                .SetTextVariable("LEVEL", Item?.Difficulty ?? 0)
                .ToString();

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        private bool _isVisible = true;

        [DataSourceProperty]
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                if (_isVisible == value)
                    return;
                _isVisible = value;
            }
        }

        [DataSourceProperty]
        public bool IsEnabled
        {
            get
            {
                if (Item == null)
                    return true; // Always allow unequipping
                if (RowList?.Screen?.SelectedTroop == null)
                    return false; // No troop selected yet
                if (NotAvailable)
                    return false; // Item is not available in stores
                if (InProgress)
                    return false; // Cannot equip items that are still in progress

                // Check if the selected troop can equip this item
                return CanEquip;
            }
        }

        [DataSourceProperty]
        public bool IsActual =>
            RowList?.Screen?.EquipmentEditor?.SelectedSlot?.IsStaged == true
            && Item == RowList?.Screen?.EquipmentEditor?.SelectedSlot?.ActualItem;

        [DataSourceProperty]
        public bool CanEquip => List?.Screen?.SelectedTroop?.CanEquip(Item) ?? false;

        [DataSourceProperty]
        public bool CantEquip =>
            Item is not null && List?.Screen?.SelectedTroop != null && !CanEquip && !InProgress;

        [DataSourceProperty]
        public bool ShowValue
        {
            get
            {
                if (!IsEnabled)
                    return false;
                if (!Config.GetOption<bool>("PayForEquipment"))
                    return false;
                if (Item == null)
                    return false;
                if (Value == 0)
                    return false;
                if (Stock > 0)
                    return false;
                if (IsSelected)
                    return false;
                if (IsActual)
                    return false;

                return true;
            }
        }

        /* ━━━━━━━━ Stocks ━━━━━━━━ */

        [DataSourceProperty]
        public int Stock => Item?.GetStock() ?? 0;

        [DataSourceProperty]
        public string InStockText =>
            L.T("in_stock", "In Stock ({STOCK})").SetTextVariable("STOCK", Stock).ToString();

        [DataSourceProperty]
        public bool ShowStock
        {
            get
            {
                if (InProgress)
                    return false;
                if (!Config.GetOption<bool>("PayForEquipment"))
                    return false;
                if (Item == null)
                    return false;
                if (Value == 0)
                    return false;
                if (Stock == 0)
                    return false;
                if (IsSelected)
                    return false;

                return true;
            }
        }

        /* ━━━━━━━━━ Image ━━━━━━━━ */

#if BL13
        [DataSourceProperty]
        public string AdditionalArgs => Item?.Image?.AdditionalArgs;

        [DataSourceProperty]
        public string Id => Item?.Image?.Id;

        [DataSourceProperty]
        public string TextureProviderName => Item?.Image?.TextureProviderName;
#else
        [DataSourceProperty]
        public string ImageId => Item?.Image.Id;

        [DataSourceProperty]
        public int ImageTypeCode => Item?.Image.ImageTypeCode ?? 0;

        [DataSourceProperty]
        public string ImageAdditionalArgs => Item?.Image.AdditionalArgs;
#endif

        /* ━━━━━━━━ Tooltip ━━━━━━━ */

        [DataSourceProperty]
        public BasicTooltipViewModel Hint => Tooltip.MakeItemTooltip(Item);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void Unstage()
        {
            var slot = List?.Screen?.EquipmentEditor?.SelectedSlot;
            if (slot?.StagedItem == null)
                return; // No slot or no staged item, cannot unstage

            Log.Debug("[Unstage] Unstaging item: " + slot.StagedItem.Name);

            slot.StagedItem.Stock(); // Restock the staged item
            TroopEquipBehavior.UnstageChange(
                List?.Screen?.SelectedTroop,
                slot.Slot,
                LoadoutCategory,
                LoadoutIndex
            );
        }

        [DataSourceMethod]
        public new void ExecuteSelect()
        {
            var screen = List?.Screen;
            var slot = screen?.EquipmentEditor?.SelectedSlot;
            var troop = screen?.SelectedTroop;

            if (slot == null || troop == null)
            {
                Log.Error("[ExecuteSelect] No slot or troop selected, aborting.");
                return;
            }

            var selectedItem = Item;
            var equippedItem = slot?.ActualItem;
            var stagedItem = slot?.StagedItem;

            bool staging = stagedItem != null;
            bool selectionIsNull = selectedItem == null;
            bool selectionIsEquipped = selectedItem != null && selectedItem == equippedItem;
            bool selectionIsStaged = selectedItem != null && selectedItem == stagedItem;

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
                        Log.Debug("[ExecuteSelect] Nothing is equipped, no-op.");
                        // Unstage if something is staged
                        Unstage();
                        // Select the row
                        Select();
                    }
                    // Case 1b: An item is equipped
                    else
                    {
                        Log.Debug("[ExecuteSelect] An item is equipped, proceeding to unequip.");
                        // Warn if unequipping will take time
                        if (Config.GetOption<bool>("EquipmentChangeTakesTime"))
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
                                        Unstage();
                                        // Apply unequip
                                        EquipmentManager.Equip(
                                            troop,
                                            slot.Slot,
                                            null,
                                            LoadoutCategory,
                                            LoadoutIndex
                                        );
                                        // Select the row
                                        Select();
                                    },
                                    negativeAction: () => { } // Give the player an out
                                )
                            );
                        }
                    }
                }
                // Case 2: Selection is already equipped
                else if (selectionIsEquipped)
                {
                    Log.Debug("[ExecuteSelect] Selection is already equipped.");

                    // Unstage if something is staged
                    Unstage();
                    // Select the row
                    Select();

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
                            Unstage();
                            // Apply the item change
                            EquipmentManager.EquipFromStock(
                                troop,
                                slot.Slot,
                                selectedItem,
                                LoadoutCategory,
                                LoadoutIndex
                            );
                            // Select the row
                            Select();
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
                                            .SetTextVariable("ITEM_NAME", selectedItem.Name)
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
                                            Unstage();
                                            // Apply the item change
                                            EquipmentManager.EquipFromPurchase(
                                                troop,
                                                slot.Slot,
                                                selectedItem,
                                                LoadoutCategory,
                                                LoadoutIndex
                                            );
                                            // Select the row
                                            Select();
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
                        Unstage();
                        // Apply the item change
                        EquipmentManager.Equip(
                            troop,
                            slot.Slot,
                            selectedItem,
                            LoadoutCategory,
                            LoadoutIndex
                        );
                        // Select the row
                        Select();
                    }
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WItem Item { get; } = item;

        public void UpdateIsVisible(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                IsVisible = true;
                return;
            }

            if (Item == null)
            {
                IsVisible = true;
                return;
            }

            var search = searchText.Trim().ToLowerInvariant();
            var name = Item.Name.ToString().ToLowerInvariant();
            var category = Item.Category.ToString().ToLowerInvariant();
            var type = Item.Type.ToString().ToLowerInvariant();
            var culture = Item.Culture?.Name?.ToString().ToLowerInvariant() ?? "";
            IsVisible =
                name.Contains(search)
                || category.Contains(search)
                || type.Contains(search)
                || culture.Contains(search);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WLoadout.Category LoadoutCategory =>
            List?.Screen?.EquipmentEditor?.LoadoutCategory ?? WLoadout.Category.Battle;
        private int LoadoutIndex => List?.Screen?.EquipmentEditor?.LoadoutIndex ?? 0;

        protected override void OnSelect() { }

        protected override void OnUnselect() { }
    }
}
