using System.Collections.Generic;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor
{
    /// <summary>
    /// ViewModel for filterable lists of rows.
    /// </summary>
    public abstract class BaseListVM : BaseVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private string _filterText = string.Empty;

        [DataSourceProperty]
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (_filterText == value)
                    return;
                value ??= string.Empty;
                _filterText = value;
                OnFilterTextChanged();
                OnPropertyChanged(nameof(FilterText));
            }
        }

        [DataSourceProperty]
        public string FilterLabel => L.S("item_search_label", "Filter:");

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public abstract List<BaseListElementVM> Rows { get; }

        /// <summary>
        /// Hook for when the filter text changes; default implementation filters rows directly.
        /// </summary>
        protected virtual void OnFilterTextChanged()
        {
            foreach (var row in Rows)
                row.ApplyFilter(_filterText);
        }

        /// <summary>
        /// Reapply the current filter to all list rows.
        /// </summary>
        public virtual void RefreshFilter()
        {
            OnFilterTextChanged();
        }
    }
}
