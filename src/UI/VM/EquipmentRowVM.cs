using System;
using TaleWorlds.Library;
using TaleWorlds.Core;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM
{
    public sealed class EquipmentRowVM : ViewModel
    {
        private readonly Action<EquipmentRowVM> _onSelect;

        public ItemObject Equipment { get; }

        private bool _isSelected;

        public EquipmentRowVM(ItemObject equipment, Action<EquipmentRowVM> onSelect)
        {
            Equipment = equipment;
            _onSelect = onSelect;
        }

        [DataSourceProperty] public string Name => Equipment?.Name.ToString();

        [DataSourceProperty] public bool IsSelected
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
            _onSelect?.Invoke(this);
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(IsSelected));
        }
    }
}
