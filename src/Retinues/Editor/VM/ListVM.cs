using System;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Utilities;
using TaleWorlds.Library;

namespace Retinues.Editor.VM
{
    public class ListVM : ViewModel
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //   Headers / Selection as before
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private MBBindingList<ListHeaderVM> _headers;
        private ListElementVM _selectedElement;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //            Sorting
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private MBBindingList<SortButtonVM> _sortButtons;

        // Total normalized width used by sort buttons (matches template).
        private const int SortButtonsTotalWidth = 586;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //        Pagination / Filter
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private int _currentPageIndex;
        private int _totalPages = 1;
        private int _pageSize = 100;

        private string _filterText;

        public ListVM()
        {
            _headers = [];
            _sortButtons = [];

            _currentPageIndex = 0;
            _totalPages = 1;
            _filterText = string.Empty;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //            Headers
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [DataSourceProperty]
        public MBBindingList<ListHeaderVM> Headers
        {
            get => _headers;
            set
            {
                if (value == _headers)
                    return;
                _headers = value;
                OnPropertyChanged(nameof(Headers));
                RecalculatePagination();
            }
        }

        [DataSourceProperty]
        public ListElementVM SelectedElement
        {
            get => _selectedElement;
            set
            {
                if (value == _selectedElement)
                    return;
                _selectedElement = value;
                OnPropertyChanged(nameof(SelectedElement));
            }
        }

        public ListHeaderVM AddHeader(string id, string name)
        {
            var header = new ListHeaderVM(this, id, name);
            _headers.Add(header);
            RecalculatePagination();
            return header;
        }

        public void Clear()
        {
            _headers.Clear();
            SelectedElement = null;
            RecalculatePagination();
        }

        internal void OnElementSelected(ListElementVM element)
        {
            foreach (var header in _headers)
                header.ClearSelectionExcept(element);

            SelectedElement = element;
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(Headers));
            OnPropertyChanged(nameof(SortButtons));
            OnPropertyChanged(nameof(SelectedElement));

            foreach (var header in _headers)
                header.Refresh();

            RecalculatePagination();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            foreach (var header in _headers)
                header.RefreshValues();

            RecalculatePagination();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //           Sort Buttons
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [DataSourceProperty]
        public MBBindingList<SortButtonVM> SortButtons
        {
            get => _sortButtons;
            set
            {
                if (value == _sortButtons)
                    return;
                _sortButtons = value;
                OnPropertyChanged(nameof(SortButtons));
                SetDynamicButtonProperties();
            }
        }

        /// <summary>
        /// Adds a sort button with a relative width that will later be normalized to 588.
        /// </summary>
        public SortButtonVM AddSortButton(string id, string text, int relativeWidth)
        {
            var vm = new SortButtonVM(this, id, text, relativeWidth);
            _sortButtons.Add(vm);
            SetDynamicButtonProperties();
            return vm;
        }

        /// <summary>
        /// Normalize all sort button widths so their sum equals SortButtonsTotalWidth.
        /// </summary>
        private void SetDynamicButtonProperties()
        {
            if (_sortButtons == null || _sortButtons.Count == 0)
                return;

            // Sum requested widths (use 1 as minimum to avoid division by zero).
            var totalRequested = _sortButtons.Sum(b => Math.Max(1, b.RequestedWidth));
            if (totalRequested <= 0)
                totalRequested = _sortButtons.Count;

            var remaining = SortButtonsTotalWidth;

            for (int i = 0; i < _sortButtons.Count; i++)
            {
                var button = _sortButtons[i];
                var request = Math.Max(1, button.RequestedWidth);

                int width;
                if (i == _sortButtons.Count - 1)
                {
                    // Last one gets whatever remains so we always hit exactly SortButtonsTotalWidth.
                    width = Math.Max(1, remaining);
                }
                else
                {
                    var fraction = (double)request / totalRequested;
                    width = (int)System.Math.Round(SortButtonsTotalWidth * fraction);
                    if (width < 1)
                        width = 1;
                    remaining -= width;
                }

                button.SetNormalizedWidth(width);

                // Mark last vs regular so the template can pick the correct brush
                var isLast = i == _sortButtons.Count - 1;
                button.SetIsLastColumn(isLast);
            }
        }

        /// <summary>
        /// Entry point called by SortButtonVM when a button is clicked.
        /// For now this only updates visual state; it does not reorder any rows.
        /// </summary>
        internal void OnSortButtonClicked(SortButtonVM clicked)
        {
            if (clicked == null)
                return;

            foreach (var button in _sortButtons)
            {
                if (ReferenceEquals(button, clicked))
                    button.CycleSortState();
                else
                    button.ResetSortState();
            }

            // Hook for future: actual sort behavior can be wired here.
            // For now, we only update the visual state.
            OnSortChanged();
        }

        private void OnSortChanged()
        {
            // Stub: actual sorting not implemented yet.
            // When you do implement sorting, this is the place to
            // reorder your headers/elements based on SortButtons state.
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //          Pagination
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [DataSourceProperty]
        public string PageLabel => "Page:";

        [DataSourceProperty]
        public string PageText => $"{_currentPageIndex + 1}/{Math.Max(1, _totalPages)}";

        [DataSourceProperty]
        public bool HasPrevPage => _currentPageIndex > 0;

        [DataSourceProperty]
        public bool HasNextPage => _currentPageIndex + 1 < _totalPages;

        /// <summary>
        /// Page size used when computing total pages from current items.
        /// This does not yet actually slice the headers/elements – it's pure state.
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (value <= 0 || value == _pageSize)
                    return;
                _pageSize = value;
                RecalculatePagination();
            }
        }

        private void RecalculatePagination()
        {
            // For now we only compute "virtual" pagination from total row count (headers + elements).
            var totalItems = 0;

            if (_headers != null)
            {
                foreach (var header in _headers)
                {
                    if (header?.Elements != null)
                        totalItems += header.Elements.Count;
                }
            }

            if (totalItems <= 0)
            {
                _totalPages = 1;
                _currentPageIndex = 0;
            }
            else
            {
                _totalPages = (totalItems + _pageSize - 1) / _pageSize;
                if (_currentPageIndex < 0)
                    _currentPageIndex = 0;
                if (_currentPageIndex >= _totalPages)
                    _currentPageIndex = _totalPages - 1;
            }

            OnPaginationChanged();
        }

        private void OnPaginationChanged()
        {
            OnPropertyChanged(nameof(PageText));
            OnPropertyChanged(nameof(HasPrevPage));
            OnPropertyChanged(nameof(HasNextPage));

            // Later: slice visible elements based on _currentPageIndex / PageSize.
        }

        [DataSourceMethod]
        public void ExecutePrevPage()
        {
            if (!HasPrevPage)
                return;

            _currentPageIndex--;
            OnPaginationChanged();

            // Stub: rows are not actually paged yet.
        }

        [DataSourceMethod]
        public void ExecuteNextPage()
        {
            if (!HasNextPage)
                return;

            _currentPageIndex++;
            OnPaginationChanged();

            // Stub: rows are not actually paged yet.
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //              Filter
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [DataSourceProperty]
        public string FilterLabel => "Filter:";

        [DataSourceProperty]
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (value == _filterText)
                    return;
                _filterText = value;
                OnPropertyChanged(nameof(FilterText));

                ApplyFilter();
            }
        }

        /// <summary>
        /// Stub filter method; currently does nothing but is called when FilterText changes.
        /// </summary>
        private void ApplyFilter()
        {
            // Stub: no actual filtering yet.
            // Later: apply FilterText to headers/elements and recalc pagination.
        }
    }
}
