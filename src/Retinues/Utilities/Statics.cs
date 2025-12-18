using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Retinues.Utilities
{
    /// <summary>
    /// Tags a static "clear" method that should run when a new game session starts/loads.
    /// Method signature must be: static void MethodName()
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class StaticClearActionAttribute : Attribute
    {
        /// <summary>
        /// Lower runs first.
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// Optional friendly name for logging.
        /// </summary>
        public string Name { get; set; } = null;
    }

    /// <summary>
    /// Discovers and exposes static clear actions tagged with [StaticClearAction].
    /// </summary>
    public static class Statics
    {
        private static readonly object _lock = new();
        private static List<Action> _clearActions;
        private static bool _built;

        /// <summary>
        /// List of discovered clear actions (each action is wrapped in a try/catch and logs failures).
        /// </summary>
        public static IReadOnlyList<Action> ClearActions
        {
            get
            {
                EnsureBuilt();
                return _clearActions;
            }
        }

        /// <summary>
        /// Forces rediscovery of clear actions (rarely needed; mainly for debugging).
        /// </summary>
        public static void InvalidateClearActions()
        {
            lock (_lock)
            {
                _built = false;
                _clearActions = null;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Discovery                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void EnsureBuilt()
        {
            if (_built)
                return;

            lock (_lock)
            {
                if (_built)
                    return;

                _clearActions = BuildClearActions(Assembly.GetExecutingAssembly());
                _built = true;

                Log.Debug(
                    $"Statics: discovered {_clearActions.Count} [StaticClearAction] methods."
                );
            }
        }

        private static List<Action> BuildClearActions(params Assembly[] assemblies)
        {
            var entries = new List<(int order, string id, Action action)>();

            if (assemblies == null || assemblies.Length == 0)
                assemblies = new[] { Assembly.GetExecutingAssembly() };

            foreach (var asm in assemblies.Where(a => a != null).Distinct())
            {
                foreach (var t in SafeGetTypes(asm))
                {
                    MethodInfo[] methods;
                    try
                    {
                        methods = t.GetMethods(Reflection.Flags);
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var mi in methods)
                    {
                        StaticClearActionAttribute attr = null;
                        try
                        {
                            attr = mi.GetCustomAttribute<StaticClearActionAttribute>(
                                inherit: false
                            );
                        }
                        catch
                        {
                            // ignore
                        }

                        if (attr == null)
                            continue;

                        if (!IsValidClearMethod(mi))
                        {
                            Log.Warn(
                                $"Statics: ignoring [StaticClearAction] {t.FullName}.{mi.Name} (must be static void with no params)."
                            );
                            continue;
                        }

                        var id = !string.IsNullOrEmpty(attr.Name)
                            ? attr.Name
                            : $"{t.FullName}.{mi.Name}";

                        // Wrap in try/catch so SubModule can just "clear()".
                        void action()
                        {
                            try
                            {
                                Log.Info($"Statics: running clear action: {id}");
                                mi.Invoke(null, null);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Statics: clear action failed: {id} | {ex}");
                            }
                        }

                        entries.Add((attr.Order, id, action));
                    }
                }
            }

            return
            [
                .. entries
                    .OrderBy(e => e.order)
                    .ThenBy(e => e.id, StringComparer.Ordinal)
                    .Select(e => e.action),
            ];
        }

        private static bool IsValidClearMethod(MethodInfo mi)
        {
            if (mi == null)
                return false;
            if (!mi.IsStatic)
                return false;
            if (mi.ContainsGenericParameters)
                return false;
            if (mi.ReturnType != typeof(void))
                return false;

            ParameterInfo[] ps;
            try
            {
                ps = mi.GetParameters();
            }
            catch
            {
                return false;
            }

            return ps.Length == 0;
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly asm)
        {
            try
            {
                return asm.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types?.Where(t => t != null) ?? [];
            }
            catch
            {
                return [];
            }
        }
    }
}
