using System;
using System.Collections.Generic;
using Retinues.Utilities;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace Retinues.Editor.Widgets
{
    public class AutoScrollButtonWidget : ButtonWidget
    {
        [Editor(false)]
        public bool AutoScrollEnabled { get; set; } = true;

        [Editor(false)]
        public bool AutoScrollAlways { get; set; } = false;

        [Editor(false)]
        public float AutoScrollTopOffset { get; set; }

        [Editor(false)]
        public float AutoScrollBottomOffset { get; set; }

        [Editor(false)]
        public float AutoScrollLeftOffset { get; set; }

        [Editor(false)]
        public float AutoScrollRightOffset { get; set; }

        [Editor(false)]
        public float AutoScrollHorizontalTarget { get; set; } = -1f;

        [Editor(false)]
        public float AutoScrollVerticalTarget { get; set; } = -1f;

        [Editor(false)]
        public float AutoScrollInterpolationTime { get; set; } = 0.08f;

        private static readonly Dictionary<string, int> LastScrolledVersionByScope = new(
            StringComparer.Ordinal
        );

        private ScrollablePanel _parentPanel;
        private bool _pendingScroll;

        private int _autoScrollVersion;
        private string _autoScrollScope = "EditorList";

        [Editor(false)]
        public int AutoScrollVersion
        {
            get => _autoScrollVersion;
            set
            {
                if (value == _autoScrollVersion)
                    return;

                _autoScrollVersion = value;

                // If we are selected when the version changes, request a scroll.
                RequestScrollIfEligible();
            }
        }

        [Editor(false)]
        public string AutoScrollScope
        {
            get => _autoScrollScope;
            set
            {
                var next = string.IsNullOrEmpty(value) ? "EditorList" : value;
                if (string.Equals(next, _autoScrollScope, StringComparison.Ordinal))
                    return;

                _autoScrollScope = next;
            }
        }

        public AutoScrollButtonWidget(UIContext context)
            : base(context)
        {
            boolPropertyChanged += OnBoolPropertyChanged;
            intPropertyChanged += OnIntPropertyChanged;
        }

        protected override void OnConnectedToRoot()
        {
            base.OnConnectedToRoot();

            _parentPanel ??= FindParentScrollablePanel(this);

            // Important: handles the "row spawned already selected" case.
            RequestScrollIfEligible();
        }

        private void OnBoolPropertyChanged(
            PropertyOwnerObject owner,
            string propertyName,
            bool value
        )
        {
            if (propertyName != "IsSelected" || !value)
                return;

            // Selection changed; only scroll if a new auto-scroll version is pending.
            RequestScrollIfEligible();
        }

        private void OnIntPropertyChanged(PropertyOwnerObject owner, string propertyName, int value)
        {
            if (propertyName != nameof(AutoScrollVersion))
                return;

            // Some Gauntlet paths do not hit the setter reliably; be defensive.
            RequestScrollIfEligible();
        }

        private void RequestScrollIfEligible()
        {
            if (!AutoScrollAlways)
            {
                if (!AutoScrollEnabled)
                    return;

                if (!IsSelected)
                    return;
            }

            if (_autoScrollVersion <= 0)
                return;

            var scope = string.IsNullOrEmpty(_autoScrollScope) ? "EditorList" : _autoScrollScope;
            var key = AutoScrollAlways ? $"{scope}:top" : $"{scope}:row";

            if (
                LastScrolledVersionByScope.TryGetValue(key, out var last)
                && last >= _autoScrollVersion
            )
                return;

            _pendingScroll = true;
            _parentPanel ??= FindParentScrollablePanel(this);
        }

        protected override void OnLateUpdate(float dt)
        {
            base.OnLateUpdate(dt);

            if (!_pendingScroll)
                return;

            if (_parentPanel == null)
                return;

            if (IsHidden || !IsVisible)
                return;

#if BL13
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
#else
            // BL12 only supports one offset per axis (applies to both sides)
            int hOff = (int)Math.Round(Math.Max(AutoScrollLeftOffset, AutoScrollRightOffset));
            int vOff = (int)Math.Round(Math.Max(AutoScrollTopOffset, AutoScrollBottomOffset));

            _parentPanel.ScrollToChild(
                this,
                horizontalTargetValue: AutoScrollHorizontalTarget,
                verticalTargetValue: AutoScrollVerticalTarget,
                horizontalOffsetInPixels: hOff,
                verticalOffsetInPixels: vOff,
                verticalInterpolationTime: AutoScrollInterpolationTime,
                horizontalInterpolationTime: AutoScrollInterpolationTime
            );
#endif

            _pendingScroll = false;

            var scope = string.IsNullOrEmpty(_autoScrollScope) ? "EditorList" : _autoScrollScope;
            var key = AutoScrollAlways ? $"{scope}:top" : $"{scope}:row";
            LastScrolledVersionByScope[key] = _autoScrollVersion;
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
