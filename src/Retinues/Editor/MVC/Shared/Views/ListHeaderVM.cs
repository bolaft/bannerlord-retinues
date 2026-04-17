using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Events;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Shared.Views
{
    /// <summary>
    /// Collapsible header that groups list rows.
    /// </summary>
    public class ListHeaderVM(BaseListVM list, string id, string name) : EventListenerVM
    {
        internal BaseListVM List = list;

        private readonly string _id = id;

        [DataSourceProperty]
        public string Id => _id;

        private string _name = name;

        [DataSourceProperty]
        public string Name
        {
            get => _name;
            private set
            {
                if (value == _name)
                    return;

                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Rows                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly List<BaseListRowVM> _rows = [];
        public List<BaseListRowVM> Rows => _rows;

        // We expose ExpandedRows as the ItemsSource in Gauntlet.
        // To make expand/collapse fast, we swap the entire binding list reference:
        // - expanded => cached populated list (built once)
        // - collapsed => empty list
        private readonly MBBindingList<BaseListRowVM> _expandedRowsEmpty = [];
        private MBBindingList<BaseListRowVM> _expandedRowsBuilt;
        private MBBindingList<BaseListRowVM> _expandedRows;

        [DataSourceProperty]
        public MBBindingList<BaseListRowVM> ExpandedRows
        {
            get => _expandedRows;
            private set
            {
                if (ReferenceEquals(value, _expandedRows))
                    return;

                _expandedRows = value;
                OnPropertyChanged(nameof(ExpandedRows));
            }
        }

        /// <summary>
        /// If true, we keep the built list when collapsed (swap to empty list only).
        /// If false, collapsing discards the built list (frees memory).
        /// </summary>
        protected virtual bool CacheExpandedRowsWhenCollapsed => false;

        private void EnsureExpandedRowsBuilt()
        {
            if (_expandedRowsBuilt != null)
                return;

            var built = new MBBindingList<BaseListRowVM>();

            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                if (row != null)
                    built.Add(row);
            }

            _expandedRowsBuilt = built;
        }

        public void AddRow(BaseListRowVM row)
        {
            if (row == null)
                return;

            _rows.Add(row);

            // If we already built the cached list, keep it in sync.
            _expandedRowsBuilt?.Add(row);

            // If expanded and we're showing the built list, also ensure the current ExpandedRows has it.
            // (Usually redundant since _expandedRowsBuilt is the expanded source.)
        }

        internal bool ContainsSelectedRow()
        {
            if (_rows.Count == 0)
                return false;

            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                if (row != null && row.IsSelected)
                    return true;
            }

            return false;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Expanded / Toggle                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool _isExpanded;

        [DataSourceProperty]
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (value == _isExpanded)
                    return;

                _isExpanded = value;

                if (_isExpanded)
                {
                    EnsureExpandedRowsBuilt();
                    ExpandedRows = _expandedRowsBuilt;
                }
                else
                {
                    // Hide rows by swapping to empty items source.
                    ExpandedRows = _expandedRowsEmpty;

                    if (!CacheExpandedRowsWhenCollapsed)
                        _expandedRowsBuilt = null;
                }

                OnPropertyChanged(nameof(IsExpanded));
                OnPropertyChanged(nameof(CollapseIndicatorState));
                OnPropertyChanged(nameof(MarginBottom));
            }
        }

        [DataSourceProperty]
        public string CollapseIndicatorState => _isExpanded ? "Expanded" : "Collapsed";

        [DataSourceMethod]
        public void ExecuteToggle()
        {
            IsExpanded = !IsExpanded;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Visible / Enabled                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public virtual bool IsVisible => true;

        [DataSourceProperty]
        public virtual bool IsEnabled => HasAnyVisibleRows;

        internal bool HasVisibleRows => VisibleRowCount > 0;

        private bool HasAnyVisibleRows
        {
            get
            {
                for (int i = 0; i < _rows.Count; i++)
                {
                    var row = _rows[i];
                    if (row != null && row.IsVisible)
                        return true;
                }
                return false;
            }
        }
        protected virtual bool CollapseWhenNotVisible => true;
        protected virtual bool ForceExpandedWhenNotVisible => false;

        public void UpdateState()
        {
            OnPropertyChanged(nameof(IsVisible));
            OnPropertyChanged(nameof(IsEnabled));

            if ((!IsEnabled || (CollapseWhenNotVisible && !IsVisible)) && _isExpanded)
                IsExpanded = false;

            if (ForceExpandedWhenNotVisible && HasVisibleRows && !IsVisible && !_isExpanded)
                IsExpanded = true;
        }

        internal void OnRowVisibilityChanged()
        {
            OnPropertyChanged(nameof(RowCountText));
            List.RecomputeHeaderStates();
            UpdateState();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Margins                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public int MarginBottom => _isExpanded ? 0 : 3;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Row Count                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string RowCountText => $"({VisibleRowCount})";

        protected int VisibleRowCount
        {
            get
            {
                if (_rows.Count == 0)
                    return 0;

                var count = 0;

                for (int i = 0; i < _rows.Count; i++)
                {
                    var row = _rows[i];
                    if (row != null && row.IsVisible && !row.IsUnlockHint)
                        count++;
                }

                return count;
            }
        }

        public void UpdateRowCount() => OnPropertyChanged(nameof(RowCountText));
    }
}
