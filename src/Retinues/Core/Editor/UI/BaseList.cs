using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Editor.UI.VM;
using TaleWorlds.Library;

namespace Retinues.Core.Editor.UI
{
    /// <summary>
    /// Base class for editor list view models. Provides access to the editor screen, rows, and selection logic.
    /// </summary>
    public abstract class BaseList<TSelf, TRow>(EditorScreenVM screen) : ViewModel
        where TSelf : BaseList<TSelf, TRow>
        where TRow : BaseRow<TSelf, TRow>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected readonly EditorScreenVM _screen = screen;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public EditorScreenVM Screen => _screen;

        public virtual List<TRow> Rows { get; protected set; } = [];

        public TRow SelectedRow => Rows.FirstOrDefault(r => r.IsSelected);

        public void Select(TRow row)
        {
            foreach (var r in Rows)
                r.IsSelected = ReferenceEquals(r, row);

            OnPropertyChanged(nameof(SelectedRow));
        }
    }
}
