using TaleWorlds.Library;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM.Troop
{
    public sealed class TroopRowVM(WCharacter troop, TroopListVM owner) : ViewModel, IView
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private readonly TroopListVM _owner = owner;

        private bool _isSelected = false;

        // =========================================================================
        // Selected Troop
        // =========================================================================

        public WCharacter SelectedTroop => _owner.SelectedTroop;

        // =========================================================================
        // Public API
        // =========================================================================

        public WCharacter Troop = troop;

        public bool IsSelected {
            get => _isSelected;
            set
            {
                if (value)
                    Log.Debug($"{Troop.Name} selected.");

                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

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
            OnPropertyChanged(nameof(Troop));
            OnPropertyChanged(nameof(IsSelected));
        }
    }
}