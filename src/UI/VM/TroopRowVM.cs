using System;
using TaleWorlds.Library;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM
{
    public sealed class TroopRowVM : ViewModel
    {
        private readonly Action<TroopRowVM> _onSelect;

        public CharacterWrapper Troop { get; }

        private bool _isSelected;

        public TroopRowVM(CharacterWrapper troop, Action<TroopRowVM> onSelect)
        {
            Troop = troop;
            _onSelect = onSelect;
        }

        [DataSourceProperty] public int ImageTypeCode => Troop?.Image.ImageTypeCode ?? 0;

        [DataSourceProperty] public string ImageId => Troop?.Image.Id ?? "";

        [DataSourceProperty] public string ImageAdditionalArgs => Troop?.Image.AdditionalArgs ?? "";

        [DataSourceProperty] public string TierText => $"T{Troop.Tier}";

        [DataSourceProperty] public string DisplayName
        {
            get
            {
                var indent = new string(' ', Depth * 4);
                return $"{indent}{Troop.Name}";
            }
        }

        public int Depth { get; set; }

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
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(TierText));
            OnPropertyChanged(nameof(ImageId));
            OnPropertyChanged(nameof(ImageTypeCode));
            OnPropertyChanged(nameof(ImageAdditionalArgs));
        }
    }
}
