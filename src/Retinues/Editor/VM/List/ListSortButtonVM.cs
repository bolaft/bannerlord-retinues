using Bannerlord.UIExtenderEx.Attributes;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List
{
    /// <summary>
    /// Header sort button with three-state sort (none, asc, desc).
    /// </summary>
    public class ListSortButtonVM(ListVM list, ListSortKey sortKey, string text, int requestedWidth)
        : BaseStatefulVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly ListVM _list = list;

        private string _text = text;
        private int _requestedWidth = requestedWidth;

        private int _normalizedWidth;
        private int _width;
        private bool _isLastColumn;
        private bool _isSelected;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Accessors                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal readonly ListSortKey SortKey = sortKey;

        [DataSourceProperty]
        public string Id => SortKey.ToString();

        [DataSourceProperty]
        public string Text
        {
            get => _text;
            private set
            {
                if (value == _text)
                {
                    return;
                }

                _text = value;
                OnPropertyChanged(nameof(Text));
            }
        }

        /// <summary>
        /// Relative width used for normalization.
        /// </summary>
        [DataSourceProperty]
        public int RequestedWidth
        {
            get => _requestedWidth;
            private set
            {
                if (value == _requestedWidth)
                {
                    return;
                }

                _requestedWidth = value;
                OnPropertyChanged(nameof(RequestedWidth));
            }
        }

        /// <summary>
        /// Normalized width (kept for internal use and debugging).
        /// </summary>
        [DataSourceProperty]
        public int NormalizedWidth
        {
            get => _normalizedWidth;
            private set
            {
                if (value == _normalizedWidth)
                {
                    return;
                }

                _normalizedWidth = value;
                OnPropertyChanged(nameof(NormalizedWidth));
            }
        }

        /// <summary>
        /// Bound by the template as SuggestedWidth.
        /// </summary>
        [DataSourceProperty]
        public int Width
        {
            get => _width;
            private set
            {
                if (value == _width)
                {
                    return;
                }

                _width = value;
                OnPropertyChanged(nameof(Width));
            }
        }

        [DataSourceProperty]
        public bool IsLastColumn
        {
            get => _isLastColumn;
            private set
            {
                if (value == _isLastColumn)
                {
                    return;
                }

                _isLastColumn = value;
                OnPropertyChanged(nameof(IsLastColumn));
            }
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            private set
            {
                if (value == _isSelected)
                {
                    return;
                }

                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public bool IsSortedAscending => _sortStateIndex == 1;
        public bool IsSortedDescending => _sortStateIndex == 2;

        // 0 = none, 1 = ascending, 2 = descending.
        private int _sortStateIndex;

        [DataSourceProperty]
        public int SortState
        {
            get => _sortStateIndex;
            private set
            {
                if (value == _sortStateIndex)
                {
                    return;
                }

                _sortStateIndex = value;

                OnPropertyChanged(nameof(SortState));

                // Selected only when there is an active sort.
                IsSelected = _sortStateIndex != 0;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal void SetNormalizedWidth(int width)
        {
            if (width <= 0)
            {
                width = 1;
            }

            NormalizedWidth = width;
            Width = width;
        }

        internal void SetIsLastColumn(bool isLast)
        {
            IsLastColumn = isLast;
        }

        internal void CycleSortState()
        {
            SortState = (_sortStateIndex + 1) % 3;
        }

        internal void ResetSortState()
        {
            if (_sortStateIndex == 0)
            {
                return;
            }

            SortState = 0;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Commands                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteToggleSort()
        {
            _list?.OnSortButtonClicked(this);
        }
    }
}
