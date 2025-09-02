using System;
using System.Text;
using TaleWorlds.Library;
using TaleWorlds.Core;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM
{
    public sealed class EquipmentRowVM : ViewModel
    {
        private readonly Action<EquipmentRowVM> _onSelect;

        public ItemObject Equipment { get; }

        private bool _isSelected;
        private bool _canEquip;

        public EquipmentRowVM(ItemObject equipment, bool canEquip, Action<EquipmentRowVM> onSelect)
        {
            Equipment = equipment;
            _onSelect = onSelect;
            _canEquip = canEquip;
        }

        // ---- Existing ----
        [DataSourceProperty] public string Name => Equipment?.Name?.ToString() ?? "Empty";

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        [DataSourceMethod] public void ExecuteSelect()
        {
            // Prevents buying the already equipped item
            if (IsSelected) return;

            if (CanEquip)
            {
                _onSelect?.Invoke(this);
            }
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(ImageId));
            OnPropertyChanged(nameof(ImageTypeCode));
            OnPropertyChanged(nameof(ImageAdditionalArgs));
            OnPropertyChanged(nameof(Stock));
            OnPropertyChanged(nameof(Cost));
        }

        private ImageIdentifierVM Image => Equipment != null ? new ImageIdentifierVM(Equipment, "") : null;

        [DataSourceProperty] public int ImageTypeCode => Image?.ImageTypeCode ?? 0;

        [DataSourceProperty] public string ImageId => Image?.Id ?? "";

        [DataSourceProperty] public string ImageAdditionalArgs => Image?.AdditionalArgs ?? "";

        [DataSourceProperty] public bool CanEquip => _canEquip;

        [DataSourceProperty] public int Stock => EquipmentManager.GetStock(Equipment);

        [DataSourceProperty] public bool ShowStock
        {
            get
            {
                // Don't show if there is a cost
                if (Cost > 0) return false;

                // Don't show if there is no stock
                if (Stock == 0) return false;

                return true;
            }
        }

        [DataSourceProperty] public int Cost
        {
            get
            {
                // No cost if set in config
                if (!Config.PayForTroopEquipment) return 0;

                // No cost for empty slot
                if (Equipment == null) return 0;

                // If we have it in stock, we don't pay
                if (Stock > 0) return 0;

                // Otherwise, pay item price
                return Equipment?.Value ?? 0;
            }
        }

        [DataSourceProperty] public bool ShowCost
        {
            get
            {
                // Don't show if costs nothing
                if (Cost == 0) return false;

                // Don't show if currently equipped
                if (IsSelected) return false;

                return true;
            }
        }
    }
}
