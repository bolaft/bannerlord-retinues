using System;
using System.Linq;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List
{
    public class ListVM : ViewModel
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //   Headers / Selection as before
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private MBBindingList<ListHeaderVM> _headers;
        private ListRowVM _selectedElement;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //            Sorting
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private MBBindingList<ListSortButtonVM> _sortButtons;

        // Total normalized width used by sort buttons (matches template).
        private const int SortButtonsTotalWidth = 586;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //        Pagination / Filter
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private string _filterText;

        public ListVM()
        {
            _headers = [];
            _sortButtons = [];
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
            }
        }

        [DataSourceProperty]
        public ListRowVM SelectedElement
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
            // Insert at the top because the list is displayed in reverse
            _headers.Insert(0, header);
            return header;
        }

        public void Clear()
        {
            _headers.Clear();
            SelectedElement = null;
        }

        internal void OnElementSelected(ListRowVM element)
        {
            foreach (var header in _headers)
                header.ClearSelectionExcept(element);

            SelectedElement = element;
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            foreach (var header in _headers)
                header.RefreshValues();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //           Sort Buttons
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [DataSourceProperty]
        public MBBindingList<ListSortButtonVM> SortButtons
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
        public ListSortButtonVM AddSortButton(string id, string text, int relativeWidth)
        {
            var vm = new ListSortButtonVM(this, id, text, relativeWidth);
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
        internal void OnSortButtonClicked(ListSortButtonVM clicked)
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

            OnSortChanged();
        }

        private void OnSortChanged()
        {
            // Stub: actual sorting not implemented yet.
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
        }
    }
}
