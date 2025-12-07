using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Retinues.Utils
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
        }

        private static readonly Stopwatch Total = new();
        private static readonly Dictionary<string, Segment> Segments = new(
            StringComparer.OrdinalIgnoreCase
        );

        private static bool _running;

        /// <summary>
        /// Starts a new timing session, resetting any previous measurements.
        /// </summary>
        public static void Start()
        {
            Total.Reset();
            Total.Start();
            Segments.Clear();
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

            if (!segment.Stopwatch.IsRunning)
                segment.Stopwatch.Start();
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

            if (segment.Stopwatch.IsRunning)
                segment.Stopwatch.Stop();
        }

        /// <summary>
        /// Ends the current session and logs total time and per-segment percentages.
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

            Log.Debug($"Time: {totalSeconds:0.00000}s");

            if (Segments.Count == 0)
                return;

            foreach (var segment in Segments.Values)
            {
                var seconds = segment.Elapsed.TotalSeconds;
                var percent = totalSeconds > 0.0 ? (seconds / totalSeconds) * 100.0 : 0.0;

                Log.Debug($"{segment.Label}: {seconds:0.00000}s ({percent:0.0}%)");
            }
        }
    }
}
