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
                    return;

                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
                OnPropertyChanged(nameof(MarginBottom));
            }
        }

        [DataSourceProperty]
        public virtual bool IsVisible => true;

        [DataSourceProperty]
        public virtual bool IsEnabled => VisibleRowCount > 0;

        internal bool HasVisibleRows => VisibleRowCount > 0;
        protected virtual bool CollapseWhenNotVisible => true;
        protected virtual bool ForceExpandedWhenNotVisible => false;

        public void UpdateState()
        {
            OnPropertyChanged(nameof(IsVisible));
            OnPropertyChanged(nameof(IsEnabled));

            // Default behavior: collapse when disabled, and (optionally) when hidden.
            if ((!IsEnabled || (CollapseWhenNotVisible && !IsVisible)) && _isExpanded)
                IsExpanded = false;

            // Equipment special-case: when toggle is hidden but rows exist, keep the section open.
            if (ForceExpandedWhenNotVisible && HasVisibleRows && !IsVisible && !_isExpanded)
                IsExpanded = true;
        }

        internal void OnRowVisibilityChanged()
        {
            OnPropertyChanged(nameof(RowCountText));

            // Row visibility changes affect "how many headers are full",
            // so other headers must refresh their IsVisible too.
            List?.OnHeaderRowVisibilityChanged();

            UpdateState();
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

        protected int VisibleRowCount
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
    }
}
