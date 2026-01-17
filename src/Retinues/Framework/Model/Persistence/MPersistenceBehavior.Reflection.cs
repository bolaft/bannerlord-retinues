using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Retinues.Utilities;

namespace Retinues.Framework.Model.Persistence
{
    public sealed partial class MPersistenceBehavior
    {
        static Dictionary<string, Type> _wrapperByBaseFullName;

        static Dictionary<string, Type> WrapperByBaseFullName =>
            _wrapperByBaseFullName ??= BuildWrapperTypeMapSafe();

        /// <summary>
        /// Builds a map of base type full names to wrapper types.
        /// </summary>
        static Dictionary<string, Type> BuildWrapperTypeMapSafe()
        {
            var asm = typeof(MPersistenceBehavior).Assembly;
            var map = new Dictionary<string, Type>(StringComparer.Ordinal);

            Type[] types;

            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // This is the big one: some types couldn't be loaded.
                // Keep the ones we can, and log the loader exceptions.
                types = [.. ex.Types.Where(t => t != null)];

                if (ex.LoaderExceptions != null)
                {
                    for (int i = 0; i < ex.LoaderExceptions.Length; i++)
                    {
                        var le = ex.LoaderExceptions[i];
                        if (le != null)
                            Log.Warning(
                                $"Persistence: loader exception while scanning types: {le}"
                            );
                    }
                }
            }

            for (int i = 0; i < types.Length; i++)
            {
                var t = types[i];
                if (t == null)
                    continue;

                try
                {
                    if (!WrapperReflection.IsConcreteWrapperType(t))
                        continue;

                    var wb = WrapperReflection.GetWBaseGeneric(t);
                    if (wb == null)
                        continue;

                    var baseArg = wb.GetGenericArguments()[1];
                    if (baseArg?.FullName == null)
                        continue;

                    map[baseArg.FullName] = t;
                }
                catch (Exception e)
                {
                    Log.Warning(
                        $"Persistence: failed to register wrapper type '{t.FullName}': {e}"
                    );
                }
            }

            Log.Info($"Persistence: wrapper map built ({map.Count} entries).");
            return map;
        }

        /// <summary>
        /// Applies the XML root to restore persistence entries.
        /// </summary>
        static void ApplyXml(XElement root, bool allowDefer)
        {
            foreach (var el in root.Elements())
            {
                var uid = (string)el.Attribute("uid");
                if (string.IsNullOrEmpty(uid))
                    continue;

                string payload;

                if (el.Name.LocalName == "Entry")
                {
                    payload = el.Value ?? string.Empty;
                }
                else
                {
                    var copy = new XElement(el);
                    copy.SetAttributeValue("uid", null);
                    payload = copy.ToString(SaveOptions.DisableFormatting);
                }

                ApplySingle(uid, payload, allowDefer);
            }
        }

        /// <summary>
        /// Applies a single persistence entry by UID and data.
        /// </summary>
        static void ApplySingle(string uid, string data, bool allowDefer)
        {
            if (string.IsNullOrEmpty(uid))
                return;

            var sep = uid.IndexOf(':');
            if (sep <= 0 || sep >= uid.Length - 1)
                return;

            var baseTypeFullName = uid.Substring(0, sep);
            var stringId = uid.Substring(sep + 1);

            // Defer clan and kingdom restores until session is launched
            if (allowDefer && ShouldDefer(baseTypeFullName))
            {
                EnqueueDeferred(uid, data);
                return;
            }

            if (!WrapperByBaseFullName.TryGetValue(baseTypeFullName, out var wrapperType))
                return;

            object wrapper;

            try
            {
                wrapper = WrapperReflection.TryGetWrapperInstance(wrapperType, stringId);
            }
            catch (Exception ex)
            {
                Log.Warning(
                    $"MPersistence: failed to resolve wrapper instance for uid='{uid}' type='{wrapperType?.FullName}' id='{stringId}': {ex}"
                );
                return;
            }

            if (wrapper == null)
                return;

            using var scope = LoadingLogger.Begin(uid);

            try
            {
                WrapperReflection.TryDeserialize(wrapperType, wrapper, data);

                var line = scope.BuildLine();
                Log.Debug(line);
            }
            catch (Exception ex)
            {
                Log.Warning(
                    $"MPersistence: failed to deserialize uid='{uid}' type='{wrapperType?.FullName}' id='{stringId}': {ex}"
                );
            }
        }

