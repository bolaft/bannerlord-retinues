using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Retinues.Utilities
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                       Attributes                       //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Attribute to wrap a method or property accessor with a try/catch via Harmony finalizer.
    /// Allows specifying fallback value and whether to swallow exceptions.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Method | AttributeTargets.Property,
        Inherited = false,
        AllowMultiple = false
    )]
    public sealed class SafeMethodAttribute(
        object fallback = null,
        bool swallow = true,
        Type fallbackType = null
    ) : Attribute
    {
        // Primitive/string/enum fallback (ignored for void).
        public object Fallback { get; } = fallback;

        // If set, try Activator.CreateInstance(fallbackType) and use it as the fallback
        public Type FallbackType { get; } = fallbackType;

        // If false, rethrow after logging.
        public bool Swallow { get; } = swallow;
    }

    /// <summary>
    /// Attribute to mark a class for auto-wrapping its methods and accessors with safe finalizers.
    /// Supports configuration for fallback values and scope.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class SafeClassAttribute : Attribute
    {
        // Scope switches
        public bool PublicOnly { get; set; } = false;
        public bool IncludeAccessors { get; set; } = true;
        public bool IncludeDerived { get; set; } = true;
        public bool SwallowByDefault { get; set; } = true;

        // Common fallbacks
        public bool UseIntFallback { get; set; } = true;
        public int IntFallback { get; set; } = 0;
        public bool UseLongFallback { get; set; } = true;
        public long LongFallback { get; set; } = 0L;
        public bool UseFloatFallback { get; set; } = true;
        public float FloatFallback { get; set; } = 0f;
        public bool UseDoubleFallback { get; set; } = true;
        public double DoubleFallback { get; set; } = 0d;
        public bool UseBoolFallback { get; set; } = true;
        public bool BoolFallback { get; set; } = false;
        public bool UseStringFallback { get; set; } = true;
        public string StringFallback { get; set; } = "";

        // Arrays
        public bool UseEmptyArrayFallback { get; set; } = true;
        public bool UseEmptyEnumerableFallback { get; set; } = true;
        public Type OpenGenericListFallback { get; set; } = typeof(List<>);
    }

    /// <summary>
    /// Attribute to mark a method as unsafe (excluded from safe patching).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class UnsafeMethodAttribute : Attribute { }

    /// <summary>
    /// Attribute to mark a property as unsafe (excluded from safe patching).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class UnsafePropertyAttribute : Attribute { }

    /// <summary>
    /// Harmony patcher for safe method/property wrappers.
    /// Applies finalizers to catch exceptions and provide fallback values.
    /// </summary>
    public static class SafeMethodPatcher
    {
        private static bool _applied;

        // Cache per accessor/method: (fallback object, fallback type, swallow, class config)
        private static readonly ConcurrentDictionary<MethodBase, Behavior> _behaviorCache = new();

        private struct Behavior
        {
            public object Fallback; // explicit object
            public Type FallbackType; // explicit concrete or open generic
            public bool Swallow;
            public SafeClassAttribute ClassCfg; // may be null
        }

        /// <summary>
        /// Applies safe patching to all eligible methods and properties in the given assemblies.
        /// </summary>
        public static void ApplyAll(Harmony harmony, params Assembly[] assemblies)
        {
            if (harmony == null)
                throw new ArgumentNullException(nameof(harmony));
            if (_applied)
                return;
            _applied = true;

            if (assemblies == null || assemblies.Length == 0)
                assemblies = [Assembly.GetExecutingAssembly()];

            var allTypes = assemblies.SelectMany(SafeGetTypes).ToArray();

            // 1) Explicit member-level [SafeMethod]
            foreach (var t in allTypes)
            {
                PatchExplicitSafeMethods(harmony, t);
                PatchExplicitSafeProperties(harmony, t);
            }

            // 2) Class-wide [SafeClass]
            foreach (var t in allTypes)
            {
                var cls = t.GetCustomAttribute<SafeClassAttribute>();
                if (cls == null)
                    continue;

                IEnumerable<Type> targets = [t];
                if (cls.IncludeDerived)
                    targets = targets.Concat(allTypes.Where(x => x != t && x.IsSubclassOf(t)));

                foreach (var tt in targets.Distinct())
                    PatchTypeByClassRule(harmony, tt, cls);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Explicit                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void PatchExplicitSafeMethods(Harmony harmony, Type type)
        {
            var flags =
                BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.DeclaredOnly;
            foreach (var m in type.GetMethods(flags))
            {
                var a = m.GetCustomAttribute<SafeMethodAttribute>();
                if (a == null)
                    continue;
                PatchOne(
                    harmony,
                    m,
                    new Behavior
                    {
                        Fallback = a.Fallback,
                        FallbackType = a.FallbackType,
                        Swallow = a.Swallow,
                    }
                );
            }
        }

        private static void PatchExplicitSafeProperties(Harmony harmony, Type type)
        {
            var flags =
                BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.DeclaredOnly;
            foreach (var p in type.GetProperties(flags))
            {
                var propA = p.GetCustomAttribute<SafeMethodAttribute>();
                if (propA != null)
                {
                    if (p.GetMethod != null)
                        PatchOne(
                            harmony,
                            p.GetMethod,
                            new Behavior
                            {
                                Fallback = propA.Fallback,
                                FallbackType = propA.FallbackType,
                                Swallow = propA.Swallow,
                            }
                        );
                    if (p.SetMethod != null)
                        PatchOne(
                            harmony,
                            p.SetMethod,
                            new Behavior
                            {
                                Fallback = propA.Fallback,
                                FallbackType = propA.FallbackType,
                                Swallow = propA.Swallow,
                            }
                        );
                }
                else
                {
                    var getA = p.GetMethod?.GetCustomAttribute<SafeMethodAttribute>();
                    if (getA != null && p.GetMethod != null)
                        PatchOne(
                            harmony,
                            p.GetMethod,
                            new Behavior
                            {
                                Fallback = getA.Fallback,
                                FallbackType = getA.FallbackType,
                                Swallow = getA.Swallow,
                            }
                        );

                    var setA = p.SetMethod?.GetCustomAttribute<SafeMethodAttribute>();
                    if (setA != null && p.SetMethod != null)
                        PatchOne(
                            harmony,
                            p.SetMethod,
                            new Behavior
                            {
                                Fallback = setA.Fallback,
                                FallbackType = setA.FallbackType,
                                Swallow = setA.Swallow,
                            }
                        );
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Class-wide                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void PatchTypeByClassRule(Harmony harmony, Type type, SafeClassAttribute cfg)
        {
            var vis = cfg.PublicOnly
                ? BindingFlags.Public
                : BindingFlags.Public | BindingFlags.NonPublic;
            var flags =
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly | vis;

            foreach (var m in type.GetMethods(flags))
            {
                if (m.IsSpecialName)
                    continue; // accessors handled below
                if (m.GetCustomAttribute<UnsafeMethodAttribute>() != null)
                    continue;
                if (!IsHarmonyPatchable(m))
                    continue;

                var overrideAttr = m.GetCustomAttribute<SafeMethodAttribute>();
                var behavior =
                    overrideAttr != null
                        ? new Behavior
                        {
                            Fallback = overrideAttr.Fallback,
                            FallbackType = overrideAttr.FallbackType,
                            Swallow = overrideAttr.Swallow,
                            ClassCfg = cfg,
                        }
                        : new Behavior { Swallow = cfg.SwallowByDefault, ClassCfg = cfg };

                PatchOne(harmony, m, behavior);
            }

            if (!cfg.IncludeAccessors)
                return;

            foreach (var p in type.GetProperties(flags))
            {
                if (p.GetCustomAttribute<UnsafePropertyAttribute>() != null)
                    continue;

                var propA = p.GetCustomAttribute<SafeMethodAttribute>();

                if (p.GetMethod != null)
                {
                    var accA = p.GetMethod.GetCustomAttribute<SafeMethodAttribute>() ?? propA;
                    if (accA != null)
                        PatchOne(
                            harmony,
                            p.GetMethod,
                            new Behavior
                            {
                                Fallback = accA.Fallback,
                                FallbackType = accA.FallbackType,
                                Swallow = accA.Swallow,
                                ClassCfg = cfg,
                            }
                        );
                    else
                        PatchOne(
                            harmony,
                            p.GetMethod,
                            new Behavior { Swallow = cfg.SwallowByDefault, ClassCfg = cfg }
                        );
                }

                if (p.SetMethod != null)
                {
                    var accA = p.SetMethod.GetCustomAttribute<SafeMethodAttribute>() ?? propA;
                    if (accA != null)
                        PatchOne(
                            harmony,
                            p.SetMethod,
                            new Behavior
                            {
                                Fallback = accA.Fallback,
                                FallbackType = accA.FallbackType,
                                Swallow = accA.Swallow,
                                ClassCfg = cfg,
                            }
                        );
                    else
                        PatchOne(
                            harmony,
                            p.SetMethod,
                            new Behavior { Swallow = cfg.SwallowByDefault, ClassCfg = cfg }
                        );
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Core Patching                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void PatchOne(Harmony harmony, MethodBase method, Behavior behavior)
        {
            // already-patched guard (unchanged)
            var info = Harmony.GetPatchInfo(method);
            if (info?.Finalizers?.Any(p => p.owner == harmony.Id) == true)
                return;

            _behaviorCache[method] = behavior;

            HarmonyMethod finalizer =
                (method is MethodInfo mi && mi.ReturnType != typeof(void))
                    ? new HarmonyMethod(
                        typeof(SafeMethodPatcher)
                            .GetMethod(
                                nameof(FinalizerGeneric),
                                BindingFlags.Static | BindingFlags.NonPublic
                            )!
                            .MakeGenericMethod(mi.ReturnType)
                    )
                    : new HarmonyMethod(
                        typeof(SafeMethodPatcher).GetMethod(
                            nameof(FinalizerVoid),
                            BindingFlags.Static | BindingFlags.NonPublic
                        )
                    );

            try
            {
                harmony.Patch(method, finalizer: finalizer);
            }
            catch (Exception) { }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Finalizers                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static Exception FinalizerVoid(Exception __exception, MethodBase __originalMethod)
        {
            if (__exception == null)
                return null;
            var beh = GetBehavior(__originalMethod);
            LogException(__originalMethod, __exception);
            return beh.Swallow ? null : __exception;
        }

        private static Exception FinalizerGeneric<T>(
            Exception __exception,
            ref T __result,
            MethodBase __originalMethod
        )
        {
            if (__exception == null)
                return null;

            var beh = GetBehavior(__originalMethod);
            LogException(__originalMethod, __exception);

            if (beh.Swallow)
            {
                if (TryResolveFallback(typeof(T), beh, out var val) && val is T t)
                    __result = t;
                else
                    __result = default;
                return null;
            }
            return __exception;
        }

        private static Behavior GetBehavior(MethodBase method)
        {
            if (_behaviorCache.TryGetValue(method, out var b))
                return b;

            // Try member-level attribute (rare path; we normally cache at patch time)
            var a = method.GetCustomAttribute<SafeMethodAttribute>();
            if (a != null)
                return new Behavior
                {
                    Fallback = a.Fallback,
                    FallbackType = a.FallbackType,
                    Swallow = a.Swallow,
                };

            // Try property-level attribute
            if (
                (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
                && method.DeclaringType != null
            )
            {
                var propName = method.Name.Substring(4);
                var prop = method.DeclaringType.GetProperty(
                    propName,
                    BindingFlags.Instance
                        | BindingFlags.Static
                        | BindingFlags.Public
                        | BindingFlags.NonPublic
                );
                var pa = prop?.GetCustomAttribute<SafeMethodAttribute>();
                if (pa != null)
                    return new Behavior
                    {
                        Fallback = pa.Fallback,
                        FallbackType = pa.FallbackType,
                        Swallow = pa.Swallow,
                    };
            }

            // Default behavior
            return new Behavior { Swallow = true };
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Fallbacks                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool TryResolveFallback(Type returnType, Behavior beh, out object value)
        {
            // 1) Explicit object fallback (per-method/property)
            if (beh.Fallback != null)
            {
                value = beh.Fallback;
                return true;
            }

            // 2) Explicit FallbackType (per-method/property) -> construct
            if (beh.FallbackType != null)
            {
                if (TryConstruct(beh.FallbackType, returnType, out value))
                    return true;
            }

            // 3) Class-level defaults by return type
            if (beh.ClassCfg != null)
            {
                var cfg = beh.ClassCfg;

                // Arrays: T[] -> Array.Empty<T>()
                if (
                    cfg.UseEmptyArrayFallback
                    && returnType.IsArray
                    && returnType.GetArrayRank() == 1
                )
                {
                    var tItem = returnType.GetElementType();
                    value = typeof(Array)
                        .GetMethod(nameof(Array.Empty))!
                        .MakeGenericMethod(tItem!)
                        .Invoke(null, null);
                    return true;
                }

                // IEnumerable<T> families -> OpenGenericListFallback<T>()
                if (
                    cfg.UseEmptyEnumerableFallback
                    && TryGetEnumerableItemType(returnType, out var elemT)
                )
                {
                    var open = cfg.OpenGenericListFallback ?? typeof(List<>);
                    if (open.IsGenericTypeDefinition && open.GetGenericArguments().Length == 1)
                    {
                        var concrete = open.MakeGenericType(elemT);
                        if (TryConstruct(concrete, returnType, out value))
                            return true;
                    }
                }

                // Primitives / common types
                if (returnType == typeof(int) && cfg.UseIntFallback)
                {
                    value = cfg.IntFallback;
                    return true;
                }
                if (returnType == typeof(long) && cfg.UseLongFallback)
                {
                    value = cfg.LongFallback;
                    return true;
                }
                if (returnType == typeof(float) && cfg.UseFloatFallback)
                {
                    value = cfg.FloatFallback;
                    return true;
                }
                if (returnType == typeof(double) && cfg.UseDoubleFallback)
                {
                    value = cfg.DoubleFallback;
                    return true;
                }
                if (returnType == typeof(bool) && cfg.UseBoolFallback)
                {
                    value = cfg.BoolFallback;
                    return true;
                }
                if (returnType == typeof(string) && cfg.UseStringFallback)
                {
                    value = cfg.StringFallback;
                    return true;
                }
            }

            value = null;
            return false;
        }

        private static bool TryConstruct(Type toConstruct, Type returnType, out object value)
        {
            value = null;
            try
            {
                if (toConstruct.ContainsGenericParameters)
                    return false;

                var obj = Activator.CreateInstance(toConstruct);
                if (obj == null)
                    return false;

                // Either the constructed type IS the return type, or it's assignable to it.
                if (returnType.IsAssignableFrom(toConstruct))
                {
                    value = obj;
                    return true;
                }
            }
            catch { }
            return false;
        }

        private static bool TryGetEnumerableItemType(Type t, out Type item)
        {
            item = null;
            if (t.IsArray)
            {
                item = t.GetElementType();
                return item != null;
            }

            if (t.IsGenericType)
            {
                // direct IEnumerable<T> or common family
                var def = t.GetGenericTypeDefinition();
                if (
                    def == typeof(IEnumerable<>)
                    || def == typeof(IList<>)
                    || def == typeof(ICollection<>)
                    || def == typeof(IReadOnlyList<>)
                    || def == typeof(IReadOnlyCollection<>)
                )
                {
                    item = t.GetGenericArguments()[0];
                    return true;
                }
            }

            // search implemented interfaces for IEnumerable<T>
            var ienum = t.GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                );
            if (ienum != null)
            {
                item = ienum.GetGenericArguments()[0];
                return true;
            }
            return false;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Utils                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void LogException(MethodBase method, Exception ex)
        {
            try
            {
                var owner = method.DeclaringType?.FullName ?? "<unknown>";
                var sig = $"{owner}.{method.Name}";
                Log.Exception(ex, caller: sig);
            }
            catch { }
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly asm)
        {
            try
            {
                return asm.GetTypes();
            }
            catch (ReflectionTypeLoadException rtle)
            {
                return rtle.Types.Where(t => t != null)!;
            }
        }

        private static bool IsHarmonyPatchable(MethodBase m)
        {
            if (m.IsAbstract || m.GetMethodBody() == null)
                return false;

            if (m.DeclaringType != null && m.DeclaringType.ContainsGenericParameters)
                return false;

            if (m is MethodInfo mi)
            {
                if (mi.ContainsGenericParameters)
                    return false;

                var rt = mi.ReturnType;
                if (rt.IsGenericParameter || rt.ContainsGenericParameters)
                    return false;

                if (IsByRefLikeOrUnsupported(rt))
                    return false;
            }

            foreach (var p in m.GetParameters())
            {
                var pt = p.ParameterType;

                if (pt.IsGenericParameter || pt.ContainsGenericParameters)
                    return false;

                if (IsByRefLikeOrUnsupported(pt))
                    return false;
            }

            // Optional: avoid compiler-generated state machines (iterator/async MoveNext)
            if (m.GetCustomAttribute<CompilerGeneratedAttribute>() != null && m.Name == "MoveNext")
                return false;

            return true;

            static bool IsByRefLikeOrUnsupported(Type t)
            {
                // Skip pointers
                if (t.IsPointer)
                    return true;

                // Quick filters for Span<T>/ReadOnlySpan<T> on net472 where IsByRefLike isn't available
                var n = t.IsByRef ? t.GetElementType()?.FullName : t.FullName;
                if (n == null)
                    return false;

                return n.StartsWith("System.Span`1", StringComparison.Ordinal)
                    || n.StartsWith("System.ReadOnlySpan`1", StringComparison.Ordinal)
                    || n == "System.TypedReference";
            }
        }
    }
}
