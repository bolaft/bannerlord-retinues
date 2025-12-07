using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Retinues.Wrappers
{
    /// <summary>
    /// Marks a property on a Wrapper as persistent state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class WrapDataAttribute(string key = null) : Attribute
    {
        /// <summary>
        /// Optional explicit key used for persistence; defaults to property name.
        /// </summary>
        public string Key { get; } = key;
    }

    /// <summary>
    /// Marks a Wrapper property as a direct proxy to a member on TBase.
    /// If MemberName is null, the wrapper property name is used.
    /// If SetterName is provided, it will be used as the setter method on TBase.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ReflectedAttribute(string memberName = null, string setterName = null)
        : Attribute
    {
        public string MemberName { get; } = memberName;
        public string SetterName { get; } = setterName;
    }

    /// <summary>
    /// Marks a property on a Wrapper as cached and declares invalidators.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CachedAttribute(params string[] invalidators) : Attribute
    {
        public string[] Invalidators { get; } = invalidators;
    }

    /// <summary>
    /// Global registry of all wrapper types that contain persistent data.
    /// </summary>
    internal static class WrapperRegistry
    {
        private static readonly List<IWrapperSync> _entries = [];

        internal static IReadOnlyList<IWrapperSync> Entries => _entries;

        internal static void Register(IWrapperSync entry)
        {
            if (entry == null)
                return;

            _entries.Add(entry);
        }
    }

    /// <summary>
    /// Base wrapper for MBObjectBase derivatives with per-StringId persistent state
    /// defined via properties annotated with [WrapData].
    /// </summary>
    public abstract class Wrapper<TWrapper, TBase> : IEquatable<TWrapper>
        where TWrapper : Wrapper<TWrapper, TBase>
        where TBase : MBObjectBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Static state                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool _staticInitialized;
        private static readonly object _staticInitLock = new();

        private static Dictionary<string, PropertyEntry> _properties;

        private sealed class PropertyEntry
        {
            public string Key;
            public Type ValueType;
            public IDictionary Map;
        }

        private static Dictionary<string, ReflectedEntry> _reflected;

        private sealed class ReflectedEntry
        {
            public string MemberName;
            public MemberInfo Member;
            public Func<TBase, object> Getter;
            public Action<TBase, object> Setter;
        }

        private sealed class CacheStore
        {
            public string PropertyName;
            public Type ValueType;
            public IDictionary Map;
        }

        private static Dictionary<string, CacheStore> _cacheStores;
        private static Dictionary<string, List<string>> _cacheInvalidationMap;

        private sealed class WrapperSyncRegistration : IWrapperSync
        {
            public void Sync(IDataStore dataStore)
            {
                SyncAll(dataStore);
            }
        }

        private static readonly Dictionary<string, TWrapper> _instances = [];
        private static MethodInfo _syncDataMethod;

        private static void EnsureStaticInitialized()
        {
            if (_staticInitialized)
                return;

            lock (_staticInitLock)
            {
                if (_staticInitialized)
                    return;

                // If something goes wrong here, we want it to throw _here_,
                // not deep in a type initializer.
                _properties = DiscoverWrapperProperties();
                _reflected = DiscoverReflectedProperties();
                _syncDataMethod = typeof(IDataStore).GetMethod("SyncData");

                // Cache-related setup
                DiscoverCachedProperties(out _cacheStores, out _cacheInvalidationMap);

                WrapperRegistry.Register(new WrapperSyncRegistration());

                _staticInitialized = true;
            }
        }

        private static Dictionary<string, PropertyEntry> DiscoverWrapperProperties()
        {
            var result = new Dictionary<string, PropertyEntry>();

            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var props = typeof(TWrapper).GetProperties(bindingFlags);

            foreach (var prop in props)
            {
                if (!prop.CanRead || !prop.CanWrite)
                    continue;

                var attr = prop.GetCustomAttribute<WrapDataAttribute>(inherit: true);
                if (attr == null)
                    continue;

                var key = attr.Key ?? prop.Name;
                var valueType = prop.PropertyType;
                var mapType = typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType);
                var map = (IDictionary)Activator.CreateInstance(mapType);

                result[key] = new PropertyEntry
                {
                    Key = key,
                    ValueType = valueType,
                    Map = map,
                };
            }

            return result;
        }

        private static Dictionary<string, ReflectedEntry> DiscoverReflectedProperties()
        {
            var result = new Dictionary<string, ReflectedEntry>();

            var wrapperType = typeof(TWrapper);
            var baseType = typeof(TBase);
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var props = wrapperType.GetProperties(bindingFlags);

            foreach (var prop in props)
            {
                var attr = prop.GetCustomAttribute<ReflectedAttribute>(inherit: true);
                if (attr == null)
                    continue;

                // Default to wrapper property name when member name is omitted.
                var memberName = string.IsNullOrEmpty(attr.MemberName)
                    ? prop.Name
                    : attr.MemberName;

                // Try property on TBase first, then field.
                MemberInfo member =
                    baseType.GetProperty(memberName, bindingFlags)
                    ?? (MemberInfo)baseType.GetField(memberName, bindingFlags);

                if (member == null)
                {
                    // No backing member; you might want to plug logging here.
                    continue;
                }

                Func<TBase, object> getter = null;
                Action<TBase, object> setter = null;

                // Optional custom setter method.
                MethodInfo customSetter = null;
                if (!string.IsNullOrEmpty(attr.SetterName))
                {
                    // Expect a method on TBase: void SetterName(<wrapper property type>)
                    customSetter = baseType.GetMethod(
                        attr.SetterName,
                        bindingFlags,
                        binder: null,
                        types: [prop.PropertyType],
                        modifiers: null
                    );

                    if (customSetter != null)
                    {
                        setter = (obj, value) => customSetter.Invoke(obj, [value]);
                    }
                }

                if (member is PropertyInfo baseProp)
                {
                    if (baseProp.CanRead)
                        getter = obj => baseProp.GetValue(obj);

                    // Only use property setter if no custom setter was provided / found
                    if (setter == null)
                    {
                        var setMethod = baseProp.GetSetMethod(true); // include non-public
                        if (setMethod != null)
                            setter = (obj, value) => baseProp.SetValue(obj, value);
                    }
                }
                else if (member is FieldInfo baseField)
                {
                    getter = baseField.GetValue;

                    setter ??= baseField.SetValue;
                }

                result[prop.Name] = new ReflectedEntry
                {
                    MemberName = memberName,
                    Member = member,
                    Getter = getter,
                    Setter = setter,
                };
            }

            return result;
        }

        private static void DiscoverCachedProperties(
            out Dictionary<string, CacheStore> stores,
            out Dictionary<string, List<string>> invalidation
        )
        {
            stores = new Dictionary<string, CacheStore>();
            invalidation = new Dictionary<string, List<string>>();

            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var props = typeof(TWrapper).GetProperties(bindingFlags);

            foreach (var prop in props)
            {
                if (!prop.CanRead)
                    continue;

                var attr = prop.GetCustomAttribute<CachedAttribute>(inherit: true);
                if (attr == null)
                    continue;

                var valueType = prop.PropertyType;
                var mapType = typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType);
                var map = (IDictionary)Activator.CreateInstance(mapType);

                stores[prop.Name] = new CacheStore
                {
                    PropertyName = prop.Name,
                    ValueType = valueType,
                    Map = map,
                };

                var invalidators = attr.Invalidators;
                if (invalidators == null || invalidators.Length == 0)
                    continue;

                for (int i = 0; i < invalidators.Length; i++)
                {
                    var inv = invalidators[i];
                    if (string.IsNullOrEmpty(inv))
                        continue;

                    if (!invalidation.TryGetValue(inv, out var list))
                    {
                        list = [];
                        invalidation[inv] = list;
                    }

                    if (!list.Contains(prop.Name))
                        list.Add(prop.Name);
                }
            }
        }

        private static void InvalidateCachesFor(string invalidatorPropertyName, string stringId)
        {
            if (string.IsNullOrEmpty(invalidatorPropertyName) || string.IsNullOrEmpty(stringId))
                return;

            EnsureStaticInitialized();

            if (
                _cacheInvalidationMap == null
                || !_cacheInvalidationMap.TryGetValue(
                    invalidatorPropertyName,
                    out var affectedProps
                )
                || affectedProps.Count == 0
            )
                return;

            for (int i = 0; i < affectedProps.Count; i++)
            {
                var cachedPropName = affectedProps[i];
                if (string.IsNullOrEmpty(cachedPropName))
                    continue;

                if (_cacheStores != null && _cacheStores.TryGetValue(cachedPropName, out var store))
                {
                    var map = store.Map;
                    if (map.Contains(stringId))
                        map.Remove(stringId);
                }
            }
        }

        /// <summary>
        /// Called by the global registry to save/load all persistent state
        /// for this wrapper type.
        /// </summary>
        internal static void SyncAll(IDataStore dataStore)
        {
            EnsureStaticInitialized();

            if (_syncDataMethod == null || _properties.Count == 0)
                return;

            var typeName = typeof(TWrapper).FullName;

            foreach (var entry in _properties.Values)
            {
                var dict = entry.Map;
                var dictType = dict.GetType();

                // Skip empty dictionaries when saving; avoids cluttering saves.
                if (dataStore.IsSaving && dict.Count == 0)
                    continue;

                var key = $"{typeName}.{entry.Key}";
                var args = new object[] { key, dict };

                var generic = _syncDataMethod.MakeGenericMethod(dictType);
                generic.Invoke(dataStore, args);

                entry.Map = (IDictionary)args[1];
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Factory                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets (or creates) a wrapper from a base object instance.
        /// </summary>
        public static TWrapper Get(TBase baseObject)
        {
            if (baseObject == null)
                return null;

            EnsureStaticInitialized();

            var id = baseObject.StringId;
            if (!string.IsNullOrEmpty(id) && _instances.TryGetValue(id, out var existing))
                return existing;

            var wrapper = (TWrapper)Activator.CreateInstance(typeof(TWrapper));
            wrapper.InitializeFromBase(baseObject);
            return wrapper;
        }

        /// <summary>
        /// Gets (or creates) a wrapper from a StringId.
        /// </summary>
        public static TWrapper Get(string stringId)
        {
            if (string.IsNullOrEmpty(stringId))
                return null;

            EnsureStaticInitialized();

            if (_instances.TryGetValue(stringId, out var existing))
                return existing;

            _instances.Where(kv => kv.Key == stringId).ToList();

            var baseObj = MBObjectManager.Instance.GetObject<TBase>(stringId);
            if (baseObj == null)
                return null;

            var wrapper = (TWrapper)Activator.CreateInstance(typeof(TWrapper));
            wrapper.InitializeFromBase(baseObj);
            return wrapper;
        }

        /// <summary>
        /// Returns all wrapper instances for this type, ensuring every TBase is wrapped.
        /// </summary>
        public static IReadOnlyCollection<TWrapper> All
        {
            get
            {
                // Get all base objects of the appropriate type.
                var objects = MBObjectManager.Instance.GetObjectTypeList<TBase>();
                if (objects == null)
                    return [];

                // Ensure all base objects are wrapped before returning instances.
                foreach (var baseObject in objects)
                    Get(baseObject);

                return _instances.Values;
            }
        }

        /// <summary>
        /// Returns all wrapper instances that satisfy the given predicate.
        /// </summary>
        public static List<TWrapper> Find(Func<TWrapper, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var results = new List<TWrapper>();

            foreach (var wrapper in All)
            {
                if (predicate(wrapper))
                    results.Add(wrapper);
            }

            return results;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Instance                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private TBase _base;
        private bool _initialized;

        /// <summary>
        /// String identifier of the underlying object.
        /// </summary>
        public string StringId
        {
            get
            {
                EnsureInitialized();
                return _base.StringId;
            }
        }

        /// <summary>
        /// Underlying TaleWorlds object.
        /// </summary>
        public TBase Base
        {
            get
            {
                EnsureInitialized();
                return _base;
            }
        }

        /// <summary>
        /// Constructs an uninitialized wrapper. Use TWrapper.Get(...) to obtain initialized instances.
        /// </summary>
        protected Wrapper() { }

        protected void InitializeFromBase(TBase baseObject)
        {
            if (baseObject == null)
                throw new ArgumentNullException(nameof(baseObject));

            if (_initialized)
            {
                if (!ReferenceEquals(_base, baseObject))
                    throw new InvalidOperationException("Wrapper is already initialized.");
                return;
            }

            _base = baseObject;
            _initialized = true;

            var id = _base.StringId;
            if (!string.IsNullOrEmpty(id))
                _instances[id] = (TWrapper)this;
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException(
                    "Wrapper is not initialized. Use TWrapper.Get(...) instead of new."
                );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Per-Property Helpers                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected T Get<T>([CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            if (!_properties.TryGetValue(propertyName, out var entry))
                return default;

            var map = entry.Map;
            if (!map.Contains(StringId))
                return default;

            return (T)map[StringId];
        }

        protected void Set<T>(T value, [CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            if (!_properties.TryGetValue(propertyName, out var entry))
                return;

            entry.Map[StringId] = value;

            InvalidateCachesFor(propertyName, StringId);
        }

        protected T GetCached<T>(
            Func<T> valueFactory,
            [CallerMemberName] string propertyName = null
        )
        {
            if (valueFactory == null)
                throw new ArgumentNullException(nameof(valueFactory));

            EnsureInitialized();
            EnsureStaticInitialized();

            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            if (_cacheStores == null || !_cacheStores.TryGetValue(propertyName, out var store))
                return valueFactory();

            var map = store.Map;
            var key = StringId;
            if (string.IsNullOrEmpty(key))
                return valueFactory();

            if (map.Contains(key))
                return (T)map[key];

            var value = valueFactory();
            map[key] = value;
            return value;
        }

        protected void InvalidateCache([CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(StringId))
                return;

            EnsureStaticInitialized();

            if (_cacheStores != null && _cacheStores.TryGetValue(propertyName, out var store))
            {
                var map = store.Map;
                if (map.Contains(StringId))
                    map.Remove(StringId);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Reflected Base Access                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Reads a value from the underlying TBase member mapped with [Reflected].
        /// </summary>
        protected T GetRef<T>([CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            EnsureInitialized();

            if (!_reflected.TryGetValue(propertyName, out var entry) || entry.Getter == null)
                return default;

            var value = entry.Getter(Base);
            if (value == null)
                return default;

            return (T)value;
        }

        /// <summary>
        /// Writes a value to the underlying TBase member mapped with [Reflected].
        /// Uses the custom setter method when provided.
        /// </summary>
        protected void SetRef<T>(T value, [CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            EnsureInitialized();

            if (!_reflected.TryGetValue(propertyName, out var entry) || entry.Setter == null)
                return;

            entry.Setter(Base, value);

            InvalidateCachesFor(propertyName, StringId);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equality                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool Equals(TWrapper other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return StringId == other.StringId;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            return obj is TWrapper other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringId != null ? StringId.GetHashCode() : 0;
        }

        public static bool operator ==(
            Wrapper<TWrapper, TBase> left,
            Wrapper<TWrapper, TBase> right
        )
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left is null || right is null)
                return false;

            return left.StringId == right.StringId;
        }

        public static bool operator !=(
            Wrapper<TWrapper, TBase> left,
            Wrapper<TWrapper, TBase> right
        )
        {
            return !(left == right);
        }
    }
}
