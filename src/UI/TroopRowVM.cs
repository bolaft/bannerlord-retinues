using System;
using TaleWorlds.Library;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI
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

        [DataSourceProperty]
        public string Name => Troop.Name;

        [DataSourceProperty]
        public string TierText => Troop != null ? $"T{Troop.Level}" : "";

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

        [DataSourceMethod]
        public void ExecuteSelect()
        {
            _onSelect?.Invoke(this);
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(TierText));
        }
    }
}
