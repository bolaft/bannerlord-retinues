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
        private readonly ListVM _list = list;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       IsSelected                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        private bool _isSelected;

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Sort State                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // 0 = none, 1 = ascending, 2 = descending.
        private int _sortStateIndex;

        public bool IsSortedAscending => _sortStateIndex == 1;
        public bool IsSortedDescending => _sortStateIndex == 2;

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

        [DataSourceMethod]
        public void ExecuteToggleSort()
        {
            _list?.OnSortButtonClicked(this);
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
        //                           Key                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal readonly ListSortKey SortKey = sortKey;

        [DataSourceProperty]
        public string Id => SortKey.ToString();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Text                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private string _text = text;

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Width                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public int RequestedWidth = requestedWidth;
        private int _width;

        [DataSourceProperty]
        public float Width
        {
            get => _width;
            set
            {
                // convert float coming from Gauntlet back to int
                var intWidth = (int)value;
                if (intWidth <= 0)
                {
                    intWidth = 1;
                }

                if (intWidth == _width)
                {
                    return;
                }

                _width = intWidth;
                OnPropertyChanged(nameof(Width));
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      IsLastColumn                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool _isLastColumn;

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

        internal void SetIsLastColumn(bool isLast)
        {
            IsLastColumn = isLast;
        }
    }
}
