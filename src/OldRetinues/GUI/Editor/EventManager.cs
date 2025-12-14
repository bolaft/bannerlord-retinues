using System;
using System.Collections.Generic;
using Retinues.Utils;

namespace OldRetinues.GUI.Editor
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                       Events Enum                      //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Types of UI events emitted by the editor.
    /// </summary>
    public enum UIEvent
    {
        Troop,
        Faction,
        Equipment,
        Appearance,
        Equip,
        Train,
        Conversion,
        Slot,
        Party,
    }

    /// <summary>
    /// Centralized event bus with batching and safe fan-out.
    /// </summary>
    [SafeClass]
    public static class EventManager
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Event Registration                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly List<WeakReference<BaseVM>> _listeners = [];
        private static readonly object _lock = new();
        private static int _depth;

        /// <summary>
        /// Register a view-model to receive global events.
        /// </summary>
        internal static void Register(BaseVM vm)
        {
            if (vm == null)
                return;
            lock (_lock)
                _listeners.Add(new WeakReference<BaseVM>(vm));
        }

        /// <summary>
        /// Unregister a view-model from receiving global events.
        /// </summary>
        internal static void Unregister(BaseVM vm)
        {
            if (vm == null)
                return;
            lock (_lock)
            {
                for (int i = _listeners.Count - 1; i >= 0; --i)
                {
                    if (_listeners[i].TryGetTarget(out var t) && ReferenceEquals(t, vm))
                    {
                        _listeners.RemoveAt(i);
                        break;
                    }
                    if (_listeners[i].TryGetTarget(out _) == false)
                        _listeners.RemoveAt(i);
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Fire a single UI event to all listeners.
        /// </summary>
        public static void Fire(UIEvent e) => NotifySnapshot(vm => vm.__OnGlobalPulse(e));

        /// <summary>
        /// Execute a batch of updates and defer notifications until completion.
        /// </summary>
        public static void FireBatch(Action emit)
        {
            if (emit == null)
                return;
            BeginPulse();
            try
            {
                emit();
            }
            finally
            {
                EndPulse();
            }
        }

        /// <summary>
        /// Fire a sequence of events as a single batched pulse.
        /// </summary>
        public static void FireSequence(params UIEvent[] events)
        {
            BeginPulse();
            try
            {
                if (events != null)
                    foreach (var e in events)
                        Fire(e);
            }
            finally
            {
                EndPulse();
            }
        }

        /// <summary>
        /// Begin a notification pulse (enter batch mode).
        /// </summary>
        public static void BeginPulse()
        {
            bool notify = false;
            lock (_lock)
            {
                _depth++;
                if (_depth == 1)
                    notify = true;
            }
            if (notify)
                NotifySnapshot(vm => vm.__BeginPulse());
        }

        /// <summary>
        /// End a notification pulse (exit batch mode).
        /// </summary>
        public static void EndPulse()
        {
            bool notify = false;
            lock (_lock)
            {
                if (_depth > 0 && --_depth == 0)
                    notify = true;
            }
            if (notify)
                NotifySnapshot(vm => vm.__EndPulse());
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Snapshot                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Take a snapshot of listeners and invoke an action on each safely.
        /// </summary>
        private static void NotifySnapshot(Action<BaseVM> action)
        {
            if (action == null)
                return;

            List<BaseVM> snapshot;
            lock (_lock)
            {
                snapshot = new List<BaseVM>(_listeners.Count);
                for (int i = _listeners.Count - 1; i >= 0; --i)
                {
                    if (_listeners[i].TryGetTarget(out var t) && t != null)
                        snapshot.Add(t);
                    else
                        _listeners.RemoveAt(i);
                }
            }

            foreach (var vm in snapshot)
            {
                try
                {
                    action(vm);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"EventManager notify error: {ex}");
                }
            }
        }
    }
}
