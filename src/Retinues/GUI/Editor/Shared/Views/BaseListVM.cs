using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.GUI.Components;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Services;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.Shared.Views
{
    /// <summary>
    /// Keys for sorting list rows.
    /// </summary>
    public enum ListSortKey
    {
        Name,
        Tier,
        Value,
        Culture,
    }

    /// <summary>
    /// Shared list ViewModel base. Concrete lists (Character/Equipment/Library/Doctrines)
    /// implement Build() to populate SortButtons and Headers.
    /// </summary>
    public abstract partial class BaseListVM : EventListenerVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Lifecycle                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// The page this list VM is responsible for.
        /// </summary>
        protected abstract EditorPage Page { get; }

        /// <summary>
        /// Rebuilds the list (sort buttons + headers + rows).
        /// Concrete VMs also call RecomputeHeaderStates() inside their Build().
        /// </summary>
        public abstract void Build();

        /// <summary>
        /// Hook called after a successful activation build (page switched to this list).
        /// </summary>
        protected virtual void AfterBuildOnActivate() { }

        /// <summary>
        /// Clears all headers and their rows from the list.
        /// </summary>
        public void Clear()
        {
            SetHeaders([]);
        }

        /// <summary>
        /// On page change, rebuild the list if this VM is now active.
        /// </summary>
        [EventListener(UIEvent.Page)]
        protected void OnPageChange()
        {
            if (State.Page != Page)
                return;

            AutoScrollRowsEnabled = true;
            AutoScrollVersion++;

            Build();
            AfterBuildOnActivate();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Filter Tooltips                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Builds the filter tooltip shown above the list.
        /// Override in concrete lists for page-specific descriptions.
        /// </summary>
        protected virtual Tooltip GetFilterTooltip() =>
            new(L.S("filter_tooltip_description", "Type to filter the list."));
    }
}
