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
    /// Marks a property on a WrappedObject as persistent state.
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
    /// Global registry of all wrapped types that contain persistent data.
    /// </summary>
    internal static class WrappedRegistry
    {
        private static readonly List<IWrappedSync> _entries = [];

        internal static IReadOnlyList<IWrappedSync> Entries => _entries;

        internal static void Register(IWrappedSync entry)
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
    public abstract class WrappedObject<TWrapper, TBase> : IEquatable<TWrapper>
        where TWrapper : WrappedObject<TWrapper, TBase>
        where TBase : MBObjectBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Static state                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private sealed class PropertyEntry
        {
            public string Key;
            public Type ValueType;
            public IDictionary Map;
        }

        private sealed class WrappedSyncRegistration : IWrappedSync
        {
            public void Sync(IDataStore dataStore)
            {
                SyncAll(dataStore);
            }
        }

        private static readonly Dictionary<string, TWrapper> _instances = [];
        private static readonly Dictionary<string, PropertyEntry> _properties;
        private static readonly MethodInfo _syncDataMethod;

        static WrappedObject()
        {
            _properties = DiscoverWrappedProperties();
            _syncDataMethod = typeof(IDataStore).GetMethod("SyncData");

            WrappedRegistry.Register(new WrappedSyncRegistration());
        }

        private static Dictionary<string, PropertyEntry> DiscoverWrappedProperties()
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

        /// <summary>
        /// Called by the global registry to save/load all persistent state
        /// for this wrapper type.
        /// </summary>
        internal static void SyncAll(IDataStore dataStore)
        {
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
        /// Returns all wrapped instances for this type, ensuring every TBase is wrapped.
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
        /// Returns all wrapped instances that satisfy the given predicate.
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
        protected WrappedObject() { }

        protected void InitializeFromBase(TBase baseObject)
        {
            if (baseObject == null)
                throw new ArgumentNullException(nameof(baseObject));

            if (_initialized)
            {
                if (!ReferenceEquals(_base, baseObject))
                    throw new InvalidOperationException("WrappedObject is already initialized.");
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
                    "WrappedObject is not initialized. Use TWrapper.Get(...) instead of new."
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
            WrappedObject<TWrapper, TBase> left,
            WrappedObject<TWrapper, TBase> right
        )
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left is null || right is null)
                return false;

            return left.StringId == right.StringId;
        }

        public static bool operator !=(
            WrappedObject<TWrapper, TBase> left,
            WrappedObject<TWrapper, TBase> right
        )
        {
            return !(left == right);
        }
    }
}
