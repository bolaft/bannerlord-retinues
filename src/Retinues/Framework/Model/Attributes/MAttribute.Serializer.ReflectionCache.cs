using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using TaleWorlds.ObjectSystem;

namespace Retinues.Framework.Model.Attributes
{
    /// <summary>
    /// Serialization support for MAttribute<T>.
    /// </summary>
    public partial class MAttribute<T>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Reflection Cache                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        static readonly object CacheLock = new();

        // Single lock used for all serializer reflection caches.
        // These lookups can be hit on save/load and while building UI lists, so caching keeps them cheap.

        static MethodInfo _mbGetObjectGeneric;

        static readonly Dictionary<Type, bool> IsWrapperCache = [];
        static readonly Dictionary<Type, MethodInfo> WrapperGetCache = [];

        /// <summary>
        /// Gets the generic MBObjectManager.GetObject<T>(string) method.
        /// </summary>
        static MethodInfo GetMBGetObjectGeneric()
        {
            if (_mbGetObjectGeneric != null)
                return _mbGetObjectGeneric;

            lock (CacheLock)
            {
                if (_mbGetObjectGeneric != null)
                    return _mbGetObjectGeneric;

                _mbGetObjectGeneric = typeof(MBObjectManager)
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m =>
                        m.Name == "GetObject"
                        && m.IsGenericMethodDefinition
                        && m.GetGenericArguments().Length == 1
                        && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType == typeof(string)
                    );

                return _mbGetObjectGeneric;
            }
        }

        /// <summary>
        /// Determines whether the given type is a WBase<,> wrapper type.
        /// </summary>
        static bool IsWBaseType(Type type)
        {
            if (type == null)
                return false;

            lock (CacheLock)
            {
                if (IsWrapperCache.TryGetValue(type, out var cached))
                    return cached;
            }

            var t = type;
            while (t != null && t != typeof(object))
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(WBase<,>))
                {
                    lock (CacheLock)
                        IsWrapperCache[type] = true;

                    return true;
                }

                t = t.BaseType;
            }

            lock (CacheLock)
                IsWrapperCache[type] = false;

            return false;
        }

        /// <summary>
        /// Gets the static Get(string) method of a WBase<,> wrapper type.
        /// </summary>
        static MethodInfo GetWrapperGetMethod(Type wrapperType)
        {
            if (wrapperType == null)
                return null;

            lock (CacheLock)
            {
                if (WrapperGetCache.TryGetValue(wrapperType, out var mi))
                    return mi;
            }

            var found = wrapperType.GetMethod(
                "Get",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy,
                binder: null,
                types: [typeof(string)],
                modifiers: null
            );

            lock (CacheLock)
                WrapperGetCache[wrapperType] = found;

            return found;
        }

        /// <summary>
        /// Resolves an MBObjectBase by type and StringId.
        /// </summary>
        static MBObjectBase ResolveMBObject(Type objectType, string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id == "null")
                return null;

            var mgr = MBObjectManager.Instance;
            if (mgr == null)
                return null;

            var getObjectGeneric = GetMBGetObjectGeneric();
            if (getObjectGeneric == null)
                return null;

            try
            {
                return getObjectGeneric.MakeGenericMethod(objectType).Invoke(mgr, [id])
                    as MBObjectBase;
            }
            catch
            {
                return null;
            }
        }
    }
}
