using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace Retinues.GUI.Widgets
{
    public class ScrollToEdgeButtonWidget(UIContext context) : ButtonWidget(context)
    {
        [Editor(false)]
        public bool ScrollToBottom { get; set; }

        [Editor(false)]
        public float InterpolationTime { get; set; } = 0.2f;

        protected override void HandleClick()
        {
            // Keep vanilla click behavior (state changes + event firing)
            base.HandleClick();

            var panel = FindParentScrollablePanel(this);
            if (panel == null)
                return;

            var scrollbar = panel.VerticalScrollbar;
            if (scrollbar == null)
                return;

            var target = ScrollToBottom ? scrollbar.MaxValue : scrollbar.MinValue;

            if (InterpolationTime <= float.Epsilon)
                scrollbar.ValueFloat = target;
            else
                panel.SetVerticalScrollTarget(target, InterpolationTime);
        }

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
