using System;
using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace Retinues.GUI.Widgets
{
    /// <summary>
    /// ButtonWidget that can auto-scroll itself into view inside the nearest parent ScrollablePanel.
    /// </summary>
    public class AutoScrollButtonWidget : ButtonWidget
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Public Knobs                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Enables auto-scroll when IsSelected becomes true (unless AutoScrollAlways is true).
        [Editor(false)]
        public bool AutoScrollEnabled { get; set; } = true;

        // If true, eligibility ignores selection/enablement and only depends on version progression.
        // Intended for cases like "scroll to top" or "scroll to focused item" even if not selected.
        [Editor(false)]
        public bool AutoScrollAlways { get; set; } = false;

        // Desired padding above the child after scrolling.</summary>
        [Editor(false)]
        public float AutoScrollTopOffset { get; set; }

        // Desired padding below the child after scrolling.</summary>
        [Editor(false)]
        public float AutoScrollBottomOffset { get; set; }

        // Desired padding to the left of the child after scrolling.</summary>
        [Editor(false)]
        public float AutoScrollLeftOffset { get; set; }

        // Desired padding to the right of the child after scrolling.</summary>
        [Editor(false)]
        public float AutoScrollRightOffset { get; set; }

        // Optional explicit horizontal scroll target for ScrollToChild.
        // -1 means "let ScrollablePanel decide".
        [Editor(false)]
        public float AutoScrollHorizontalTarget { get; set; } = -1f;

        // Optional explicit vertical scroll target for ScrollToChild.
        // -1 means "let ScrollablePanel decide".
        [Editor(false)]
        public float AutoScrollVerticalTarget { get; set; } = -1f;

        // Smooth scroll duration. Kept short so selection snaps quickly without looking abrupt.
        [Editor(false)]
        public float AutoScrollInterpolationTime { get; set; } = 0.08f;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   De-Duping / Scoping                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Global "already scrolled" memory keyed by scope and mode.
        /// This prevents re-scrolling repeatedly during list rebuilds / rebinds.
        /// </summary>
        private static readonly Dictionary<string, int> LastScrolledVersionByScope = new(
            StringComparer.Ordinal
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Runtime State                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>Cached nearest ancestor ScrollablePanel used for ScrollToChild.</summary>
        private ScrollablePanel _parentPanel;

        /// <summary>Set true when a scroll has been requested; executed in LateUpdate.</summary>
        private bool _pendingScroll;

        /// <summary>Current version token used to detect "new selection/refresh" states.</summary>
        private int _autoScrollVersion;

        /// <summary>
        /// Logical group name used to isolate de-duping between different lists/screens.
        /// Default chosen for editor lists.
        /// </summary>
        private string _autoScrollScope = "EditorList";

        /// <summary>
        /// Monotonically increasing token set by the VM.
        /// When it changes, and the widget is eligible, we scroll once for that scope.
        /// </summary>
        [Editor(false)]
        public int AutoScrollVersion
        {
            get => _autoScrollVersion;
            set
            {
                if (value == _autoScrollVersion)
                    return;

                _autoScrollVersion = value;

                // If we are selected (or in "always" mode) when the version changes, request a scroll.
                // Note: actual scrolling is deferred to LateUpdate so layout/visibility has stabilized.
                RequestScrollIfEligible();
            }
        }

        /// <summary>
        /// Logical scope string for de-duping. Different lists should use different scopes so they do not
        /// block each other when versions overlap.
        /// </summary>
        [Editor(false)]
        public string AutoScrollScope
        {
            get => _autoScrollScope;
            set
            {
                // Normalize empty/null to a stable default.
                var next = string.IsNullOrEmpty(value) ? "EditorList" : value;
                if (string.Equals(next, _autoScrollScope, StringComparison.Ordinal))
                    return;

                _autoScrollScope = next;
            }
        }

        /// <summary>
        /// Hooks property change listeners used by Gauntlet for selection/version changes.
        /// </summary>
        public AutoScrollButtonWidget(UIContext context)
            : base(context)
        {
            // Gauntlet raises these when UI-bound properties change.
            boolPropertyChanged += OnBoolPropertyChanged;
            intPropertyChanged += OnIntPropertyChanged;
        }

        /// <summary>
        /// Called when the widget enters the widget tree.
        /// We resolve parent panel once and handle "row spawned already selected".
        /// </summary>
        protected override void OnConnectedToRoot()
        {
            base.OnConnectedToRoot();

            // Cache the parent panel early; searching every frame is unnecessary.
            _parentPanel ??= FindParentScrollablePanel(this);

            // Important: covers the case where a row is created already selected
            // (no selection-change event will fire after creation).
            RequestScrollIfEligible();
        }

        /// <summary>
        /// Handles selection changes. We scroll on "IsSelected -> true" if a new version is pending.
        /// </summary>
        private void OnBoolPropertyChanged(
            PropertyOwnerObject owner,
            string propertyName,
            bool value
        )
        {
            if (propertyName != "IsSelected" || !value)
                return;

            // Only scroll if the version indicates something new (de-duping happens in RequestScrollIfEligible).
            RequestScrollIfEligible();
        }

        /// <summary>
        /// Defensive hook: some Gauntlet binding paths may bypass the C# setter.
        /// We react to the internal intPropertyChanged event to be safe.
        /// </summary>
        private void OnIntPropertyChanged(PropertyOwnerObject owner, string propertyName, int value)
        {
            if (propertyName != nameof(AutoScrollVersion))
                return;

            // Setter might not be hit reliably in some binding paths; re-check eligibility here.
            RequestScrollIfEligible();
        }

        /// <summary>
        /// Determines if a scroll should occur, and schedules it for LateUpdate.
        /// This method does not perform scrolling immediately.
        /// </summary>
        private void RequestScrollIfEligible()
        {
            // "Always" ignores selection/enablement gating.
            if (!AutoScrollAlways)
            {
                if (!AutoScrollEnabled)
                    return;

                // Typical behavior: only the selected row should bring itself into view.
                if (!IsSelected)
                    return;
            }

            // Versions <= 0 are treated as "disabled/uninitialized".
            if (_autoScrollVersion <= 0)
                return;

            // Ensure scope is non-empty (in case something wrote an empty string).
            var scope = string.IsNullOrEmpty(_autoScrollScope) ? "EditorList" : _autoScrollScope;

            // Different key depending on mode: selected row vs "always" target (often top/anchor).
            var key = AutoScrollAlways ? $"{scope}:top" : $"{scope}:row";

            // De-dupe: if we've already scrolled for this scope at this version (or newer), do nothing.
            if (
                LastScrolledVersionByScope.TryGetValue(key, out var last)
                && last >= _autoScrollVersion
            )
                return;

            // Schedule actual scroll for LateUpdate.
            _pendingScroll = true;

            // Ensure we have a panel by the time LateUpdate runs.
            _parentPanel ??= FindParentScrollablePanel(this);
        }

        /// <summary>
        /// LateUpdate is used to execute the scroll after Gauntlet has finalized layout and visibility.
        /// This avoids scrolling to stale sizes/positions during rebuild.
        /// </summary>
        protected override void OnLateUpdate(float dt)
        {
            base.OnLateUpdate(dt);

            if (!_pendingScroll)
                return;

            if (_parentPanel == null)
                return;

            // Avoid scrolling for hidden/invisible widgets; their geometry can be invalid.
            if (IsHidden || !IsVisible)
                return;

#if BL13
            // BL13 supports per-side offsets via AutoScrollParameters.
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
            // BL12 only supports a single offset per axis (applied symmetrically).
            // We take the max of left/right and top/bottom as a reasonable approximation.
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

            // Consume the pending request.
            _pendingScroll = false;

            // Record that this scope+mode has been scrolled for this version.
            var scope = string.IsNullOrEmpty(_autoScrollScope) ? "EditorList" : _autoScrollScope;
            var key = AutoScrollAlways ? $"{scope}:top" : $"{scope}:row";
            LastScrolledVersionByScope[key] = _autoScrollVersion;
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
