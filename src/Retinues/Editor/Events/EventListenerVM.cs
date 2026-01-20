using System;
using System.Collections.Generic;
using System.Reflection;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.Library;

namespace Retinues.Editor.Events
{
    /// <summary>
    /// Base ViewModel with shared editor state (faction and character) and
    /// attribute-driven event wiring.
    /// </summary>
    [SafeClass(IncludeDerived = true)]
    public abstract class EventListenerVM : ViewModel
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Global State                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal static EditorState State => EditorState.Instance;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Gating                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// When false, this VM ignores all UIEvent effects
        /// (no property refresh requests and no handler invocation).
        /// Override in derived VMs to gate event processing.
        /// </summary>
        protected virtual bool EventGate => true;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Event registration                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Initializes the event listener and registers it with the EventManager.
        /// </summary>
        protected EventListenerVM()
        {
            EventManager.Register(this);
        }

        /// <summary>
        /// Unregisters the VM from the EventManager and performs finalization.
        /// </summary>
        public override void OnFinalize()
        {
            EventManager.Unregister(this);
            base.OnFinalize();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Event Handling                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Represents a property listener entry with its name and global flag.
        /// </summary>
        internal readonly struct PropertyListener(string name, bool global)
        {
            internal string Name { get; } = name;
            internal bool Global { get; } = global;
        }

        // Type -> event -> property listeners
        private static readonly Dictionary<
            Type,
            Dictionary<UIEvent, PropertyListener[]>
        > _propertyEventMapCache = [];

        // Type -> event -> method handlers
        private static readonly Dictionary<
            Type,
            Dictionary<UIEvent, Action<EventListenerVM>[]>
        > _methodEventMapCache = [];

        /// <summary>
        /// Triggers a property change notification for the given property name.
        /// </summary>
        internal void __NotifyPropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }

        /// <summary>
        /// Called by EventManager for each UIEvent.
        /// Uses [EventListener] attributes on properties and methods.
        /// </summary>
        internal virtual void __OnGlobalEvent(EventManager.Context context, UIEvent e)
        {
            if (context == null)
            {
                return;
            }

            if (!EventGate)
            {
                return;
            }

            var type = GetType();

            // 1) Properties -> OnPropertyChanged at end of burst.
            var propertyMap = GetOrBuildPropertyEventMap(type);
            if (propertyMap.TryGetValue(e, out var listeners) && listeners != null)
            {
                for (int i = 0; i < listeners.Length; i++)
                {
                    var l = listeners[i];

                    if (!__ShouldNotifyProperty(context, e, l.Name, l.Global))
                    {
                        continue;
                    }

                    context.RequestNotify(this, l.Name);
                }
            }

            // 2) Methods -> invoked immediately.
            var methodMap = GetOrBuildMethodEventMap(type);
            if (methodMap.TryGetValue(e, out var handlers) && handlers != null)
            {
                for (int i = 0; i < handlers.Length; i++)
                {
                    try
                    {
                        handlers[i](this);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error invoking event handler '{type.Name}' for '{e}': {ex}");
                    }
                }
            }
        }

        /// <summary>
        /// Allows derived VMs to filter property refreshes per-event.
        /// ListRowVM uses this to refresh only selected rows unless
        /// the listener is marked Global=true.
        /// </summary>
        protected virtual bool __ShouldNotifyProperty(
            EventManager.Context context,
            UIEvent e,
            string propertyName,
            bool globalListener
        )
        {
            return true;
        }

        /// <summary>
        /// Gets or builds the cached property event map for the given type.
        /// </summary>
        private static Dictionary<UIEvent, PropertyListener[]> GetOrBuildPropertyEventMap(Type type)
        {
            if (_propertyEventMapCache.TryGetValue(type, out var map))
            {
                return map;
            }

            map = BuildPropertyEventMap(type);
            _propertyEventMapCache[type] = map;
            return map;
        }

