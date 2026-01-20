using System;
using System.Collections.Generic;
using System.Diagnostics;
using TaleWorlds.Library;

namespace Retinues.Utilities
{
    /// <summary>
    /// Simple static timer for ad-hoc profiling between two points with labeled segments.
    /// </summary>
    public static class Timer
    {
        private sealed class Segment
        {
            public string Label;
            public Stopwatch Stopwatch = new();
            public TimeSpan Elapsed => Stopwatch.Elapsed;

            // Re-entrancy guard (Begin/End can nest across Harmony patches).
            public int Depth;

            // Number of times this segment was entered (outermost Begin calls).
            public int EnterCount;
        }

        private static readonly Stopwatch Total = new();
        private static readonly Dictionary<string, Segment> Segments = new(
            StringComparer.OrdinalIgnoreCase
        );

        private static bool _running;

        // "Real time": time between first outer Begin() and last outer End().
        private static TimeSpan? _firstSegmentBegin;
        private static TimeSpan? _lastSegmentEnd;

        /// <summary>
        /// True if a timing session is active.
        /// </summary>
        public static bool IsRunning => _running;

        /// <summary>
        /// Starts a new timing session, resetting any previous measurements.
        /// </summary>
        public static void Start()
        {
            Total.Reset();
            Total.Start();
            Segments.Clear();

            _firstSegmentBegin = null;
            _lastSegmentEnd = null;

            _running = true;

            Log.Debug("Timer.Start");
        }

        /// <summary>
        /// Begins or resumes measuring a labeled segment.
        /// </summary>
        public static void Begin(string label)
        {
            if (string.IsNullOrEmpty(label))
                return;

            if (!_running)
                return;

            if (!Segments.TryGetValue(label, out var segment))
            {
                segment = new Segment { Label = label };
                Segments[label] = segment;
            }

            segment.Depth++;
            if (segment.Depth == 1)
            {
                // Track the earliest segment begin time within the full session.
                if (_firstSegmentBegin == null)
                    _firstSegmentBegin = Total.Elapsed;

                segment.EnterCount++;
                if (!segment.Stopwatch.IsRunning)
                    segment.Stopwatch.Start();
            }
        }

        /// <summary>
        /// Pauses measurement for the given labeled segment.
        /// </summary>
        public static void End(string label)
        {
            if (string.IsNullOrEmpty(label))
                return;

            if (!_running)
                return;

            if (!Segments.TryGetValue(label, out var segment))
                return;

            if (segment.Depth > 0)
                segment.Depth--;

            if (segment.Depth <= 0)
            {
                segment.Depth = 0;
                if (segment.Stopwatch.IsRunning)
                    segment.Stopwatch.Stop();

                // Track the latest segment end time within the full session.
                _lastSegmentEnd = Total.Elapsed;
            }
        }

        /// <summary>
        /// Ends the current session and logs total time and per-segment percentages.
        /// Percentages are computed against the "real time" window (first Begin -> last End).
        /// </summary>
        public static void Stop()
        {
            if (!_running)
                return;

            _running = false;

            // Stop total timer and any still-running segments.
            Total.Stop();

            foreach (var segment in Segments.Values)
            {
                if (segment.Stopwatch.IsRunning)
                    segment.Stopwatch.Stop();

                segment.Depth = 0;
            }

            var totalSeconds = Total.Elapsed.TotalSeconds;

            // Fallback: if total is zero (very small span), approximate from segments.
            if (totalSeconds <= 0)
            {
                double sum = 0;
                foreach (var seg in Segments.Values)
                    sum += seg.Elapsed.TotalSeconds;

                totalSeconds = sum;
            }

            // If at least one segment began but none ended (or last end wasn't recorded),
            // treat Stop() as the last boundary for "real time".
            if (_firstSegmentBegin != null && _lastSegmentEnd == null)
                _lastSegmentEnd = Total.Elapsed;

            double realSeconds = 0.0;
            if (_firstSegmentBegin != null && _lastSegmentEnd != null)
            {
                var span = _lastSegmentEnd.Value - _firstSegmentBegin.Value;
                if (span.Ticks > 0)
                    realSeconds = span.TotalSeconds;
            }

            Log.Debug($"Time: {totalSeconds:0.00000}s | Real: {realSeconds:0.00000}s");

            if (Segments.Count == 0)
                return;

            // Order segment logs by longest time to shortest (tie-breaker: label).
            var ordered = new List<Segment>(Segments.Values);
            ordered.Sort(
                (a, b) =>
                {
                    var c = b.Elapsed.CompareTo(a.Elapsed);
                    if (c != 0)
                        return c;

                    return string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase);
                }
            );

            foreach (var segment in ordered)
            {
                var seconds = segment.Elapsed.TotalSeconds;
                var percent = realSeconds > 0.0 ? (seconds / realSeconds) * 100.0 : 0.0;

                Log.Debug(
                    $"{segment.Label}: {seconds:0.00000}s ({percent:0.0}%) x{segment.EnterCount}"
                );
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Cheats                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

#if DEBUG
        /// <summary>
        /// Starts a new timing session (clears previous segments).
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("timer_start", "retinues")]
        public static string TimerStart(List<string> args)
        {
            Start();
            return "Timer started. Run retinues.timer_stop to log results.";
        }

        /// <summary>
        /// Stops the session and logs the breakdown (total + per-segment percentages).
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("timer_stop", "retinues")]
        public static string TimerStop(List<string> args)
        {
            Stop();
            return "Timer stopped. Check the log for segment timings.";
        }
#endif
    }
}
