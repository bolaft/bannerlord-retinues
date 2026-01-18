using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace Retinues.Interface.Widgets
{
    /// <summary>
    /// ButtonWidget that scrolls the nearest parent ScrollablePanel to an edge (top or bottom) when clicked.
    /// </summary>
    public class ScrollToEdgeButtonWidget(UIContext context) : ButtonWidget(context)
    {
        [Editor(false)]
        public bool ScrollToBottom { get; set; }

        [Editor(false)]
        public float InterpolationTime { get; set; } = 0.2f;

        /// <summary>
        /// Handles click: preserves vanilla click behavior then scrolls the parent panel.
        /// </summary>
        protected override void HandleClick()
        {
            // Keep vanilla click behavior (state changes + event firing).
            base.HandleClick();

            // Find the nearest ancestor panel. If we're not inside a ScrollablePanel, do nothing.
            var panel = FindParentScrollablePanel(this);
            if (panel == null)
                return;

            // We scroll via the panel's vertical scrollbar. If the panel has no scrollbar, do nothing.
            var scrollbar = panel.VerticalScrollbar;
            if (scrollbar == null)
                return;

            // Determine target edge.
            var target = ScrollToBottom ? scrollbar.MaxValue : scrollbar.MinValue;

            // If interpolation time is effectively zero, jump immediately to the value.
            // Otherwise use the panel helper which animates to the target.
            if (InterpolationTime <= float.Epsilon)
                scrollbar.ValueFloat = target;
            else
                panel.SetVerticalScrollTarget(target, InterpolationTime);
        }

        /// <summary>
        /// Walks up the widget tree to find the closest ScrollablePanel.
        /// Returns null if none exists.
        /// </summary>
        private static ScrollablePanel FindParentScrollablePanel(Widget widget)
        {
            var current = widget;
            while (current != null)
            {
                if (current is ScrollablePanel scrollablePanel)
                    return scrollablePanel;

                current = current.ParentWidget;
            }

            return null;
        }
    }
}