        /// <summary>
        /// Gets or builds the cached method event map for the given type.
        /// </summary>
        private static Dictionary<UIEvent, Action<EventListenerVM>[]> GetOrBuildMethodEventMap(
            Type type
        )
        {
            if (_methodEventMapCache.TryGetValue(type, out var map))
            {
                return map;
            }

            map = BuildMethodEventMap(type);
            _methodEventMapCache[type] = map;
            return map;
        }

        /// <summary>
        /// Builds the mapping of UIEvents to property listeners for the type.
        /// </summary>
        private static Dictionary<UIEvent, PropertyListener[]> BuildPropertyEventMap(Type type)
        {
            // event -> (property -> isGlobal)
            var temp = new Dictionary<UIEvent, Dictionary<string, bool>>();

            var properties = type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            for (int i = 0; i < properties.Length; i++)
            {
                var prop = properties[i];
                var attrs = prop.GetCustomAttributes(typeof(EventListenerAttribute), true);
                if (attrs == null || attrs.Length == 0)
                {
                    continue;
                }

                var name = prop.Name;

                for (int j = 0; j < attrs.Length; j++)
                {
                    var listener = attrs[j] as EventListenerAttribute;
                    if (listener?.Events == null)
                    {
                        continue;
                    }

                    var isGlobal = listener.Global;

                    for (int k = 0; k < listener.Events.Length; k++)
                    {
                        var e = listener.Events[k];

                        if (!temp.TryGetValue(e, out var map))
                        {
                            map = new Dictionary<string, bool>(StringComparer.Ordinal);
                            temp[e] = map;
                        }

                        // Dedup by property name; prefer Global=true if any listener requires it.
                        if (map.TryGetValue(name, out var existing))
                        {
                            if (!existing && isGlobal)
                            {
                                map[name] = true;
                            }
                        }
                        else
                        {
                            map[name] = isGlobal;
                        }
                    }
                }
            }

            var result = new Dictionary<UIEvent, PropertyListener[]>(temp.Count);
            foreach (var kvp in temp)
            {
                var list = new List<PropertyListener>(kvp.Value.Count);

                foreach (var p in kvp.Value)
                {
                    list.Add(new PropertyListener(p.Key, p.Value));
                }

                result[kvp.Key] = [.. list];
            }

            return result;
        }

        /// <summary>
        /// Builds the mapping of UIEvents to method handlers for the type.
        /// </summary>
        private static Dictionary<UIEvent, Action<EventListenerVM>[]> BuildMethodEventMap(Type type)
        {
            var temp = new Dictionary<UIEvent, List<Action<EventListenerVM>>>();
            var methods = type.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            for (int i = 0; i < methods.Length; i++)
            {
                var method = methods[i];

                // Skip property accessors, operators, etc.
                if (method.IsSpecialName)
                {
                    continue;
                }

                var attrs = method.GetCustomAttributes(typeof(EventListenerAttribute), true);
                if (attrs == null || attrs.Length == 0)
                {
                    continue;
                }

                // Only parameterless void methods supported as handlers.
                if (method.ReturnType != typeof(void) || method.GetParameters().Length != 0)
                {
                    Log.Warning(
                        $"Ignoring method '{type.Name}.{method.Name}' with [EventListener]: handlers must be 'void' with no parameters."
                    );
                    continue;
                }

                for (int j = 0; j < attrs.Length; j++)
                {
                    var listener = attrs[j] as EventListenerAttribute;
                    if (listener?.Events == null)
                    {
                        continue;
                    }

                    for (int k = 0; k < listener.Events.Length; k++)
                    {
                        var e = listener.Events[k];

                        if (!temp.TryGetValue(e, out var list))
                        {
                            list = [];
                            temp[e] = list;
                        }

                        void Handler(EventListenerVM vm)
                        {
                            method.Invoke(vm, null);
                        }

                        list.Add(Handler);
                    }
                }
            }

            var result = new Dictionary<UIEvent, Action<EventListenerVM>[]>(temp.Count);
            foreach (var kvp in temp)
            {
                result[kvp.Key] = [.. kvp.Value];
            }

            return result;
        }
    }
}
