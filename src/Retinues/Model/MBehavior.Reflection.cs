using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;

namespace Retinues.Model
{
    public sealed partial class MBehavior
    {
        static readonly Dictionary<string, Type> WrapperByBaseFullName = BuildWrapperTypeMap();

        static Dictionary<string, Type> BuildWrapperTypeMap()
        {
            var asm = typeof(MBehavior).Assembly;
            var map = new Dictionary<string, Type>(StringComparer.Ordinal);

            foreach (var t in asm.GetTypes())
            {
                if (!WrapperReflection.IsConcreteWrapperType(t))
                    continue;

                var wb = WrapperReflection.GetWBaseGeneric(t);
                if (wb == null)
                    continue;

                var baseArg = wb.GetGenericArguments()[1];
                if (baseArg != null && baseArg.FullName != null)
                    map[baseArg.FullName] = t;
            }

            return map;
        }

        static void ApplyXml(XElement root)
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

                ApplySingle(uid, payload);
            }
        }

        static void ApplySingle(string uid, string data)
        {
            if (string.IsNullOrEmpty(uid))
                return;

            var sep = uid.IndexOf(':');
            if (sep <= 0 || sep >= uid.Length - 1)
                return;

            var baseTypeFullName = uid.Substring(0, sep);
            var stringId = uid.Substring(sep + 1);

            if (!WrapperByBaseFullName.TryGetValue(baseTypeFullName, out var wrapperType))
                return;

            var wrapper = WrapperReflection.TryGetWrapperInstance(wrapperType, stringId);
            if (wrapper == null)
                return;

            WrapperReflection.TryDeserialize(wrapperType, wrapper, data);
        }

        static class WrapperReflection
        {
            static readonly object CacheLock = new();

            static readonly Dictionary<Type, Type> WBaseGenericCache = [];
            static readonly Dictionary<Type, PropertyInfo> AllPropertyCache = [];
            static readonly Dictionary<Type, PropertyInfo> UniqueIdPropertyCache = [];
            static readonly Dictionary<Type, MethodInfo> SerializeMethodCache = [];
            static readonly Dictionary<Type, MethodInfo> DeserializeMethodCache = [];
            static readonly Dictionary<Type, MethodInfo> GetMethodCache = [];

            public static bool IsConcreteWrapperType(Type t)
            {
                if (t == null)
                    return false;

                if (t.IsAbstract || !t.IsClass)
                    return false;

                return GetWBaseGeneric(t) != null;
            }

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

            public static IEnumerable TryGetAllEnumerable(Type wrapperType)
            {
                var wb = GetWBaseGeneric(wrapperType);
                if (wb == null)
                    return null;

                var allProp = GetAllProperty(wb);
                if (allProp == null)
                    return null;

                var allObj = allProp.GetValue(null);
                return allObj as IEnumerable;
            }

            static PropertyInfo GetAllProperty(Type wbaseGeneric)
            {
                lock (CacheLock)
                {
                    if (AllPropertyCache.TryGetValue(wbaseGeneric, out var p))
                        return p;
                }

                var prop = wbaseGeneric.GetProperty(
                    "All",
                    BindingFlags.Public | BindingFlags.Static
                );

                lock (CacheLock)
                    AllPropertyCache[wbaseGeneric] = prop;

                return prop;
            }

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

            public static object TryGetWrapperInstance(Type wrapperType, string stringId)
            {
                var get = GetGetMethod(wrapperType);
                if (get == null)
                    return null;

                return get.Invoke(null, [stringId]);
            }

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

            public static void TryDeserialize(Type wrapperType, object wrapperInstance, string data)
            {
                if (wrapperType == null || wrapperInstance == null)
                    return;

                var mi = GetDeserializeMethod(wrapperType);
                if (mi == null)
                    return;

                mi.Invoke(wrapperInstance, [data]);
            }

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
