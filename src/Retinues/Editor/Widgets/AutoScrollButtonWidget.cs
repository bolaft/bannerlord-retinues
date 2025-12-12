using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace Retinues.Editor.Widgets
{
    /// <summary>
    /// ButtonWidget that auto-scrolls itself into view when it becomes selected.
    /// </summary>
    public class AutoScrollButtonWidget : ButtonWidget
    {
        private bool _pendingScroll;
        private ScrollablePanel _parentPanel;

        [Editor(false)]
        public float AutoScrollTopOffset { get; set; }

        [Editor(false)]
        public float AutoScrollBottomOffset { get; set; }

        [Editor(false)]
        public float AutoScrollLeftOffset { get; set; }

        [Editor(false)]
        public float AutoScrollRightOffset { get; set; }

        /// <summary>
        /// -1 uses panel default behavior. 0 = top/left, 0.5 = center, 1 = bottom/right (depends on implementation).
        /// </summary>
        [Editor(false)]
        public float AutoScrollHorizontalTarget { get; set; } = -1f;

        [Editor(false)]
        public float AutoScrollVerticalTarget { get; set; } = -1f;

        [Editor(false)]
        public float AutoScrollInterpolationTime { get; set; } = 0.08f;

        public AutoScrollButtonWidget(UIContext context)
            : base(context)
        {
            boolPropertyChanged += OnBoolPropertyChanged;
        }

        protected override void OnConnectedToRoot()
        {
            base.OnConnectedToRoot();
            _parentPanel ??= FindParentScrollablePanel();
        }

        protected override void OnDisconnectedFromRoot()
        {
            base.OnDisconnectedFromRoot();
            _parentPanel = null;
            _pendingScroll = false;
        }

        protected override void OnLateUpdate(float dt)
        {
            base.OnLateUpdate(dt);

            if (!_pendingScroll)
                return;

            _parentPanel ??= FindParentScrollablePanel();
            if (_parentPanel == null || IsHidden || !IsVisible)
                return;

            _pendingScroll = false;

            var p = new ScrollablePanel.AutoScrollParameters(
                topOffset: AutoScrollTopOffset,
                bottomOffset: AutoScrollBottomOffset,
                leftOffset: AutoScrollLeftOffset,
                rightOffset: AutoScrollRightOffset,
                horizontalScrollTarget: AutoScrollHorizontalTarget,
                verticalScrollTarget: AutoScrollVerticalTarget,
                interpolationTime: AutoScrollInterpolationTime
            );

            _parentPanel.ScrollToChild(this, p);
        }

        private void OnBoolPropertyChanged(
            PropertyOwnerObject owner,
            string propertyName,
            bool value
        )
        {
            if (!ReferenceEquals(owner, this))
                return;

            if (propertyName == "IsSelected" && value)
                _pendingScroll = true;
        }

        private ScrollablePanel FindParentScrollablePanel()
        {
            Widget current = ParentWidget;
            while (current != null)
            {
                if (current is ScrollablePanel sp)
                    return sp;

                current = current.ParentWidget;
            }

            return null;
        }
    }
}
