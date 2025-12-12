using System.Linq;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List.Equipment
{
    public class EquipmentListHeaderVM(ListVM list, string id, string name)
        : ListHeaderVM(list, id, name)
    {
        // Show the header toggle only if:
        // - this header has visible rows, AND
        // - there are at least 2 "full" equipment headers
        [DataSourceProperty]
        public override bool IsVisible =>
            HasVisibleRows && List.Headers.Count(h => h.HasVisibleRows) > 1;

        // Equipment headers should never be disabled.
        [DataSourceProperty]
        public override bool IsEnabled => true;

        // Important: IsVisible now only hides the toggle, so do NOT auto-collapse.
        protected override bool CollapseWhenNotVisible => false;

        // If the toggle is hidden but we have rows, keep the section expanded so rows show.
        protected override bool ForceExpandedWhenNotVisible => true;
    }
}
