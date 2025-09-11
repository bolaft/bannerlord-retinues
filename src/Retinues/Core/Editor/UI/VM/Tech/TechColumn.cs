using System.Linq;
using TaleWorlds.Library;

namespace Retinues.Core.Editor.UI.VM.Tech
{
    public sealed class TechColumnVM : ViewModel, IView
    {
        // =========================================================================
        // Fields
        // =========================================================================

        public int Index { get; }

        public MBBindingList<TechNodeVM> Items { get; } = [];

        public TechColumnVM(int index) => Index = index;

        // =========================================================================
        // Public API
        // =========================================================================

        public void SetItems(System.Collections.Generic.IEnumerable<TechNodeVM> nodes)
        {
            Items.Clear();
            foreach (var n in nodes.OrderBy(n => n.Definition.Row))
                Items.Add(n);
            OnPropertyChanged(nameof(Items));
        }

        public void Refresh()
        {
            foreach (var n in Items) n.Refresh();
            OnPropertyChanged(nameof(Items));
        }
    }
}