        /// <summary>
        /// Determines if the given base type full name should defer persistence application.
        /// </summary>
        static bool ShouldDefer(string baseTypeFullName)
        {
            // No dependency on wrapper classes, only the base types
            return string.Equals(
                    baseTypeFullName,
                    typeof(TaleWorlds.CampaignSystem.Clan).FullName,
                    StringComparison.Ordinal
                )
                || string.Equals(
                    baseTypeFullName,
                    typeof(TaleWorlds.CampaignSystem.Kingdom).FullName,
                    StringComparison.Ordinal
                );
        }

        /// <summary>
        /// Reflection utilities for wrapper types.
        /// </summary>
        static class WrapperReflection
        {
            static readonly object CacheLock = new();

            static readonly Dictionary<Type, Type> WBaseGenericCache = [];
            static readonly Dictionary<Type, PropertyInfo> AllPropertyCache = [];
            static readonly Dictionary<Type, PropertyInfo> UniqueIdPropertyCache = [];
            static readonly Dictionary<Type, MethodInfo> SerializeMethodCache = [];
            static readonly Dictionary<Type, MethodInfo> DeserializeMethodCache = [];
            static readonly Dictionary<Type, MethodInfo> GetMethodCache = [];

            /// <summary>
            /// Determines if the given type is a concrete wrapper type.
            /// </summary>
            public static bool IsConcreteWrapperType(Type t)
            {
                if (t == null)
                    return false;

                if (t.IsAbstract || !t.IsClass)
                    return false;

                return GetWBaseGeneric(t) != null;
            }

            /// <summary>
            /// Gets the WBase<,> generic base type for the given wrapper type.
            /// </summary>
            public static Type GetWBaseGeneric(Type t)
            {
                if (t == null)
                    return null;

                lock (CacheLock)
                {
                    if (WBaseGenericCache.TryGetValue(t, out var cached))
                        return cached;
                }

                var bt = t;
                while (bt != null)
                {
                    if (bt.IsGenericType && bt.GetGenericTypeDefinition() == typeof(WBase<,>))
                    {
                        lock (CacheLock)
                            WBaseGenericCache[t] = bt;

                        return bt;
                    }

                    bt = bt.BaseType;
                }

                lock (CacheLock)
                    WBaseGenericCache[t] = null;

                return null;
            }

            static readonly Dictionary<Type, PropertyInfo> WrapperAllPropertyCache = [];

            /// <summary>
            /// Tries to get all instances of the given wrapper type.
            /// </summary>
            public static IEnumerable TryGetAllEnumerable(Type wrapperType)
            {
                var allProp = GetAllPropertyForWrapper(wrapperType);
                if (allProp == null)
                    return null;

                try
                {
                    return allProp.GetValue(null) as IEnumerable;
                }
                catch (Exception ex)
                {
                    Log.Warning(
                        $"MPersistence: failed to evaluate {wrapperType?.FullName}.All: {ex}"
                    );
                    return null;
                }
            }

