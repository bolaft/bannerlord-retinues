using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Retinues.Model
{
    /// <summary>
    /// Base model for non-MBObjectBase backed objects with shared reflection
    /// and cache helpers.
    /// </summary>
    public abstract class Model<TModel, TBase>
        where TModel : Model<TModel, TBase>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Static state                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool _staticInitialized;
        private static readonly object _staticInitLock = new();

        protected static Dictionary<string, ReflectedEntry> _reflected;
        protected static Dictionary<string, CacheStore> _cacheStores;
        protected static Dictionary<string, List<string>> _cacheInvalidationMap;

        protected sealed class ReflectedEntry
        {
            public string MemberName;
            public MemberInfo Member;
            public Func<TBase, object> Getter;
            public Action<TBase, object> Setter;
        }

        protected sealed class CacheStore
        {
            public string PropertyName;
            public Type ValueType;
            public IDictionary Map;
        }

        private static void EnsureStaticInitialized()
        {
            if (_staticInitialized)
                return;

            lock (_staticInitLock)
            {
                if (_staticInitialized)
                    return;

                _reflected = DiscoverReflectedProperties();
                DiscoverCachedProperties(out _cacheStores, out _cacheInvalidationMap);

                _staticInitialized = true;
            }
        }

        private static Dictionary<string, ReflectedEntry> DiscoverReflectedProperties()
        {
            var result = new Dictionary<string, ReflectedEntry>();

            var modelType = typeof(TModel);
            var baseType = typeof(TBase);
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var props = modelType.GetProperties(bindingFlags);

            foreach (var prop in props)
            {
                var attr = prop.GetCustomAttribute<ReflectedAttribute>(inherit: true);
                if (attr == null)
                    continue;

                var memberName = string.IsNullOrEmpty(attr.MemberName)
                    ? prop.Name
                    : attr.MemberName;

                MemberInfo member =
                    baseType.GetProperty(memberName, bindingFlags)
                    ?? (MemberInfo)baseType.GetField(memberName, bindingFlags);

                if (member == null)
                    continue;

                Func<TBase, object> getter = null;
                Action<TBase, object> setter = null;

                MethodInfo customSetter = null;
                if (!string.IsNullOrEmpty(attr.SetterName))
                {
                    customSetter = baseType.GetMethod(
                        attr.SetterName,
                        bindingFlags,
                        binder: null,
                        types: new[] { prop.PropertyType },
                        modifiers: null
                    );

                    if (customSetter != null)
                        setter = (obj, value) => customSetter.Invoke(obj, new[] { value });
                }

                if (member is PropertyInfo baseProp)
                {
                    if (baseProp.CanRead)
                        getter = obj => baseProp.GetValue(obj);

                    if (setter == null)
                    {
                        var setMethod = baseProp.GetSetMethod(true);
                        if (setMethod != null)
                            setter = (obj, value) => baseProp.SetValue(obj, value);
                    }
                }
                else if (member is FieldInfo baseField)
                {
                    getter = obj => baseField.GetValue(obj);

                    setter ??= (obj, value) => baseField.SetValue(obj, value);
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
            var props = typeof(TModel).GetProperties(bindingFlags);

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
                        list = new List<string>();
                        invalidation[inv] = list;
                    }

                    if (!list.Contains(prop.Name))
                        list.Add(prop.Name);
                }
            }
        }

        protected static void InvalidateCachesFor(string invalidatorPropertyName, string cacheKey)
        {
            if (string.IsNullOrEmpty(invalidatorPropertyName) || string.IsNullOrEmpty(cacheKey))
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
                    if (map.Contains(cacheKey))
                        map.Remove(cacheKey);
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Instance                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static int _nextId;

        private TBase _base;
        private bool _initialized;
        private string _cacheKey;

        protected string CacheKey
        {
            get
            {
                EnsureInitialized();
                if (_cacheKey == null)
                    _cacheKey = GetCacheKey();
                return _cacheKey;
            }
        }

        public TBase Base
        {
            get
            {
                EnsureInitialized();
                return _base;
            }
        }

        protected Model() { }

        protected virtual string GetCacheKey()
        {
            var id = System.Threading.Interlocked.Increment(ref _nextId);
            return id.ToString();
        }

        protected void InitializeFromBase(TBase baseObject)
        {
            if (baseObject == null)
                throw new ArgumentNullException(nameof(baseObject));

            if (_initialized)
            {
                if (!ReferenceEquals(_base, baseObject))
                    throw new InvalidOperationException(
                        "Model is already initialized with a different base object."
                    );
                return;
            }

            _base = baseObject;
            _initialized = true;
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException(
                    "Model is not initialized. Use the designated factory instead of new."
                );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Per-Property Helpers                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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
            var key = CacheKey;
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
            EnsureInitialized();

            if (string.IsNullOrEmpty(propertyName))
                return;

            EnsureStaticInitialized();

            if (_cacheStores != null && _cacheStores.TryGetValue(propertyName, out var store))
            {
                var map = store.Map;
                var key = CacheKey;
                if (!string.IsNullOrEmpty(key) && map.Contains(key))
                    map.Remove(key);
            }
        }

        protected T GetRef<T>([CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            EnsureInitialized();
            EnsureStaticInitialized();

            if (!_reflected.TryGetValue(propertyName, out var entry) || entry.Getter == null)
                return default;

            var value = entry.Getter(Base);
            if (value == null)
                return default;

            return (T)value;
        }

        protected void SetRef<T>(T value, [CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            EnsureInitialized();
            EnsureStaticInitialized();

            if (!_reflected.TryGetValue(propertyName, out var entry) || entry.Setter == null)
                return;

            entry.Setter(Base, value);

            InvalidateCachesFor(propertyName, CacheKey);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equality                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            return obj is TModel other && ReferenceEquals(Base, other.Base);
        }

        public override int GetHashCode()
        {
            var key = CacheKey;
            return key != null ? key.GetHashCode() : 0;
        }
    }
}
