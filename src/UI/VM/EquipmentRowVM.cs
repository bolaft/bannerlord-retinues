using System;
using TaleWorlds.Library;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM
{
    public sealed class EquipmentRowVM : ViewModel
    {
        private readonly Action<EquipmentRowVM> _onSelect;

        public EquipmentWrapper Equipment { get; }

        private bool _isSelected;

        public EquipmentRowVM(EquipmentWrapper item, Action<EquipmentRowVM> onSelect)
        {
            Equipment = item;
            _onSelect = onSelect;
        }

        [DataSourceProperty] public string Name => Equipment.Name;

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