            /// <summary>
            /// Gets the "All" property for the given wrapper type.
            /// </summary>
            static PropertyInfo GetAllPropertyForWrapper(Type wrapperType)
            {
                if (wrapperType == null)
                    return null;

                lock (CacheLock)
                {
                    if (WrapperAllPropertyCache.TryGetValue(wrapperType, out var cached))
                        return cached;
                }

                // Find all visible public static "All" properties (FlattenHierarchy can surface multiple).
                PropertyInfo best = null;
                try
                {
                    var props = wrapperType.GetProperties(
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy
                    );

                    var candidates = new List<PropertyInfo>();

                    for (int i = 0; i < props.Length; i++)
                    {
                        var p = props[i];
                        if (p == null)
                            continue;
                        if (!string.Equals(p.Name, "All", StringComparison.Ordinal))
                            continue;
                        if (!typeof(IEnumerable).IsAssignableFrom(p.PropertyType))
                            continue;

                        // Exclude indexers.
                        ParameterInfo[] idx;
                        try
                        {
                            idx = p.GetIndexParameters();
                        }
                        catch
                        {
                            continue;
                        }

                        if (idx != null && idx.Length != 0)
                            continue;

                        candidates.Add(p);
                    }

                    if (candidates.Count > 0)
                    {
                        // Prefer the property declared on the wrapper type itself (WClan/WKingdom "new All").
                        best = candidates.FirstOrDefault(p => p.DeclaringType == wrapperType);

                        // Else pick the closest declaring type in the inheritance chain (most-derived).
                        best ??= candidates
                            .OrderBy(p => GetInheritanceDistance(wrapperType, p.DeclaringType))
                            .FirstOrDefault();
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(
                        $"MPersistence: failed to resolve {wrapperType.FullName}.All: {ex}"
                    );
                }

                if (best != null)
                {
                    lock (CacheLock)
                        WrapperAllPropertyCache[wrapperType] = best;

                    return best;
                }

                // Fallback to WBase<,>.All (MBObjectManager-based)
                var wb = GetWBaseGeneric(wrapperType);
                var fallback = wb?.GetProperty("All", BindingFlags.Public | BindingFlags.Static);

                lock (CacheLock)
                    WrapperAllPropertyCache[wrapperType] = fallback;

                return fallback;
            }

            /// <summary>
            /// Gets the inheritance distance between two types.
            /// </summary>
            static int GetInheritanceDistance(Type from, Type declaring)
            {
                if (from == null || declaring == null)
                    return int.MaxValue;
                if (from == declaring)
                    return 0;

                var d = 0;
                var t = from;
                while (t != null)
                {
                    if (t == declaring)
                        return d;
                    d++;
                    t = t.BaseType;
                }

                return int.MaxValue;
            }

            /// <summary>
            /// Gets the unique ID of the given wrapper instance.
            /// </summary>
            public static string TryGetUniqueId(object wrapperInstance)
            {
                if (wrapperInstance == null)
                    return null;

                var t = wrapperInstance.GetType();

                var p = GetUniqueIdProperty(t);
                if (p == null)
                    return null;

                return p.GetValue(wrapperInstance, null) as string;
            }

            /// <summary>
            /// Gets the "UniqueId" property for the given wrapper type.
            /// </summary>
            static PropertyInfo GetUniqueIdProperty(Type t)
            {
                lock (CacheLock)
                {
                    if (UniqueIdPropertyCache.TryGetValue(t, out var p))
                        return p;
                }

                var prop = t.GetProperty(
                    "UniqueId",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy
                );

                lock (CacheLock)
                    UniqueIdPropertyCache[t] = prop;

                return prop;
            }

            /// <summary>
            /// Tries to serialize the given wrapper instance.
            /// </summary>
            public static string TrySerialize(object wrapperInstance)
            {
                if (wrapperInstance == null)
                    return null;

                var t = wrapperInstance.GetType();
                var mi = GetSerializeMethod(t);
                if (mi == null)
                    return null;

                return mi.Invoke(wrapperInstance, null) as string;
            }

            /// <summary>
            /// Gets the "Serialize" method for the given wrapper type.
            /// </summary>
            static MethodInfo GetSerializeMethod(Type t)
            {
                lock (CacheLock)
                {
                    if (SerializeMethodCache.TryGetValue(t, out var mi))
                        return mi;
                }

                var found = t.GetMethod(
                    "Serialize",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null
                );

                lock (CacheLock)
                    SerializeMethodCache[t] = found;

                return found;
            }

            /// <summary>
            /// Tries to get a wrapper instance of the given type by string ID.
            /// </summary>
            public static object TryGetWrapperInstance(Type wrapperType, string stringId)
            {
                var get = GetGetMethod(wrapperType);
                if (get == null)
                    return null;

                return get.Invoke(null, [stringId]);
            }

            /// <summary>
            /// Gets the "Get" method for the given wrapper type.
            /// </summary>
            static MethodInfo GetGetMethod(Type wrapperType)
            {
                lock (CacheLock)
                {
                    if (GetMethodCache.TryGetValue(wrapperType, out var mi))
                        return mi;
                }

                var found = wrapperType.GetMethod(
                    "Get",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy,
                    null,
                    [typeof(string)],
                    null
                );

                lock (CacheLock)
                    GetMethodCache[wrapperType] = found;

                return found;
            }

            /// <summary>
            /// Tries to deserialize the given data into the wrapper instance.
            /// </summary>
            public static void TryDeserialize(Type wrapperType, object wrapperInstance, string data)
            {
                if (wrapperType == null || wrapperInstance == null)
                    return;

                var mi = GetDeserializeMethod(wrapperType);
                if (mi == null)
                    return;

                // 🔒 This is a persistence restore, not a generic import
                MBase<IModel>.IsRestoringFromPersistence = true;
                try
                {
                    mi.Invoke(wrapperInstance, [data]);
                }
                finally
                {
                    MBase<IModel>.IsRestoringFromPersistence = false;
                }
            }

            /// <summary>
            /// Gets the "Deserialize" method for the given wrapper type.
            /// </summary>
            static MethodInfo GetDeserializeMethod(Type wrapperType)
            {
                lock (CacheLock)
                {
                    if (DeserializeMethodCache.TryGetValue(wrapperType, out var mi))
                        return mi;
                }

                var found = wrapperType.GetMethod(
                    "Deserialize",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                    null,
                    [typeof(string)],
                    null
                );

                lock (CacheLock)
                    DeserializeMethodCache[wrapperType] = found;

                return found;
            }
        }
    }
}
