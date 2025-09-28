using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace Retinues.Core.Editor.UI.VM.Equipment
{
    public sealed class EquipmentRowVM(WItem item, EquipmentListVM list, int? progress)
        : BaseRow<EquipmentListVM, EquipmentRowVM>(list)
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
                ? EquipmentManager.GetItemValue(Item, RowList?.Screen?.SelectedTroop)
                : 0;

        private readonly int? _progress = progress;

        [DataSourceProperty]
        public bool InProgress => _progress.HasValue;

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string ProgressText =>
            L.T("unlock_progress_text", "Unlocking ({PROGRESS}/{REQUIRED})")
                .SetTextVariable("PROGRESS", _progress ?? 0)
                .SetTextVariable("REQUIRED", Config.GetOption<int>("KillsForUnlock"))
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
            get
            {
                return _isVisible;
            }

            set
            {
                if (_isVisible == value) return;
                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }

        [DataSourceProperty]
        public bool IsEnabled
        {
            get
            {
                if (RowList?.Screen?.SelectedTroop == null)
                    return false; // No troop selected yet
                if (InProgress)
                    return false; // Cannot equip items that are still in progress
                if (Item == null)
                    return true; // Always allow unequipping

                // Check if the selected troop can equip this item
                return CanEquip;
            }
        }

        [DataSourceProperty]
        public bool CanEquip => RowList?.Screen?.SelectedTroop?.CanEquip(Item) ?? false;

        [DataSourceProperty]
        public bool CantEquip =>
            Item is not null && RowList?.Screen?.SelectedTroop != null && !CanEquip && !InProgress;

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

        [DataSourceProperty]
        public string ImageId => Item?.Image.Id;

        [DataSourceProperty]
        public int ImageTypeCode => Item?.Image.ImageTypeCode ?? 0;

        [DataSourceProperty]
        public string ImageAdditionalArgs => Item?.Image.AdditionalArgs;

        /* ━━━━━━━━ Tooltip ━━━━━━━ */

        [DataSourceProperty]
        public BasicTooltipViewModel Hint => Helpers.Tooltip.MakeItemTooltip(Item);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public new void ExecuteSelect()
        {
            // Case 1: Item has a cost
            if (Value > 0)
            {
                // Case 1a: Item is in stock
                if (Stock > 0)
                {
                    EquipItem(EquipmentManager.EquipFromStock); // Apply the item change
                }
                // Case 1b: Item is out of stock
                else
                {
                    // Case 1b1: Player can afford
                    if (Player.Gold >= Value)
                    {
                        InformationManager.ShowInquiry(
                            new InquiryData(
                                L.S("buy_item", "Buy Item"),
                                L.T(
                                        "buy_item_text",
                                        "Are you sure you want to buy {ITEM_NAME} for {ITEM_VALUE} gold?"
                                    )
                                    .SetTextVariable("ITEM_NAME", Item.Name)
                                    .SetTextVariable("ITEM_VALUE", Value)
                                    .ToString(),
                                true,
                                true,
                                L.S("yes", "Yes"),
                                L.S("no", "No"),
                                () =>
                                {
                                    EquipItem(EquipmentManager.EquipFromPurchase); // Apply the item change
                                },
                                null
                            )
                        );
                    }
                    // Case 1b2: Player cannot afford
                    else
                    {
                        InformationManager.ShowInquiry(
                            new InquiryData(
                                L.S("not_enough_gold", "Not enough gold"),
                                L.S(
                                    "not_enough_gold_text",
                                    "You do not have enough gold to purchase this item."
                                ),
                                false,
                                true,
                                null,
                                L.S("ok", "OK"),
                                null,
                                null
                            )
                        );
                    }
                }
            }
            // Case 2: Item is free (or null)
            else
            {
                EquipItem(EquipmentManager.Equip); // Apply the item change
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WItem Item { get; } = item;

        public void RefreshVisibility(string searchText)
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
            IsVisible = name.Contains(search) || category.Contains(search) || type.Contains(search);
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(Hint));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(ShowValue));
            OnPropertyChanged(nameof(Stock));
            OnPropertyChanged(nameof(ShowStock));
            OnPropertyChanged(nameof(CanEquip));
            OnPropertyChanged(nameof(CantEquip));
            OnPropertyChanged(nameof(IsEnabled));
            OnPropertyChanged(nameof(InProgress));
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(SkillRequirementText));
            OnPropertyChanged(nameof(ImageId));
            OnPropertyChanged(nameof(ImageTypeCode));
            OnPropertyChanged(nameof(ImageAdditionalArgs));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override void OnSelect()
        {
            RowList.Screen.EquipmentEditor.SelectedSlot.Refresh();
            RowList.Screen.Refresh();
            Refresh();
        }

        protected override void OnUnselect()
        {
            Refresh();
        }

        private void EquipItem(Action<WCharacter, EquipmentIndex, WItem> managerMethod)
        {
            var screen = RowList.Screen;
            var troop = screen.SelectedTroop;
            var slot = screen.EquipmentEditor.SelectedSlot.Slot;

            if (troop.Equipment.GetItem(slot) == Item)
                return; // No-op if already equipped

            // Equip the item
            managerMethod(troop, slot, Item);

            // Select the row VM
            Select();
        }
    }
}
