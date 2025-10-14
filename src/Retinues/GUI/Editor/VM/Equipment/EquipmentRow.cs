using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
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
        EquipmentListVM list,
        EquipmentSlotVM slot,
        WItem item,
        int? progress,
        bool isAvailable
    ) : BaseRow<EquipmentListVM, EquipmentRowVM>(list)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly EquipmentSlotVM _slot = slot;
        private readonly WItem _item = item;
        private readonly int _progress = progress ?? 0;
        private readonly bool _isInProgress = progress != null;
        private readonly bool _isAvailable = isAvailable;

        public WItem Item => _item;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ Values ━━━━━━━━ */

        [DataSourceProperty]
        public int Value => EquipmentManager.GetItemValue(Item, Editor.Troop);

        [DataSourceProperty]
        public int Stock => Item.GetStock();

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string Name => Item.Name ?? L.S("empty_item", "Empty");

        [DataSourceProperty]
        public string InStockText =>
            ShowInStockText
                ? L.T("in_stock", "In Stock ({STOCK})").SetTextVariable("STOCK", Stock).ToString()
                : string.Empty;

        [DataSourceProperty]
        public string ProgressText =>
            ShowProgressText
                ? L.T("unlock_progress_text", "Unlocking ({PROGRESS}/{REQUIRED})")
                    .SetTextVariable("PROGRESS", _progress)
                    .SetTextVariable("REQUIRED", Config.KillsForUnlock)
                    .ToString()
                : string.Empty;

        [DataSourceProperty]
        public string NotAvailableText =>
            ShowNotAvailableText
                ? Player.CurrentSettlement == null
                    ? L.S("item_unavailable_no_settlement", "Not in a town")
                    : L.T("item_unavailable_text", "Not sold in {SETTLEMENT}")
                        .SetTextVariable("SETTLEMENT", Player.CurrentSettlement.Name)
                        .ToString()
                : string.Empty;

        [DataSourceProperty]
        public string SkillRequirementText =>
            ShowSkillRequirementText
                ? L.T("skill_requirement_text", "Requires {SKILL}: {LEVEL}")
                    .SetTextVariable("SKILL", Item.RelevantSkill.Name)
                    .SetTextVariable("LEVEL", Item.Difficulty)
                    .ToString()
                : string.Empty;

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool ShowProgressText => !IsEnabled && _isInProgress;

        [DataSourceProperty]
        public bool ShowNotAvailableText => !IsEnabled && !ShowProgressText && !_isAvailable;

        [DataSourceProperty]
        public bool ShowSkillRequirementText =>
            !IsEnabled
            && !ShowProgressText
            && !ShowNotAvailableText
            && !Editor.Troop.MeetsItemRequirements(Item);

        [DataSourceProperty]
        public bool ShowInStockText => IsEnabled && Item.IsStocked;

        [DataSourceProperty]
        public bool ShowValue => IsEnabled && !ShowInStockText && Value > 0;

        /* ━━━━━━━━━ Image ━━━━━━━━ */

#if BL13
        [DataSourceProperty]
        public string AdditionalArgs => Item.Image?.AdditionalArgs;

        [DataSourceProperty]
        public string Id => Item.Image?.Id;

        [DataSourceProperty]
        public string TextureProviderName => Item.Image?.TextureProviderName;
#else
        [DataSourceProperty]
        public string ImageId => Item.Image.Id;

        [DataSourceProperty]
        public int ImageTypeCode => Item.Image.ImageTypeCode ?? 0;

        [DataSourceProperty]
        public string ImageAdditionalArgs => Item.Image.AdditionalArgs;
#endif

        /* ━━━━━━━━ Tooltip ━━━━━━━ */

        [DataSourceProperty]
        public BasicTooltipViewModel Hint => Tooltip.MakeItemTooltip(Item);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public new void ExecuteSelect()
        {
            if (Config.EquipmentChangeTakesTime == false) // Only check in instant equip mode
                if (
                    TroopRules.IsAllowedInContextWithPopup(
                        Editor.Troop,
                        Editor.Faction,
                        L.S("action_modify", "modify")
                    ) == false
                )
                    return; // Modification not allowed in current context

            var selectedItem = Item;
            var equippedItem = _slot.Item;
            var stagedItem = _slot.StagedItem;

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
                        _slot.Unstage();
                        // Select the row
                        Select();
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
                                        _slot.Unstage();
                                        // Apply unequip
                                        EquipmentManager.Equip(
                                            Editor.Troop,
                                            _slot.EquipmentIndex,
                                            null,
                                            Editor.EquipmentPanel.EquipmentCategory,
                                            Editor.EquipmentPanel.Index
                                        );
                                        // Select the row
                                        Select();
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
                            _slot.Unstage();
                            // Apply unequip
                            EquipmentManager.Equip(
                                Editor.Troop,
                                _slot.EquipmentIndex,
                                null,
                                Editor.EquipmentPanel.EquipmentCategory,
                                Editor.EquipmentPanel.Index
                            );
                            // Select the row
                            Select();
                        }
                    }
                }
                // Case 2: Selection is already equipped
                else if (selectionIsEquipped)
                {
                    Log.Debug("[ExecuteSelect] Selection is already equipped.");

                    // Unstage if something is staged
                    _slot.Unstage();
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
                            _slot.Unstage();
                            // Apply the item change
                            EquipmentManager.EquipFromStock(
                                Editor.Troop,
                                _slot.EquipmentIndex,
                                selectedItem,
                                Editor.EquipmentPanel.EquipmentCategory,
                                Editor.EquipmentPanel.Index
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
                                            _slot.Unstage();
                                            // Apply the item change
                                            EquipmentManager.EquipFromPurchase(
                                                Editor.Troop,
                                                _slot.EquipmentIndex,
                                                selectedItem,
                                                Editor.EquipmentPanel.EquipmentCategory,
                                                Editor.EquipmentPanel.Index
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
                        _slot.Unstage();
                        // Apply the item change
                        EquipmentManager.Equip(
                            Editor.Troop,
                            _slot.EquipmentIndex,
                            selectedItem,
                            Editor.EquipmentPanel.EquipmentCategory,
                            Editor.EquipmentPanel.Index
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

        /// <summary>
        /// Updates the visibility of the row based on the given filter text.
        /// </summary>
        public override bool FilterMatch(string filter)
        {
            if (Item == null)
                return true;

            var search = filter.Trim().ToLowerInvariant();
            var name = Item.Name.ToString().ToLowerInvariant();
            var category = Item.Category.ToString().ToLowerInvariant();
            var type = Item.Type.ToString().ToLowerInvariant();
            var culture = Item.Culture?.Name?.ToString().ToLowerInvariant() ?? "";

            return name.Contains(search)
                || category.Contains(search)
                || type.Contains(search)
                || culture.Contains(search);
        }
    }
}
