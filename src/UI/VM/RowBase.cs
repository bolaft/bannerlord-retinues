using TaleWorlds.Library;
using Bannerlord.UIExtenderEx.Attributes;

namespace CustomClanTroops.UI.VM
{
    public abstract class RowBase<TList, TRow>(TList owner) : ViewModel
        where TList : ListBase<TList, TRow>
        where TRow  : RowBase<TList, TRow>
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private bool _isSelected;

        protected readonly TList _owner = owner;

        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        // =========================================================================
        // Action Bindings
        // =========================================================================

        [DataSourceMethod]
        public void ExecuteSelect() => Select();

        // =========================================================================
        // Public API
        // =========================================================================

        public void Select()
        {
            // Safe due to self-referential generic constraint
            _owner.Select((TRow)this);
        }
    }
}
