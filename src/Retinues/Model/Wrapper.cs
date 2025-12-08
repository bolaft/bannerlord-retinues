using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Retinues.Model
{
    /// <summary>
    /// Marks a property on a Wrapper as persistent state,
    /// optionally overriding the stored value type and key.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PersistentAttribute(Type valueType = null, string key = null) : Attribute
    {
        /// <summary>
        /// Optional explicit key used for persistence; defaults to property name.
        /// </summary>
        public string Key { get; } = key;

        /// <summary>
        /// Optional explicit value type for the backing map.
        /// If null, the property type is used.
        /// </summary>
        public Type ValueType { get; } = valueType;
    }

    /// <summary>
    /// Marks a property on a Model/Wrapper as a direct proxy to a member on TBase.
    /// If MemberName is null, the model property name is used.
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
    /// Marks a property on a Model/Wrapper as cached and declares invalidators.
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
    /// defined via properties annotated with [Persistent].
    /// Inherits Model for reflection and cached-property support.
    /// </summary>
    public abstract class Wrapper<TWrapper, TBase> : Model<TWrapper, TBase>
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

                // Persistence maps.
                _properties = DiscoverWrapperProperties();
                _syncDataMethod = typeof(IDataStore).GetMethod("SyncData");

                // Register this wrapper type for global persistence.
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

                var attr = prop.GetCustomAttribute<PersistentAttribute>(inherit: true);
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

            if (!string.IsNullOrEmpty(id))
                _instances[id] = wrapper;

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

            var baseObj = MBObjectManager.Instance.GetObject<TBase>(stringId);
            if (baseObj == null)
                return null;

            var wrapper = (TWrapper)Activator.CreateInstance(typeof(TWrapper));
            wrapper.InitializeFromBase(baseObj);

            _instances[stringId] = wrapper;
            return wrapper;
        }

        /// <summary>
        /// Returns all wrapper instances for this type, ensuring every TBase is wrapped.
        /// </summary>
        public static IReadOnlyCollection<TWrapper> All
        {
            get
            {
                EnsureStaticInitialized();

                var objects = MBObjectManager.Instance.GetObjectTypeList<TBase>();
                if (objects == null)
                    return [];

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

        /// <summary>
        /// String identifier of the underlying object.
        /// </summary>
        public string StringId => Base.StringId;

        /// <summary>
        /// Constructs an uninitialized wrapper. Use TWrapper.Get(...) to obtain initialized instances.
        /// </summary>
        protected Wrapper() { }

        /// <summary>
        /// Uses StringId as the cache key for Model's cached properties.
        /// </summary>
        protected override string GetCacheKey()
        {
            return StringId;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Per-Property Helpers                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets a persistent value (annotated with [Persistent]) for this wrapper instance.
        /// </summary>
        protected T Get<T>([CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            EnsureStaticInitialized();

            if (!_properties.TryGetValue(propertyName, out var entry))
                return default;

            var map = entry.Map;
            var id = StringId;
            if (string.IsNullOrEmpty(id) || !map.Contains(id))
                return default;

            return (T)map[id];
        }

        /// <summary>
        /// Sets a persistent value (annotated with [Persistent]) for this wrapper instance
        /// and invalidates any cached properties that declare it as an invalidator.
        /// </summary>
        protected void Set<T>(T value, [CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            EnsureStaticInitialized();

            if (!_properties.TryGetValue(propertyName, out var entry))
                return;

            var id = StringId;
            if (string.IsNullOrEmpty(id))
                return;

            entry.Map[id] = value;

            // Tie persistence to cached properties via Model's cache invalidation map.
            InvalidateCachesFor(propertyName, id);
        }

        /// <summary>
        /// Sets a reflected member on TBase and, if this property is marked as
        /// persistent, stores the value in the per-StringId map as well.
        /// </summary>
        protected new void SetRef<T>(T value, [CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            // 1) Persist if this property has a persistence entry.
            EnsureStaticInitialized();

            if (_properties != null && _properties.TryGetValue(propertyName, out var entry))
            {
                var id = StringId;
                if (!string.IsNullOrEmpty(id))
                {
                    entry.Map[id] = value;

                    // Tie persistence to cached properties (same as Set<T>).
                    InvalidateCachesFor(propertyName, id);
                }
            }

            // 2) Push to underlying TBase via Model's reflection helper.
            base.SetRef(value, propertyName);
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

            if (obj is not TWrapper other)
                return false;

            var a = StringId;
            var b = other.StringId;

            if (a == null || b == null)
                return ReferenceEquals(Base, other.Base);

            return a == b;
        }

        public bool Equals(TWrapper other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            var a = StringId;
            var b = other.StringId;

            if (a == null || b == null)
                return ReferenceEquals(Base, other.Base);

            return a == b;
        }

        public override int GetHashCode()
        {
            var id = StringId;
            return id != null ? id.GetHashCode() : (Base != null ? Base.GetHashCode() : 0);
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

            var a = left.StringId;
            var b = right.StringId;

            if (a == null || b == null)
                return ReferenceEquals(left.Base, right.Base);

            return a == b;
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
