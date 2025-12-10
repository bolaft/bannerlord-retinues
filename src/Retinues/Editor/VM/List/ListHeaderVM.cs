using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.VM.List.Character;
using Retinues.Model.Characters;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List
{
    /// <summary>
    /// Collapsible header that groups list rows.
    /// </summary>
    public class ListHeaderVM(ListVM list, string id, string name) : BaseVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal ListVM List = list;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Identifier                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly string _id = id;

        [DataSourceProperty]
        public string Id => _id;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Name                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private string _name = name;

        [DataSourceProperty]
        public string Name
        {
            get => _name;
            private set
            {
                if (value == _name)
                {
                    return;
                }

                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Rows                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly MBBindingList<ListRowVM> _rows = [];

        [DataSourceProperty]
        public MBBindingList<ListRowVM> Rows => _rows;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Enabled / Expanded                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool _isExpanded;

        [DataSourceProperty]
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (value == _isExpanded)
                {
                    return;
                }

                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
                OnPropertyChanged(nameof(MarginBottom));
            }
        }

        [DataSourceProperty]
        public bool IsEnabled => VisibleRowCount > 0;

        public void UpdateIsEnabledState()
        {
            OnPropertyChanged(nameof(IsEnabled));

            if (!IsEnabled && _isExpanded)
            {
                IsExpanded = false;
            }
        }

        internal void OnRowVisibilityChanged()
        {
            OnPropertyChanged(nameof(RowCountText));
            UpdateIsEnabledState();
        }

        [DataSourceMethod]
        public void ExecuteToggle()
        {
            IsExpanded = !IsExpanded;
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

        private int VisibleRowCount
        {
            get
            {
                if (_rows.Count == 0)
                {
                    return 0;
                }

                var count = 0;

                for (int i = 0; i < _rows.Count; i++)
                {
                    var row = _rows[i];
                    if (row != null && row.IsVisible)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void UpdateRowCount()
        {
            OnPropertyChanged(nameof(RowCountText));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal void ClearSelectionExcept(ListRowVM selected)
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                if (row == null)
                {
                    continue;
                }

                row.IsSelected = ReferenceEquals(row, selected);
            }
        }
    }
}
