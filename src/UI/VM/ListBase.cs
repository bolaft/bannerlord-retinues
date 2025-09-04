using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace CustomClanTroops.UI.VM
{
    public abstract class ListBase<TSelf, TRow>(UI.ClanManagementMixinVM owner) : ViewModel
        where TSelf : ListBase<TSelf, TRow>
        where TRow : RowBase<TSelf, TRow>
    {
        // =========================================================================
        // Fields
        // =========================================================================

        protected readonly UI.ClanManagementMixinVM _owner = owner;

        // =========================================================================
        // Public API
        // =========================================================================

        public UI.ClanManagementMixinVM Owner => _owner;

        public virtual List<TRow> Rows { get; protected set; } = new();

        public TRow SelectedRow => Rows.FirstOrDefault(r => r.IsSelected);

        public void Select(TRow row)
        {
            foreach (var r in Rows)
                r.IsSelected = ReferenceEquals(r, row);

            OnPropertyChanged(nameof(SelectedRow));
        }
    }
}
