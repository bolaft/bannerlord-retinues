using System.Collections.Generic;
using System.Linq;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor
{
    public abstract class BaseList<TSelf, TRow> : BaseComponent
        where TSelf : BaseList<TSelf, TRow>
        where TRow : BaseRow<TSelf, TRow>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private string _filterText;

        [DataSourceProperty]
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (_filterText == value)
                    return;
                _filterText = value;
                foreach (var row in Rows)
                    row.UpdateIsVisible(_filterText);
                OnPropertyChanged(nameof(FilterText));
            }
        }

        [DataSourceProperty]
        public string FilterLabel => L.S("item_search_label", "Filter:");

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public virtual MBBindingList<TRow> Rows { get; protected set; } = [];

        public TRow Selection => Rows.FirstOrDefault(r => r.IsSelected);

        /// <summary>
        /// Selects the given row, deselecting all others.
        /// </summary>
        public void Select(TRow row)
        {
            if (row is null || !Rows.Contains(row))
                return;

            foreach (var r in Rows)
                r.IsSelected = ReferenceEquals(r, row);

            OnPropertyChanged(nameof(Selection));
        }
    }
}
