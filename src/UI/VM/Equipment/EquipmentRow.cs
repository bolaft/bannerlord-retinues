using TaleWorlds.Library;
using TaleWorlds.Core.ViewModelCollection.Information;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;
using System.Runtime.InteropServices;

namespace CustomClanTroops.UI.VM.Equipment
{
    public sealed class EquipmentRowVM(WItem item, EquipmentListVM owner) : ViewModel, IView
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private readonly EquipmentListVM _owner = owner;

        private bool _isSelected = false;

        // =========================================================================
        // Selected Troop
        // =========================================================================

        public WCharacter SelectedTroop => _owner.SelectedTroop;

        // =========================================================================
        // Public API
        // =========================================================================

        public WItem Item = item;

        public bool IsSelected {
            get => _isSelected;
            set
            {
                if (value)
                    Log.Debug($"{Item.Name} selected.");

                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        // =========================================================================
        // Data Source Properties
        // =========================================================================

        [DataSourceProperty]
        public BasicTooltipViewModel Hint => Helpers.ItemTooltip.Make(Item);

        [DataSourceProperty]
        public string Name => Item.Name;

        [DataSourceProperty]
        public int Value => Item.Value;

        [DataSourceProperty]
        public bool ShowValue
        {
            get
            {
                // No cost if not paying for troop equipment
                if (!Config.PayForTroopEquipment) return false;

                // No cost to empty a slot
                if (Item == null) return false;

                // No cost if item is in stock
                if (Stock > 0) return true;

                return true;
            }
        }

        [DataSourceProperty]
        public int Stock => Item.GetStock();

        [DataSourceProperty]
        public bool ShowStock
        {
            get
            {
                // No need for stocks if not paying for troop equipment
                if (!Config.PayForTroopEquipment) return false;

                // No need for stocks to empty a slot
                if (Item == null) return false;

                // No stock means no need to display stock
                if (Stock == 0) return false;

                return true;
            }
        }

        [DataSourceProperty]
        public bool CanEquip => SelectedTroop.CanEquip(Item);

        [DataSourceProperty]
        public string ImageId => Item.Image.Id;

        [DataSourceProperty]
        public int ImageTypeCode => Item.Image.ImageTypeCode;

        [DataSourceProperty]
        public string ImageAdditionalArgs => Item.Image.AdditionalArgs;

        // =========================================================================
        // Actions
        // =========================================================================

        [DataSourceMethod]
        public void ExecuteSelect()
        {
            IsSelected = true;
        }

        // =========================================================================
        // Refresh
        // =========================================================================

        public void Refresh()
        {
            OnPropertyChanged(nameof(Item));
            OnPropertyChanged(nameof(IsSelected));
        }
    }
}