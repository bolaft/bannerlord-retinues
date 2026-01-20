using System.Linq;
using Retinues.Editor.MVC.Shared.Views;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Pages.Equipment.Views.List
{
    public class EquipmentListHeaderVM(BaseListVM list, string id, string name)
        : ListHeaderVM(list, id, name)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Bindings                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public override bool IsVisible =>
            HasVisibleRows && List.Headers.Count(h => h.HasVisibleRows) > 1;

        [DataSourceProperty]
        public override bool IsEnabled => true;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override bool CollapseWhenNotVisible => false;

        protected override bool ForceExpandedWhenNotVisible => true;

        // Keep the built ExpandedRows list cached to make expand/collapse instant.
        protected override bool CacheExpandedRowsWhenCollapsed => true;
    }
}
