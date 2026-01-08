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
    /// Tags a static field or property to be reset to its default value when a new game session starts/loads.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Property,
        Inherited = false,
        AllowMultiple = false
    )]
    public sealed class StaticClearAttribute : Attribute
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

                    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
                    //                    [StaticClearAction] methods          //
                    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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
                            Log.Warn(
                                $"Statics: ignoring invalid [StaticClearAction] {t.FullName}.{mi.Name} ({reason})."
                            );
                            continue;
                        }

                        var id = !string.IsNullOrEmpty(attr.Name)
                            ? attr.Name
                            : $"{t.FullName}.{mi.Name}";

                        void action()
                        {
                            try
                            {
                                Log.Debug($"Statics: running clear action: {id}");
                                mi.Invoke(null, null);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Statics: clear action failed: {id} | {ex}");
                            }
                        }

                        entries.Add((attr.Order, id, action));
                    }

                    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
                    //                    [StaticClear] fields                 //
                    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

                    FieldInfo[] fields;
                    try
                    {
                        fields = t.GetFields(Reflection.Flags);
                    }
                    catch
                    {
                        fields = null;
                    }

                    if (fields != null)
                    {
                        foreach (var fi in fields)
                        {
                            StaticClearAttribute attr = null;
                            try
                            {
                                attr = fi.GetCustomAttribute<StaticClearAttribute>(inherit: false);
                            }
                            catch
                            {
                                // ignore
                            }

                            if (attr == null)
                                continue;

                            if (!fi.IsStatic || fi.IsLiteral)
                            {
                                Log.Warn(
                                    $"Statics: ignoring [StaticClear] invalid field {t.FullName}.{fi.Name}."
                                );
                                continue;
                            }

                            var id = !string.IsNullOrEmpty(attr.Name)
                                ? attr.Name
                                : $"{t.FullName}.{fi.Name}";

                            void action()
                            {
                                try
                                {
                                    Log.Debug($"Statics: running clear action: {id}");

                                    object value = null;
                                    try
                                    {
                                        value = fi.GetValue(null);
                                    }
                                    catch
                                    {
                                        // ignore
                                    }

                                    // Preferred behavior for ref types:
                                    // - if non-null and supports Clear(): call it (even if readonly).
                                    if (
                                        !fi.FieldType.IsValueType
                                        && value != null
                                        && TryInvokeClear(value)
                                    )
                                        return;

                                    // Otherwise assign default/null if writable.
                                    if (fi.IsInitOnly)
                                    {
                                        // readonly field without Clear(): nothing sensible to do.
                                        Log.Warn(
                                            $"Statics: cannot assign to readonly field {t.FullName}.{fi.Name}."
                                        );
                                        return;
                                    }

                                    fi.SetValue(null, GetDefaultValue(fi.FieldType));
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"Statics: clear action failed: {id} | {ex}");
                                }
                            }

                            entries.Add((attr.Order, id, action));
                        }
                    }

                    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
                    //                    [StaticClear] properties             //
                    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

                    PropertyInfo[] props;
                    try
                    {
                        props = t.GetProperties(Reflection.Flags);
                    }
                    catch
                    {
                        props = null;
                    }

                    if (props != null)
                    {
                        foreach (var pi in props)
                        {
                            StaticClearAttribute attr = null;
                            try
                            {
                                attr = pi.GetCustomAttribute<StaticClearAttribute>(inherit: false);
                            }
                            catch
                            {
                                // ignore
                            }

                            if (attr == null)
                                continue;

                            // Ignore indexers.
                            try
                            {
                                if (pi.GetIndexParameters()?.Length > 0)
                                {
                                    Log.Warn(
                                        $"Statics: ignoring [StaticClear] indexer {t.FullName}.{pi.Name}."
                                    );
                                    continue;
                                }
                            }
                            catch
                            {
                                continue;
                            }

                            var id = !string.IsNullOrEmpty(attr.Name)
                                ? attr.Name
                                : $"{t.FullName}.{pi.Name}";

                            MethodInfo getter = null;
                            MethodInfo setter = null;

                            try
                            {
                                getter = pi.GetGetMethod(nonPublic: true);
                            }
                            catch
                            {
                                getter = null;
                            }

                            try
                            {
                                setter = pi.GetSetMethod(nonPublic: true);
                            }
                            catch
                            {
                                setter = null;
                            }

                            void action()
                            {
                                try
                                {
                                    Log.Debug($"Statics: running clear action: {id}");

                                    object value = null;
                                    var canRead = getter != null && getter.IsStatic;
                                    var canWrite = setter != null && setter.IsStatic;

                                    if (canRead)
                                    {
                                        try
                                        {
                                            value = getter.Invoke(null, null);
                                        }
                                        catch
                                        {
                                            value = null;
                                        }

                                        // Preferred behavior for ref types:
                                        // - if non-null and supports Clear(): call it (even if no setter).
                                        if (
                                            !pi.PropertyType.IsValueType
                                            && value != null
                                            && TryInvokeClear(value)
                                        )
                                            return;
                                    }

                                    // Otherwise assign default/null if possible.
                                    if (canWrite)
                                    {
                                        setter.Invoke(null, [GetDefaultValue(pi.PropertyType)]);
                                        return;
                                    }

                                    // Fallback: auto-property backing field
                                    var backingName = $"<{pi.Name}>k__BackingField";
                                    FieldInfo backing = null;
                                    try
                                    {
                                        backing = t.GetField(backingName, Reflection.Flags);
                                    }
                                    catch
                                    {
                                        backing = null;
                                    }

                                    if (
                                        backing != null
                                        && backing.IsStatic
                                        && !backing.IsLiteral
                                        && !backing.IsInitOnly
                                    )
                                    {
                                        backing.SetValue(null, GetDefaultValue(pi.PropertyType));
                                        return;
                                    }

                                    // If we couldn't read earlier, try one last time to Clear() if it is a ref type.
                                    if (!canRead && !pi.PropertyType.IsValueType)
                                    {
                                        try
                                        {
                                            value =
                                                getter?.IsStatic == true
                                                    ? getter.Invoke(null, null)
                                                    : null;
                                        }
                                        catch
                                        {
                                            value = null;
                                        }

                                        if (value != null && TryInvokeClear(value))
                                            return;
                                    }

                                    Log.Warn(
                                        $"Statics: cannot clear non-writable property {t.FullName}.{pi.Name}."
                                    );
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
            }

            return
            [
                .. entries
                    .OrderBy(e => e.order)
                    .ThenBy(e => e.id, StringComparer.Ordinal)
                    .Select(e => e.action),
            ];
        }

        private static object GetDefaultValue(Type t)
        {
            if (t == null)
                return null;

            if (!t.IsValueType)
                return null;

            try
            {
                return Activator.CreateInstance(t);
            }
            catch
            {
                return null;
            }
        }

        private static bool TryInvokeClear(object instance)
        {
            if (instance == null)
                return false;

            try
            {
                var t = instance.GetType();

                // Any parameterless instance method named "Clear" returning void.
                var mi = t.GetMethod(
                    "Clear",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null
                );

                if (mi == null || mi.ReturnType != typeof(void))
                    return false;

                mi.Invoke(instance, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

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
