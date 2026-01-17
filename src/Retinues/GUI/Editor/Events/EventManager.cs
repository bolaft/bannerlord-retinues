using System;
using System.Collections.Generic;
using Retinues.Framework.Runtime;
using Retinues.Utilities;

namespace Retinues.GUI.Editor.Events
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                      Event Manager                     //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Centralized event bus with batching and per-burst dedup.
    ///
    /// - VMs register/unregister themselves.
    /// - Fire/FireBatch/FireSequence dispatch UIEvent values.
    /// - Each VM receives events through BaseVM.__OnGlobalEvent.
    /// - A "burst" ensures each (VM, property) pair is notified at most
    ///   once even if many events chain into each other.
    /// </summary>
    [SafeClass]
    public static partial class EventManager
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Burst Public API                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// True while inside FireBatch / nested Fire calls.
        /// Used by ControllerAction to enable per-burst caching.
        /// </summary>
        internal static bool IsInBurst
        {
            get
            {
                lock (_lock)
                {
                    return _burstDepth > 0;
                }
            }
        }

        /// <summary>
        /// Monotonically increasing burst identifier.
        /// Changes only when entering the outermost burst.
        /// Used by ControllerAction to scope caches.
        /// </summary>
        internal static int CurrentBurstId
        {
            get
            {
                lock (_lock)
                {
                    return _burstId;
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
        public sealed class Context
        {
            internal UIEvent RootEvent { get; private set; }
            internal UIEvent ParentEvent { get; private set; }
            internal UIEvent CurrentEvent { get; private set; }

            internal void SetDispatch(UIEvent root, UIEvent parent, UIEvent current)
            {
                RootEvent = root;
                ParentEvent = parent;
                CurrentEvent = current;
            }

            /// <summary>
            /// Queues a property notification for the VM in this context.
            /// </summary>
            internal void RequestNotify(EventListenerVM vm, string propertyName)
            {
                if (vm == null || string.IsNullOrEmpty(propertyName))
                    return;

                AddNotification(vm, propertyName);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly List<WeakReference<EventListenerVM>> _listeners = [];
        private static readonly object _lock = new();

        // Burst depth: >0 means we are inside a "burst".
        private static int _burstDepth;

        // Changes each time we enter the outermost burst.
        private static int _burstId;

        // Pending (VM, propertyName) notifications for the current burst.
        private static readonly Dictionary<
            EventListenerVM,
            HashSet<string>
        > _pendingNotifications = [];

        // Shared context instance used for all notifications.
        private static readonly Context _context = new();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Listener Registration                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Registers a VM to receive UI events.
        /// </summary>
        internal static void Register(EventListenerVM vm)
        {
            if (vm == null)
                return;

            lock (_lock)
            {
                _listeners.Add(new WeakReference<EventListenerVM>(vm));
            }
        }

        /// <summary>
        /// Unregisters a VM so it no longer receives events.
        /// </summary>
        internal static void Unregister(EventListenerVM vm)
        {
            if (vm == null)
                return;

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
        /// Fire a single UI event. Outside a batch this is a mini-burst.
        /// </summary>
        public static void Fire(UIEvent e)
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

        /// <summary>
        /// Execute a batch of changes and flush notifications at the end.
        /// </summary>
        public static void FireBatch(Action emit)
        {
            if (emit == null)
                return;

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
                return;

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

        /// <summary>
        /// Begin a new burst, initializing burst state if outermost.
        /// </summary>
        private static void BeginBurst()
        {
            lock (_lock)
            {
                _burstDepth++;

                if (_burstDepth == 1)
                {
                    _burstId++; // advance burst id
                    _pendingNotifications.Clear();
                }
            }
        }

        /// <summary>
        /// End the current burst and flush notifications if outermost.
        /// </summary>
        private static void EndBurst()
        {
            Dictionary<EventListenerVM, HashSet<string>> snapshot = null;

            lock (_lock)
            {
                if (_burstDepth == 0)
                    return;

                _burstDepth--;

                // Still inside nested bursts - do not flush yet.
                if (_burstDepth > 0)
                    return;

                if (_pendingNotifications.Count > 0)
                {
                    snapshot = new Dictionary<EventListenerVM, HashSet<string>>(
                        _pendingNotifications
                    );
                    _pendingNotifications.Clear();
                }
            }

            if (snapshot == null || snapshot.Count == 0)
                return;

            // Flush outside the lock.
            foreach (var kvp in snapshot)
            {
                var vm = kvp.Key;
                if (vm == null)
                    continue;

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
        //                      Static Clears                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Clears all listeners, pending notifications, and burst state.
        /// </summary>
        [StaticClearAction]
        public static void ClearAll()
        {
            lock (_lock)
            {
                _listeners.Clear();
                _pendingNotifications.Clear();
                _burstDepth = 0;
                _burstId = 0;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Adds a pending property notification for the given VM.
        /// </summary>
        private static void AddNotification(EventListenerVM vm, string propertyName)
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

        /// <summary>
        /// Takes a snapshot of registered listeners, cleaning up dead references.
        /// </summary>
        private static List<EventListenerVM> TakeSnapshot()
        {
            lock (_lock)
            {
                var snapshot = new List<EventListenerVM>(_listeners.Count);

                for (int i = _listeners.Count - 1; i >= 0; i--)
                {
                    if (_listeners[i].TryGetTarget(out var target) && target != null)
                        snapshot.Add(target);
                    else
                        _listeners.RemoveAt(i);
                }

                return snapshot;
            }
        }

        /// <summary>
        /// Dispatches the given root event (and its expanded parents) to listeners.
        /// </summary>
        private static void NotifyListeners(UIEvent rootEvent)
        {
            var vms = TakeSnapshot();

            foreach (var exp in ExpandWithParent(rootEvent))
            {
                _context.SetDispatch(rootEvent, exp.Parent, exp.Current);

                for (int i = 0; i < vms.Count; i++)
                {
                    var vm = vms[i];
                    if (vm == null)
                        continue;

                    vm.__OnGlobalEvent(_context, exp.Current);
                }
            }
        }
    }
}
