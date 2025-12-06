using Bannerlord.UIExtenderEx.Attributes;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Library;

namespace Retinues.Editor.VM
{
    /// <summary>
    /// Simple sort button viewmodel used by the editor list.
    /// Handles its own sort state (Default/Ascending/Descending)
    /// and notifies the owning ListVM when clicked.
    /// </summary>
    public sealed class SortButtonVM : ViewModel
    {
        private readonly ListVM _owner;

        private string _id;
        private string _text;
        private int _requestedWidth;
        private int _width;
        private bool _isSelected;
        private CampaignUIHelper.SortState _sortState;

        // Shape flags: regular (middle) vs last column (rounded right edge).
        private bool _isLastColumn;
        private bool _isRegularColumn;

        public SortButtonVM(ListVM owner, string id, string text, int requestedWidth)
        {
            _owner = owner;
            _id = id;
            _text = text;
            _requestedWidth = requestedWidth <= 0 ? 1 : requestedWidth;
            _width = _requestedWidth;
            _sortState = CampaignUIHelper.SortState.Default;
            _isSelected = false;

            _isLastColumn = false;
            _isRegularColumn = true;
        }

        internal int RequestedWidth => _requestedWidth;

        [DataSourceProperty]
        public string Id
        {
            get => _id;
            set
            {
                if (value == _id)
                    return;
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        [DataSourceProperty]
        public string Text
        {
            get => _text;
            set
            {
                if (value == _text)
                    return;
                _text = value;
                OnPropertyChanged(nameof(Text));
            }
        }

        /// <summary>
        /// Normalized width that the template binds as SuggestedWidth.
        /// </summary>
        [DataSourceProperty]
        public int Width
        {
            get => _width;
            private set
            {
                if (value == _width)
                    return;
                _width = value;
                OnPropertyChanged(nameof(Width));
            }
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            private set
            {
                if (value == _isSelected)
                    return;
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        [DataSourceProperty]
        public CampaignUIHelper.SortState SortState
        {
            get => _sortState;
            private set
            {
                if (value == _sortState)
                    return;
                _sortState = value;
                OnPropertyChanged(nameof(SortState));
            }
        }

        /// <summary>
        /// True for the last sort column (rounded right corner brush).
        /// </summary>
        [DataSourceProperty]
        public bool IsLastColumn
        {
            get => _isLastColumn;
            private set
            {
                if (value == _isLastColumn)
                    return;
                _isLastColumn = value;
                OnPropertyChanged(nameof(IsLastColumn));
            }
        }

        /// <summary>
        /// True for non-last (regular) columns.
        /// </summary>
        [DataSourceProperty]
        public bool IsRegularColumn
        {
            get => _isRegularColumn;
            private set
            {
                if (value == _isRegularColumn)
                    return;
                _isRegularColumn = value;
                OnPropertyChanged(nameof(IsRegularColumn));
            }
        }

        internal void SetNormalizedWidth(int width)
        {
            if (width <= 0)
                width = 1;
            Width = width;
        }

        /// <summary>
        /// Mark this button as being the last column (rounded brush) or not.
        /// </summary>
        internal void SetIsLastColumn(bool isLast)
        {
            IsLastColumn = isLast;
            IsRegularColumn = !isLast;
        }

        internal void ResetSortState()
        {
            SortState = CampaignUIHelper.SortState.Default;
            IsSelected = false;
        }

        /// <summary>
        /// Cycle this button's sort state: Default → Asc → Desc → Default.
        /// </summary>
        internal void CycleSortState()
        {
            switch (SortState)
            {
                case CampaignUIHelper.SortState.Default:
                    SortState = CampaignUIHelper.SortState.Ascending;
                    IsSelected = true;
                    break;

                case CampaignUIHelper.SortState.Ascending:
                    SortState = CampaignUIHelper.SortState.Descending;
                    IsSelected = true;
                    break;

                default:
                    SortState = CampaignUIHelper.SortState.Default;
                    IsSelected = false;
                    break;
            }
        }

        [DataSourceMethod]
        public void ExecuteToggleSort()
        {
            _owner?.OnSortButtonClicked(this);
        }
    }
}
