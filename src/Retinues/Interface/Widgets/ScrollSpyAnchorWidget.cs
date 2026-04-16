using System;
using System.Collections.Generic;
using Retinues.Settings;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace Retinues.Interface.Widgets
{
    /// <summary>
    /// Invisible zero-size anchor dropped at the top of each settings section.
    ///
    /// Scroll-spy algorithm (runs each frame via OnLateUpdate):
    ///
    ///   Every anchor reports (myY, SectionName) into a per-scope, per-frame
    ///   "best candidate" slot.  After all anchors have run, the slot holds the winner.
    ///   Winning rules (same as a sticky nav-highlight):
    ///
    ///   1. An anchor whose header has PASSED the viewport top
    ///      (myY &lt;= viewportTopY, i.e. scrolled above) always beats one that hasn't.
    ///   2. Among multiple "passed" anchors, the one with the LARGEST Y wins
    ///      (most recently scrolled past = the section we are currently inside).
    ///   3. If NO anchor has passed the viewport top yet (near the very top of the
    ///      list), fall back to the one with the SMALLEST Y (topmost visible header).
    ///
    ///   Because multiple calls may be better than previous ones, the callback can
    ///   fire more than once per frame — but SetActiveSectionFromSpy already guards
    ///   against no-op updates, so the extra calls are cheap.
    /// </summary>
    public class ScrollSpyAnchorWidget : Widget
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Static Registry                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly Dictionary<string, Action<string>> _callbacks = new(
            StringComparer.Ordinal
        );

        /// <summary>Register a callback for the given scroll-spy scope.</summary>
        public static void RegisterCallback(string scope, Action<string> callback)
        {
            if (!string.IsNullOrEmpty(scope) && callback != null)
                _callbacks[scope] = callback;
        }

        /// <summary>Remove the callback for the given scope.</summary>
        public static void UnregisterCallback(string scope)
        {
            if (!string.IsNullOrEmpty(scope))
                _callbacks.Remove(scope);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //               Per-frame best-candidate state           //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly struct FrameCandidate
        {
            public readonly ulong Frame;
            public readonly float BestY;
            public readonly string Name;
            public readonly bool IsPast; // true = header passed viewport top

            public FrameCandidate(ulong frame, float bestY, string name, bool isPast)
            {
                Frame = frame;
                BestY = bestY;
                Name = name;
                IsPast = isPast;
            }
        }

        private static readonly Dictionary<string, FrameCandidate> _frameBest = new(
            StringComparer.Ordinal
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Bindable Properties                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [Editor(false)]
        public string SpyScope { get; set; } = "";

        [Editor(false)]
        public string SectionName { get; set; } = "";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Runtime State                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private ScrollablePanel _parentPanel;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Constructor                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public ScrollSpyAnchorWidget(UIContext context)
            : base(context) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       LateUpdate                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override void OnLateUpdate(float dt)
        {
            base.OnLateUpdate(dt);

            if (string.IsNullOrEmpty(SpyScope) || string.IsNullOrEmpty(SectionName))
                return;

            _parentPanel ??= FindParentScrollablePanel(this);
            if (_parentPanel == null)
                return;

#if BL13 || BL14
            ulong frame = EventManager.LocalFrameNumber;
#else
            ulong frame = unchecked((ulong)Environment.TickCount);
#endif
            float viewportTopY = _parentPanel.GlobalPosition.Y;
            float myY = GlobalPosition.Y;

            // "Past" = this section's header has scrolled to or above the viewport top.
            // The tolerance covers the inter-section gap (divider ~28 px + margins) so
            // that after a click-to-scroll the target section's anchor — which may land
            // a few pixels below the viewport top — still beats the previous section.
            const float SpyPastTolerance = 60f;
            bool isPast = myY <= viewportTopY + SpyPastTolerance;

            var key = SpyScope;
            bool isBetter;

            if (!_frameBest.TryGetValue(key, out var best) || best.Frame != frame)
            {
                // First anchor seen this frame — always becomes the initial candidate.
                isBetter = true;
            }
            else if (isPast && !best.IsPast)
            {
                // A "past" anchor always beats a "not-yet-past" anchor.
                isBetter = true;
            }
            else if (isPast && best.IsPast)
            {
                // Both past: keep the one with the LARGEST Y
                // (most recently scrolled past = the section we are currently inside).
                isBetter = myY > best.BestY;
            }
            else if (!isPast && !best.IsPast)
            {
                // Neither past: keep the one with the SMALLEST Y
                // (topmost visible header = what the user sees first).
                isBetter = myY < best.BestY;
            }
            else
            {
                // isPast=false, best.IsPast=true → existing wins.
                isBetter = false;
            }

            if (!isBetter)
                return;

            _frameBest[key] = new FrameCandidate(frame, myY, SectionName, isPast);

            if (_callbacks.TryGetValue(key, out var cb))
                cb(SectionName);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Helpers                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static ScrollablePanel FindParentScrollablePanel(Widget widget)
        {
            var current = widget.ParentWidget;
            while (current != null)
            {
                if (current is ScrollablePanel panel)
                    return panel;
                current = current.ParentWidget;
            }
            return null;
        }
    }
}
