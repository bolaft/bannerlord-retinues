using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Retinues.Utilities;

namespace Retinues.Framework.Runtime
{
    /// <summary>
    /// Tags a static "clear" method that should run when a new game session starts/loads.
    /// Method signature must be: static void MethodName()
    ///
    /// If Refresh is true, the action will also run again:
    /// - after a save is fully loaded (CampaignEvents.OnGameLoadedEvent)
    /// - after character creation ends (CampaignEvents.OnCharacterCreationIsOverEvent)
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

        /// <summary>
        /// If true, this clear action will also run on "refresh" points such as
        /// after game load and after character creation.
        /// </summary>
        public bool Refresh { get; set; } = false;
    }

    /// <summary>
    /// Discovers and exposes static clear actions tagged with [StaticClearAction].
    /// </summary>
    public static class Statics
    {
        private static readonly object _lock = new();
        private static List<Action> _clearActions;
        private static List<Action> _refreshActions;
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
        /// Subset of ClearActions that should also run again after game load and character creation.
        /// </summary>
        public static IReadOnlyList<Action> RefreshActions
        {
            get
            {
                EnsureBuilt();
                return _refreshActions;
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
                _refreshActions = null;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Discovery                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Ensures that clear actions have been discovered.
        /// </summary>
        private static void EnsureBuilt()
        {
            if (_built)
                return;

            lock (_lock)
            {
                if (_built)
                    return;

                (_clearActions, _refreshActions) = BuildClearActions(
                    Assembly.GetExecutingAssembly()
                );
                _built = true;

                Log.Debug(
                    $"Statics: discovered {_clearActions.Count} [StaticClearAction] methods ({_refreshActions.Count} refresh)."
                );
            }
        }

        /// <summary>
        /// Builds the list of clear actions from the given assemblies.
        /// </summary>
        private static (List<Action> all, List<Action> refresh) BuildClearActions(
            params Assembly[] assemblies
        )
        {
            var entries = new List<(int order, string id, bool refresh, Action action)>();

            if (assemblies == null || assemblies.Length == 0)
                assemblies = [Assembly.GetExecutingAssembly()];

            foreach (var asm in assemblies.Where(a => a != null).Distinct())
            {
                foreach (var t in SafeGetTypes(asm))
                {
                    if (t == null)
                        continue;

                    // Critical: do not scan open generic type definitions (e.g. WBase`2, BaseFaction`2).
                    // Closed concrete types (WCharacter, WClan, etc.) will still surface inherited static
                    // methods via FlattenHierarchy, so skipping these avoids bogus "invalid" warnings.
                    if (t.ContainsGenericParameters)
                        continue;

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

                        if (!TryValidateClearMethod(mi, out var reason))
                        {
                            Log.Warning(
                                $"Statics: ignoring invalid [StaticClearAction] {t.FullName}.{mi.Name} ({reason})."
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
                                mi.Invoke(null, null);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Statics: clear action failed: {id} | {ex}");
                            }
                        }

                        entries.Add((attr.Order, id, attr.Refresh, action));
                    }
                }
            }

            var ordered = entries
                .OrderBy(e => e.order)
                .ThenBy(e => e.id, StringComparer.Ordinal)
                .ToList();

            var all = ordered.Select(e => e.action).ToList();
            var refresh = ordered.Where(e => e.refresh).Select(e => e.action).ToList();

            return (all, refresh);
        }

        /// <summary>
        /// Validates that the given method info matches the expected signature for a clear action.
        /// </summary>
        private static bool TryValidateClearMethod(MethodInfo mi, out string reason)
        {
            reason = null;

            if (mi == null)
            {
                reason = "method info is null";
                return false;
            }

            var problems = new List<string>();

            if (!mi.IsStatic)
                problems.Add("not static");

            if (mi.ReturnType != typeof(void))
                problems.Add(
                    $"return type is '{mi.ReturnType?.Name ?? "<null>"}' (expected 'Void')"
                );

            // This becomes true when the declaring type is an open generic type definition.
            if (mi.ContainsGenericParameters)
            {
                var dt = mi.DeclaringType;
                if (dt != null && dt.ContainsGenericParameters)
                    problems.Add($"declared on open generic type '{dt.FullName}'");
                else
                    problems.Add("contains generic parameters");
            }

            try
            {
                var ps = mi.GetParameters();
                if (ps.Length != 0)
                    problems.Add($"has {ps.Length} parameter(s) (expected 0)");
            }
            catch
            {
                problems.Add("unable to read parameters");
            }

            if (problems.Count == 0)
                return true;

            reason =
                $"{string.Join("; ", problems)}; expected: static void MethodName() declared on a non-generic type with no params";
            return false;
        }

        /// <summary>
        /// Gets types from an assembly safely, handling ReflectionTypeLoadException.
        /// </summary>
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
