using System.Linq;
using Retinues.Editor.MVC.Shared.Views;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Pages.Equipment.Views.List
{
    /// <summary>
    /// Header ViewModel for the equipment list.
    /// Controls visibility and expansion behavior for a header group.
    /// </summary>
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

        /// <summary>
        /// Keeps the header stable even when it has no visible rows.
        /// </summary>
        protected override bool CollapseWhenNotVisible => false;

        /// <summary>
        /// Forces headers to remain expanded when hidden to reduce UI churn.
        /// </summary>
        protected override bool ForceExpandedWhenNotVisible => true;

        /// <summary>
        /// Caches expanded rows to make expand/collapse instant.
        /// </summary>
        protected override bool CacheExpandedRowsWhenCollapsed => true;
    }
}
