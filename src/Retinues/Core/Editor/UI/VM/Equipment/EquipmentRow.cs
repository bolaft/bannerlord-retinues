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
    public sealed class EquipmentRowVM(WItem item, EquipmentListVM list)
        : BaseRow<EquipmentListVM, EquipmentRowVM>(list),
            IView
    {
        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty]
        public string Name => Item?.Name ?? L.S("empty_item", "Empty");

        [DataSourceProperty]
        public int Value => EquipmentManager.GetItemValue(Item, RowList?.Screen?.SelectedTroop);

        [DataSourceProperty]
        public bool ShowValue
        {
            get
            {
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

        [DataSourceProperty]
        public int Stock => Item?.GetStock() ?? 0;

        [DataSourceProperty]
        public bool ShowStock
        {
            get
            {
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

        [DataSourceProperty]
        public bool CanEquip
        {
            get
            {
                if (RowList?.Screen?.SelectedTroop == null)
                    return false; // No troop selected yet
                if (Item == null)
                    return true; // Always allow unequipping

                // Check if the selected troop can equip this item
                return RowList.Screen.SelectedTroop.CanEquip(Item);
            }
        }

        [DataSourceProperty]
        public string ImageId => Item?.Image.Id;

        [DataSourceProperty]
        public int ImageTypeCode => Item?.Image.ImageTypeCode ?? 0;

        [DataSourceProperty]
        public string ImageAdditionalArgs => Item?.Image.AdditionalArgs;

        [DataSourceProperty]
        public BasicTooltipViewModel Hint => Helpers.Tooltip.MakeItemTooltip(Item);

        // =========================================================================
        // Action Bindings
        // =========================================================================

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
                                L.T("buy_item_text", "Are you sure you want to buy {ITEM_NAME} for {ITEM_VALUE} gold?")
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
                                L.S("not_enough_gold_text", "You do not have enough gold to purchase this item."),
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

        // =========================================================================
        // Public API
        // =========================================================================

        public WItem Item { get; } = item;

        public void Refresh()
        {
            OnPropertyChanged(nameof(Hint));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(ShowValue));
            OnPropertyChanged(nameof(Stock));
            OnPropertyChanged(nameof(ShowStock));
            OnPropertyChanged(nameof(CanEquip));
            OnPropertyChanged(nameof(ImageId));
            OnPropertyChanged(nameof(ImageTypeCode));
            OnPropertyChanged(nameof(ImageAdditionalArgs));
        }

        // =========================================================================
        // Internals
        // =========================================================================

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
