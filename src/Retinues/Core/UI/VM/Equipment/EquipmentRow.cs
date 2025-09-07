using TaleWorlds.Library;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Core.Wrappers.Objects;
using Retinues.Core.Logic;
using Retinues.Core.Utils;

namespace Retinues.Core.UI.VM.Equipment
{
    public sealed class EquipmentRowVM(WItem item, EquipmentListVM list) : BaseRow<EquipmentListVM, EquipmentRowVM>(list), IView
    {
        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty]
        public BasicTooltipViewModel Hint => Helpers.Tooltip.MakeItemTooltip(Item);

        [DataSourceProperty]
        public string Name => Item?.Name ?? "Empty";

        [DataSourceProperty]
        public int Value => Item?.Value ?? 0;

        [DataSourceProperty]
        public bool ShowValue
        {
            get
            {
                if (!Config.GetOption<bool>("PayForEquipment")) return false;
                if (Item == null) return false;
                if (Value == 0) return false;
                if (Stock > 0) return false;
                if (IsSelected) return false;

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
                if (!Config.GetOption<bool>("PayForEquipment")) return false;
                if (Item == null) return false;
                if (Value == 0) return false;
                if (Stock == 0) return false;
                if (IsSelected) return false;

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
                    Item.Unstock();  // Reduce stock by 1
                    ChangeItem(); // Apply the item change
                }
                // Case 1b: Item is out of stock
                else
                {
                    // Case 1b1: Player can afford
                    if (Player.Gold >= Value)
                    {
                        InformationManager.ShowInquiry(new InquiryData(
                            "Buy Item",
                            $"Are you sure you want to buy {Item.Name} for {Value} gold?",
                            true, true,
                            "Yes", "No",
                            () =>
                            {
                                Player.ChangeGold(-Value); // Deduct cost
                                ChangeItem(); // Apply the item change
                            }, null
                        ));
                    }
                    // Case 1b2: Player cannot afford
                    else
                    {
                        InformationManager.ShowInquiry(new InquiryData(
                            "Not enough gold",
                            "You do not have enough gold to purchase this item.",
                            false, true,
                            null, "OK",
                            null, null)
                        );
                    }
                }
            }
            // Case 2: Item is free (or null)
            else
            {
                ChangeItem(); // Apply the item change
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

        private void ChangeItem()
        {
            var screen = RowList.Screen;
            var troop = screen.SelectedTroop;
            var slot = screen.EquipmentEditor.SelectedSlot;

            if (troop.Equipment.GetItem(slot.Slot) == Item)
                return; // No-op if already equipped

            // If equipping a new item, unequip the old one (if any)
            var oldItem = troop.Unequip(slot.Slot);

            // If the old item had a value, restock it
            if (oldItem != null && oldItem.Value > 0)
                oldItem.Stock();

            // If unequipping a horse, also unequip the harness
            if (slot.Slot == EquipmentIndex.Horse)
            {
                var harnessItem = troop.Equipment.GetItem(EquipmentIndex.HorseHarness);
                if (harnessItem != null)
                {
                    var oldHarness = troop.Unequip(EquipmentIndex.HorseHarness);
                    if (oldHarness != null && oldHarness.Value > 0)
                        oldHarness.Stock();
                }
            }

            // Equip item to selected troop
            troop.Equip(Item, slot.Slot);

            // Select the row VM
            Select();
        }
    }
}