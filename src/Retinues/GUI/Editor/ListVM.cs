using System.Collections.Generic;
using System.Linq;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor
{
    public abstract class ListVM : BaseVM
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
                foreach (var row in Rows)
                    row.ApplyFilter(_filterText);
                OnPropertyChanged(nameof(FilterText));
            }
        }

        [DataSourceProperty]
        public string FilterLabel => L.S("item_search_label", "Filter:");

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public abstract List<ListElementVM> Rows { get; }

        public void RefreshFilter()
        {
            Log.Info($"Refreshing list filter with text: '{FilterText}'");
            foreach (var row in Rows)
                row.ApplyFilter(FilterText);
        }
    }
}
