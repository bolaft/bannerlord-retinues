using System;
using System.Collections.Generic;
using System.Reflection;
using Retinues.Utilities;
using TaleWorlds.Library;

namespace Retinues.Editor.VM
{
    /// <summary>
    /// Base ViewModel with shared editor state (faction and character) and
    /// attribute-driven event wiring.
    /// </summary>
    [SafeClass(IncludeDerived = true)]
    public abstract class BaseVM : ViewModel
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Global State                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal static State State = new();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected BaseVM()
        {
            EventManager.Register(this);
        }

        public override void OnFinalize()
        {
            EventManager.Unregister(this);
            base.OnFinalize();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Event handling                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Type -> event -> property names
        private static readonly Dictionary<
            Type,
            Dictionary<UIEvent, string[]>
        > _propertyEventMapCache = [];

        // Type -> event -> method handlers
        private static readonly Dictionary<
            Type,
            Dictionary<UIEvent, Action<BaseVM>[]>
        > _methodEventMapCache = [];

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

            var type = GetType();

            // 1) Properties -> OnPropertyChanged at end of burst.
            var propertyMap = GetOrBuildPropertyEventMap(type);
            if (propertyMap.TryGetValue(e, out var properties) && properties != null)
            {
                for (int i = 0; i < properties.Length; i++)
                {
                    context.RequestNotify(this, properties[i]);
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

        private static Dictionary<UIEvent, string[]> GetOrBuildPropertyEventMap(Type type)
        {
            if (_propertyEventMapCache.TryGetValue(type, out var map))
            {
                return map;
            }

            map = BuildPropertyEventMap(type);
            _propertyEventMapCache[type] = map;
            return map;
        }

        private static Dictionary<UIEvent, Action<BaseVM>[]> GetOrBuildMethodEventMap(Type type)
        {
            if (_methodEventMapCache.TryGetValue(type, out var map))
            {
                return map;
            }

            map = BuildMethodEventMap(type);
            _methodEventMapCache[type] = map;
            return map;
        }

        private static Dictionary<UIEvent, string[]> BuildPropertyEventMap(Type type)
        {
            var temp = new Dictionary<UIEvent, List<string>>();
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

                    for (int k = 0; k < listener.Events.Length; k++)
                    {
                        var e = listener.Events[k];

                        if (!temp.TryGetValue(e, out var list))
                        {
                            list = [];
                            temp[e] = list;
                        }

                        if (!list.Contains(name))
                        {
                            list.Add(name);
                        }
                    }
                }
            }

            var result = new Dictionary<UIEvent, string[]>(temp.Count);
            foreach (var kvp in temp)
            {
                result[kvp.Key] = [.. kvp.Value];
            }

            return result;
        }

        private static Dictionary<UIEvent, Action<BaseVM>[]> BuildMethodEventMap(Type type)
        {
            var temp = new Dictionary<UIEvent, List<Action<BaseVM>>>();
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
                    Log.Warn(
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

                        void Handler(BaseVM vm)
                        {
                            method.Invoke(vm, null);
                        }

                        list.Add(Handler);
                    }
                }
            }

            var result = new Dictionary<UIEvent, Action<BaseVM>[]>(temp.Count);
            foreach (var kvp in temp)
            {
                result[kvp.Key] = [.. kvp.Value];
            }

            return result;
        }
    }
}
