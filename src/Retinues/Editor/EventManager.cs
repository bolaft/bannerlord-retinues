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
        Name,
        Culture,
        Appearance,
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
    public sealed class EventListenerAttribute(params UIEvent[] events) : Attribute
    {
        public UIEvent[] Events { get; } = events ?? [];
    }

    /// <summary>
    /// Marks a method conceptually as an emitter for one or more
    /// UI events. This is metadata/documentation for now; actual
    /// emission is done by calling EventManager.Fire(...).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class EventEmitterAttribute(params UIEvent[] events) : Attribute
    {
        public UIEvent[] Events { get; } = events ?? [];
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
    [SafeClass]
    public static class EventManager
    {
        // current scope for the event being dispatched
        internal static EventScope CurrentScope { get; private set; } = EventScope.Global;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Event Hierarchy                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Declarative parent → children relationships between events.
        /// Firing a parent will also fire all of its descendants once
        /// in the current burst.
        /// </summary>
        private static readonly Dictionary<UIEvent, UIEvent[]> _hierarchy = new()
        {
            // Culture change implies appearance (since culture drives look).
            { UIEvent.Culture, new[] { UIEvent.Appearance } },
        };

        /// <summary>
        /// Expands a root event into itself + all transitive children,
        /// with cycle protection and per-event dedup.
        /// </summary>
        private static IEnumerable<UIEvent> Expand(UIEvent root)
        {
            var visited = new HashSet<UIEvent>();
            var stack = new Stack<UIEvent>();

            stack.Push(root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (!visited.Add(current))
                {
                    continue;
                }

                yield return current;

                if (_hierarchy.TryGetValue(current, out var children) && children != null)
                {
                    // Push in reverse so the array order is preserved on pop.
                    for (int i = children.Length - 1; i >= 0; i--)
                    {
                        stack.Push(children[i]);
                    }
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Burst Context Type                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Shared context passed to VMs so they can queue property
        /// notifications without immediately calling OnPropertyChanged.
        /// </summary>
        internal sealed class Context
        {
            internal void RequestNotify(BaseVM vm, string propertyName)
            {
                if (vm == null || string.IsNullOrEmpty(propertyName))
                {
                    return;
                }

                AddNotification(vm, propertyName);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly List<WeakReference<BaseVM>> _listeners = [];
        private static readonly object _lock = new();

        // Burst depth: >0 means we are inside a "burst".
        private static int _burstDepth;

        // Pending (VM, propertyName) notifications for the current burst.
        private static readonly Dictionary<BaseVM, HashSet<string>> _pendingNotifications = [];

        // Shared context instance used for all notifications.
        private static readonly Context _context = new();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Listener Registration                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal static void Register(BaseVM vm)
        {
            if (vm == null)
            {
                return;
            }

            lock (_lock)
            {
                _listeners.Add(new WeakReference<BaseVM>(vm));
            }
        }

        internal static void Unregister(BaseVM vm)
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
            Dictionary<BaseVM, HashSet<string>> snapshot = null;

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
                    snapshot = new Dictionary<BaseVM, HashSet<string>>(_pendingNotifications);
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

        private static void AddNotification(BaseVM vm, string propertyName)
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

        private static List<BaseVM> TakeSnapshot()
        {
            lock (_lock)
            {
                var snapshot = new List<BaseVM>(_listeners.Count);

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
