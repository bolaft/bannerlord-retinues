using System;
using System.Collections.Generic;
using Retinues.Editor.VM;
using Retinues.Utilities;

namespace Retinues.Editor
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                       Events Enum                      //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Types of UI events emitted by the editor.
    /// </summary>
    public enum UIEvent
    {
        Mode,
        Faction,
        Troop,
    }

    public enum EventScope
    {
        Global,
        Local,
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                    Event Attributes                    //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Marks a property as listening to one or more UI events.
    /// When any of these events fire, the property will be
    /// scheduled for OnPropertyChanged in the current burst.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Method,
        AllowMultiple = true,
        Inherited = true
    )]
    public sealed class EventListenerAttribute : Attribute
    {
        public UIEvent[] Events { get; }

        public EventListenerAttribute(params UIEvent[] events)
        {
            Events = events ?? Array.Empty<UIEvent>();
        }
    }

    /// <summary>
    /// Marks a method conceptually as an emitter for one or more
    /// UI events. This is metadata/documentation for now; actual
    /// emission is done by calling EventManager.Fire(...).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class EventEmitterAttribute : Attribute
    {
        public UIEvent[] Events { get; }

        public EventEmitterAttribute(params UIEvent[] events)
        {
            Events = events ?? Array.Empty<UIEvent>();
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                      Event Manager                     //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Centralized event bus with batching and per-burst dedup.
    ///
    /// - VMs register/unregister themselves.
    /// - Fire/FireBatch/FireSequence dispatch UIEvent values.
    /// - Each VM receives events through BaseStatefulVM.__OnGlobalEvent.
    /// - A "burst" ensures each (VM, property) pair is notified at most
    ///   once even if many events chain into each other.
    /// </summary>
    public static class EventManager
    {
        // current scope for the event being dispatched
        internal static EventScope CurrentScope { get; private set; } = EventScope.Global;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Burst Context Type                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Shared context passed to VMs so they can queue property
        /// notifications without immediately calling OnPropertyChanged.
        /// </summary>
        internal sealed class Context
        {
            internal void RequestNotify(BaseStatefulVM vm, string propertyName)
            {
                if (vm == null || string.IsNullOrEmpty(propertyName))
                {
                    return;
                }

                EventManager.AddNotification(vm, propertyName);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly List<WeakReference<BaseStatefulVM>> _listeners =
            new List<WeakReference<BaseStatefulVM>>();
        private static readonly object _lock = new object();

        // Burst depth: >0 means we are inside a "burst".
        private static int _burstDepth;

        // Pending (VM, propertyName) notifications for the current burst.
        private static readonly Dictionary<BaseStatefulVM, HashSet<string>> _pendingNotifications =
            new Dictionary<BaseStatefulVM, HashSet<string>>();

        // Shared context instance used for all notifications.
        private static readonly Context _context = new Context();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Listener Registration                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal static void Register(BaseStatefulVM vm)
        {
            if (vm == null)
            {
                return;
            }

            lock (_lock)
            {
                _listeners.Add(new WeakReference<BaseStatefulVM>(vm));
            }
        }

        internal static void Unregister(BaseStatefulVM vm)
        {
            if (vm == null)
            {
                return;
            }

            lock (_lock)
            {
                for (int i = _listeners.Count - 1; i >= 0; i--)
                {
                    if (!_listeners[i].TryGetTarget(out var target) || target == null)
                    {
                        _listeners.RemoveAt(i);
                        continue;
                    }

                    if (ReferenceEquals(target, vm))
                    {
                        _listeners.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Fire a single UI event. Outside a batch, this still behaves
        /// as a mini-burst so listeners are only notified once.
        /// </summary>
        public static void Fire(UIEvent e, EventScope scope = EventScope.Global)
        {
            var previousScope = CurrentScope;
            CurrentScope = scope;

            try
            {
                if (_burstDepth == 0)
                {
                    BeginBurst();
                    try
                    {
                        NotifyListeners(e);
                    }
                    finally
                    {
                        EndBurst();
                    }
                }
                else
                {
                    NotifyListeners(e);
                }
            }
            finally
            {
                CurrentScope = previousScope;
            }
        }

        /// <summary>
        /// Execute a batch of changes and flush notifications at the end.
        /// Any events fired inside this delegate are part of the same burst.
        /// </summary>
        public static void FireBatch(Action emit)
        {
            if (emit == null)
            {
                return;
            }

            BeginBurst();
            try
            {
                emit();
            }
            finally
            {
                EndBurst();
            }
        }

        /// <summary>
        /// Fire a sequence of events as a single burst.
        /// </summary>
        public static void FireSequence(params UIEvent[] events)
        {
            if (events == null || events.Length == 0)
            {
                return;
            }

            FireBatch(() =>
            {
                for (int i = 0; i < events.Length; i++)
                {
                    Fire(events[i]);
                }
            });
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Burst Management                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void BeginBurst()
        {
            lock (_lock)
            {
                _burstDepth++;

                if (_burstDepth == 1)
                {
                    _pendingNotifications.Clear();
                }
            }
        }

        private static void EndBurst()
        {
            Dictionary<BaseStatefulVM, HashSet<string>> snapshot = null;

            lock (_lock)
            {
                if (_burstDepth == 0)
                {
                    return;
                }

                _burstDepth--;

                // Still inside nested bursts - do not flush yet.
                if (_burstDepth > 0)
                {
                    return;
                }

                if (_pendingNotifications.Count > 0)
                {
                    snapshot = new Dictionary<BaseStatefulVM, HashSet<string>>(
                        _pendingNotifications
                    );
                    _pendingNotifications.Clear();
                }
            }

            if (snapshot == null || snapshot.Count == 0)
            {
                return;
            }

            // Flush outside the lock.
            foreach (var kvp in snapshot)
            {
                var vm = kvp.Key;
                if (vm == null)
                {
                    continue;
                }

                foreach (var propertyName in kvp.Value)
                {
                    try
                    {
                        vm.__NotifyPropertyChanged(propertyName);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Error notifying '{vm.GetType().Name}.{propertyName}' from EventManager: {ex}"
                        );
                    }
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void AddNotification(BaseStatefulVM vm, string propertyName)
        {
            lock (_lock)
            {
                if (!_pendingNotifications.TryGetValue(vm, out var set))
                {
                    set = new HashSet<string>(StringComparer.Ordinal);
                    _pendingNotifications[vm] = set;
                }

                set.Add(propertyName);
            }
        }

        private static List<BaseStatefulVM> TakeSnapshot()
        {
            lock (_lock)
            {
                var snapshot = new List<BaseStatefulVM>(_listeners.Count);

                for (int i = _listeners.Count - 1; i >= 0; i--)
                {
                    if (_listeners[i].TryGetTarget(out var target) && target != null)
                    {
                        snapshot.Add(target);
                    }
                    else
                    {
                        _listeners.RemoveAt(i);
                    }
                }

                return snapshot;
            }
        }

        private static void NotifyListeners(UIEvent e)
        {
            var vms = TakeSnapshot();

            for (int i = 0; i < vms.Count; i++)
            {
                var vm = vms[i];

                try
                {
                    vm.__OnGlobalEvent(_context, e);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error dispatching event '{e}' to '{vm.GetType().Name}': {ex}");
                }
            }
        }
    }
}
