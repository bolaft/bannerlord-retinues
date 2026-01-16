using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Retinues.GUI.Editor;
using Retinues.GUI.Editor.Events;
using Retinues.Utilities;

namespace Retinues.Configuration
{
    public static partial class SettingsManager
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const double EventThrottleSeconds = 0.1;

        private static readonly Dictionary<string, double> _lastUiEventFireTime = new(
            StringComparer.OrdinalIgnoreCase
        );

        private static readonly HashSet<string> _pendingUiEventKeys = new(
            StringComparer.OrdinalIgnoreCase
        );

        /// <summary>
        /// Gets the current time in seconds for event throttling.
        /// </summary>
        private static double NowSeconds()
        {
            return Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        }

        /// <summary>
        /// Queues UI events for the given option key if needed.
        /// </summary>
        private static void QueueUIEventsIfNeeded(string key)
        {
            if (!EditorScreen.IsOpen)
                return;

            DiscoverOptions();

            if (!_byKey.TryGetValue(key, out var opt) || opt == null)
                return;

            if (opt.Fires == null || opt.Fires.Length == 0)
                return;

            // Only throttle slider spam (float/double). Everything else stays instant.
            if (opt.Type != typeof(float) && opt.Type != typeof(double))
            {
                _lastUiEventFireTime[key] = NowSeconds();
                FireUIEventsIfNeeded(key);
                return;
            }

            double now = NowSeconds();

            if (!_lastUiEventFireTime.TryGetValue(key, out double last))
                last = 0;

            if (now - last >= EventThrottleSeconds)
            {
                _lastUiEventFireTime[key] = now;
                FireUIEventsIfNeeded(key);
                return;
            }

            // Too soon - coalesce and let Tick() flush later.
            _pendingUiEventKeys.Add(key);
        }

        /// <summary>
        /// Flushes throttled UI events. Call this periodically while the editor is open.
        /// </summary>
        internal static void Tick()
        {
            if (!EditorScreen.IsOpen)
                return;

            if (_pendingUiEventKeys.Count == 0)
                return;

            double now = NowSeconds();

            // Copy to avoid modifying while iterating.
            foreach (var key in _pendingUiEventKeys.ToArray())
            {
                if (!_lastUiEventFireTime.TryGetValue(key, out double last))
                    last = 0;

                if (now - last < EventThrottleSeconds)
                    continue;

                _pendingUiEventKeys.Remove(key);
                _lastUiEventFireTime[key] = now;

                try
                {
                    FireUIEventsIfNeeded(key);
                }
                catch (Exception e)
                {
                    Log.Exception(e, "EventManager.Fire failed for throttled event.");
                }
            }
        }

        /// <summary>
        /// Fires UI events for the given option key.
        /// </summary>
        private static void FireUIEventsIfNeeded(string key)
        {
            if (!EditorScreen.IsOpen)
                return;

            DiscoverOptions();

            if (!_byKey.TryGetValue(key, out var opt) || opt == null)
                return;

            if (opt.Fires == null || opt.Fires.Length == 0)
                return;

            foreach (var ev in opt.Fires)
            {
                try
                {
                    EventManager.Fire(ev);
                }
                catch (Exception e)
                {
                    Log.Exception(e, "EventManager.Fire failed for event.");
                }
            }
        }
    }
}
