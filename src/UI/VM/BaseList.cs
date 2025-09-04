using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace CustomClanTroops.UI.VM
{
    public abstract class BaseList<TSelf, TRow>(UI.ClanScreen screen) : ViewModel
        where TSelf : BaseList<TSelf, TRow>
        where TRow : BaseRow<TSelf, TRow>
    {
        // =========================================================================
        // Fields
        // =========================================================================

        protected readonly UI.ClanScreen _screen = screen;

        // =========================================================================
        // Public API
        // =========================================================================

        public UI.ClanScreen Screen => _screen;

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
