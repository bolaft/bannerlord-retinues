using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor.VM.Equipment.Panel;
using Retinues.GUI.Helpers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Equipment.List
{
    [SafeClass]
    public sealed class EquipmentRowVM : BaseRow<EquipmentListVM, EquipmentRowVM>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public readonly EditorVM Editor;
        public readonly WItem Item;
        public readonly int? Progress;
        public readonly bool IsAvailable;

        public EquipmentRowVM(EditorVM editor, WItem item, int? progress, bool isAvailable)
        {
            Log.Info("Building EquipmentRowVM...");

            Editor = editor;
            Item = item;
            Progress = progress;
            IsAvailable = isAvailable;
        }

        public void Initialize()
        {
            Log.Info("Initializing EquipmentRowVM...");

            EventManager.EquipmentItemChange.RegisterProperties(
                this,
                nameof(InStockText),
                nameof(SkillRequirementText),
                nameof(TierBlockedText),
                nameof(ShowIsEquipped),
                nameof(ShowProgressText),
                nameof(ShowInStockText),
                nameof(ShowValue)
            );
            EventManager.SkillChange.RegisterProperties(
                this,
                nameof(SkillRequirementText),
                nameof(IsEnabled)
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Quick Access                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WEquipment SelectedEquipment => Editor?.EquipmentScreen?.Equipment;
        private WCharacter SelectedTroop => Editor?.TroopScreen?.TroopList?.Selection?.Troop;
        private EquipmentSlotVM SelectedSlot => Editor?.EquipmentScreen?.EquipmentPanel?.Selection;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Helper
        private bool IsTierBlocked =>
            Item != null
            && !DoctrineAPI.IsDoctrineUnlocked<Ironclad>()
            && (Item.Tier - (SelectedTroop?.Tier ?? 0)) > Config.AllowedTierDifference;

        // Helper
        private bool IsEmptyRow => Item == null;

        /* ━━━━━━━━ Values ━━━━━━━━ */

        [DataSourceProperty]
        public int Value => !IsEmptyRow ? EquipmentManager.GetItemCost(Item, SelectedTroop) : 0;

        [DataSourceProperty]
        public int Stock => Item?.GetStock() ?? 0;

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string Name => Item?.Name ?? L.S("empty_item", "Empty");

        [DataSourceProperty]
        public string InStockText =>
            ShowInStockText
                ? L.T("in_stock", "In Stock ({STOCK})").SetTextVariable("STOCK", Stock).ToString()
                : string.Empty;

        [DataSourceProperty]
        public string ProgressText =>
            ShowProgressText
                ? L.T("unlock_progress_text", "Unlocking ({PROGRESS}/{REQUIRED})")
                    .SetTextVariable("PROGRESS", Progress ?? 0)
                    .SetTextVariable("REQUIRED", Config.KillsForUnlock)
                    .ToString()
                : string.Empty;

        [DataSourceProperty]
        public string NotAvailableText =>
            ShowNotAvailableText
                ? Player.CurrentSettlement == null
                    ? L.S("item_unavailable_no_settlement", "Not in a town")
                    : L.T("item_unavailable_text", "Not sold in {SETTLEMENT}")
                        .SetTextVariable("SETTLEMENT", Player.CurrentSettlement?.Name)
                        .ToString()
                : string.Empty;

        [DataSourceProperty]
        public string SkillRequirementText =>
            ShowSkillRequirementText
                ? L.T("skill_requirement_text", "Requires {SKILL}: {LEVEL}")
                    .SetTextVariable("SKILL", Item?.RelevantSkill?.Name)
                    .SetTextVariable("LEVEL", Item?.Difficulty ?? 0)
                    .ToString()
                : string.Empty;

        [DataSourceProperty]
        public string TierBlockedText =>
            IsTierBlocked ? L.S("item_tier_blocked_text", "Troop tier too low.") : string.Empty;

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool ShowIsEquipped =>
            Config.EquipmentChangeTakesTime && (SelectedSlot?.Item == Item);

        [DataSourceProperty]
        public bool ShowProgressText => !IsEmptyRow && Progress != null;

        [DataSourceProperty]
        public bool ShowNotAvailableText => !IsEmptyRow && !ShowProgressText && !IsAvailable;

        [DataSourceProperty]
        public bool ShowTierBlockedText =>
            !IsEmptyRow && !ShowProgressText && !ShowSkillRequirementText && IsTierBlocked;

        [DataSourceProperty]
        public bool ShowSkillRequirementText =>
            !IsEmptyRow
            && !ShowProgressText
            && !ShowNotAvailableText
            && !ShowTierBlockedText
            && !(SelectedTroop?.MeetsItemRequirements(Item) ?? true);

        [DataSourceProperty]
        public bool ShowInStockText => IsEnabled && Item?.IsStocked == true;

        [DataSourceProperty]
        public bool ShowValue => IsEnabled && !ShowInStockText && Value > 0;

        /* ━━━━━━━━━ Image ━━━━━━━━ */

#if BL13
        [DataSourceProperty]
        public string ImageAdditionalArgs => Item?.Image?.AdditionalArgs;

        [DataSourceProperty]
        public string ImageId => Item?.Image?.Id;

        [DataSourceProperty]
        public string ImageTextureProviderName => Item?.Image?.TextureProviderName;
#else
        [DataSourceProperty]
        public string ImageId => Item?.Image?.Id;

        [DataSourceProperty]
        public int ImageTypeCode => Item?.Image?.ImageTypeCode ?? 0;

        [DataSourceProperty]
        public string ImageAdditionalArgs => Item?.Image?.AdditionalArgs;
#endif

        /* ━━━━━━━━ Tooltip ━━━━━━━ */

        [DataSourceProperty]
        public BasicTooltipViewModel Hint => Tooltip.MakeItemTooltip(Item);

        public override EquipmentListVM List => Editor.EquipmentScreen.EquipmentList;

        /* ━━━━━━━━ Enabled ━━━━━━━ */

        [DataSourceProperty]
        public override bool IsEnabled
        {
            get
            {
                if (IsEmptyRow)
                    return true; // Always can unequip

                if (ShowNotAvailableText)
                    return false; // Not sold here

                if (ShowTierBlockedText)
                    return false; // Troop tier too low

                if (ShowSkillRequirementText)
                    return false; // Skill requirements not met

                if (ShowProgressText)
                    return false; // Still unlocking

                return true; // All checks passed
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public new void ExecuteSelect()
        {
            if (Config.EquipmentChangeTakesTime == false) // Only check in instant equip mode
                if (
                    TroopRules.IsAllowedInContextWithPopup(
                        SelectedTroop,
                        L.S("action_modify", "modify")
                    ) == false
                )
                    return; // Modification not allowed in current context

            var selectedItem = Item;
            var equippedItem = SelectedSlot.Item;
            var stagedItem = SelectedSlot.StagedItem;

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
                        SelectedSlot.Unstage();
                        // Select the row
                        List.Select(Item);
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
                                        SelectedSlot.Unstage();
                                        // Apply unequip
                                        EquipmentManager.Equip(
                                            SelectedTroop,
                                            SelectedSlot.Index,
                                            Item,
                                            SelectedEquipment.Index
                                        );
                                        // Select the row
                                        List.Select(Item);
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
                            SelectedSlot.Unstage();
                            // Apply unequip
                            EquipmentManager.Equip(
                                SelectedTroop,
                                SelectedSlot.Index,
                                Item,
                                SelectedEquipment.Index
                            );
                            // Select the row
                            List.Select(Item);
                        }
                    }
                }
                // Case 2: Selection is already equipped
                else if (selectionIsEquipped)
                {
                    Log.Debug("[ExecuteSelect] Selection is already equipped.");

                    // Unstage if something is staged
                    SelectedSlot.Unstage();
                    // Select the row
                    List.Select(Item);

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
                            SelectedSlot.Unstage();
                            // Apply the item change
                            EquipmentManager.EquipFromStock(
                                SelectedTroop,
                                SelectedSlot.Index,
                                Item,
                                SelectedEquipment.Index
                            );
                            // Select the row
                            List.Select(Item);
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
                                            SelectedSlot.Unstage();
                                            // Apply the item change
                                            EquipmentManager.EquipFromPurchase(
                                                SelectedTroop,
                                                SelectedSlot.Index,
                                                Item,
                                                SelectedEquipment.Index
                                            );
                                            // Select the row
                                            List.Select(Item);
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
                        SelectedSlot.Unstage();
                        // Apply the item change
                        EquipmentManager.Equip(
                            SelectedTroop,
                            SelectedSlot.Index,
                            Item,
                            SelectedEquipment.Index
                        );
                        // Select the row
                        List.Select(Item);
                    }
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override bool FilterMatch(string filter)
        {
            if (IsEmptyRow)
                return true;

            var search = filter.Trim().ToLowerInvariant();
            var name = Item?.Name?.ToString().ToLowerInvariant() ?? string.Empty;
            var category = Item?.Category?.ToString().ToLowerInvariant() ?? string.Empty;
            var type = Item?.Type.ToString().ToLowerInvariant() ?? string.Empty;
            var culture = Item?.Culture?.Name?.ToString().ToLowerInvariant() ?? "";

            return name.Contains(search)
                || category.Contains(search)
                || type.Contains(search)
                || culture.Contains(search);
        }
    }
}
