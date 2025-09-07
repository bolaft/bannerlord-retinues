using System.Collections.Generic;
using TaleWorlds.Library;
using Bannerlord.UIExtenderEx.Attributes;

namespace CustomClanTroops.UI.VM
{
    public abstract class BaseRow<TList, TRow>(TList rowList) : ViewModel
        where TList : BaseList<TList, TRow>
        where TRow : BaseRow<TList, TRow>
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private bool _isSelected;

        protected readonly TList _rowList = rowList;

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

                    // Specific row selection logic
                    if (value) OnSelect();
                    else OnUnselect();

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

        public TList RowList => _rowList;

        public List<TRow> Rows => _rowList.Rows;

        public void Select()
        {
            // No-op if already selected
            if (IsSelected)
                return;

            // Safe due to self-referential generic constraint
            _rowList.Select((TRow)this);
        }

        public void Unselect()
        {
            if (!IsSelected)
                return;

            IsSelected = false;
        }

        // =========================================================================
        // Placeholders
        // =========================================================================

        protected virtual void OnSelect() { }

        protected virtual void OnUnselect() { }
    }
}
