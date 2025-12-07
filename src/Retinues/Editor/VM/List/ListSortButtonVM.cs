using Bannerlord.UIExtenderEx.Attributes;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                      Sort Button                      //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Header sort button with three-state sort (none, asc, desc).
    /// </summary>
    public class ListSortButtonVM(ListVM list, string id, string text, int requestedWidth)
        : BaseStatefulVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly ListVM _list = list;

        private string _id = id;
        private string _text = text;
        private int _requestedWidth = requestedWidth;

        private int _normalizedWidth;
        private int _width;
        private bool _isLastColumn;

        // 0 = none, 1 = ascending, 2 = descending.
        private int _sortStateIndex;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Accessors                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal ListVM List => _list;

        [DataSourceProperty]
        public string Id
        {
            get => _id;
            private set
            {
                if (value == _id)
                {
                    return;
                }

                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

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
        public bool IsSortedAscending => _sortStateIndex == 1;

        [DataSourceProperty]
        public bool IsSortedDescending => _sortStateIndex == 2;

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
            _sortStateIndex = (_sortStateIndex + 1) % 3;

            OnPropertyChanged(nameof(IsSortedAscending));
            OnPropertyChanged(nameof(IsSortedDescending));
        }

        internal void ResetSortState()
        {
            if (_sortStateIndex == 0)
            {
                return;
            }

            _sortStateIndex = 0;

            OnPropertyChanged(nameof(IsSortedAscending));
            OnPropertyChanged(nameof(IsSortedDescending));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Commands                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteClick()
        {
            _list?.OnSortButtonClicked(this);
        }
    }
}
