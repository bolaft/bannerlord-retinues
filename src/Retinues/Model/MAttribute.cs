using System;
using System.Collections.Generic;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace Retinues.Model
{
    internal interface IMAttributePersistent
    {
        void Collect(string key, MAttributePersistence.Data data);

        void Apply(string key, MAttributePersistence.Data data);
    }

    [SafeClass(IncludeDerived = true)]
    public class MAttribute<T> : IMAttributePersistent
    {
        readonly string _targetName;
        readonly object _baseInstance;

        readonly Func<object, T> _getter;
        readonly Action<object, T> _setter;

        readonly bool _persistent;
        bool _hasLocalChanges;

        public bool IsPersistent => _persistent;

        /// <summary>
        /// Creates an attribute that uses reflection to get/set the value.
        /// </summary>
        public MAttribute(object baseInstance, string targetName, bool persistent = false)
        {
            _baseInstance = baseInstance ?? throw new ArgumentNullException(nameof(baseInstance));
            _targetName = targetName ?? throw new ArgumentNullException(nameof(targetName));

            bool isProperty = Reflection.HasProperty(_baseInstance, _targetName);
            bool isField = Reflection.HasField(_baseInstance, _targetName);

            if (isField)
            {
                _getter = obj => Reflection.GetFieldValue<T>(obj, _targetName);
                _setter = (obj, value) => Reflection.SetFieldValue(obj, _targetName, value);
            }
            else if (isProperty)
            {
                _getter = obj => Reflection.GetPropertyValue<T>(obj, _targetName);
                _setter = (obj, value) => Reflection.SetPropertyValue(obj, _targetName, value);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Target '{_targetName}' is neither a field nor a property on type '{_baseInstance.GetType().Name}'."
                );
            }

            _persistent = persistent;

            if (_persistent)
            {
                MAttributePersistence.Register(this, _baseInstance, _targetName);
            }
        }

        /// <summary>
        /// Creates an attribute that gets/sets the value using the given delegates.
        /// </summary>
        public MAttribute(
            object baseInstance,
            Func<object, T> getter,
            Action<object, T> setter,
            string targetName = null,
            bool persistent = false
        )
        {
            _baseInstance = baseInstance ?? throw new ArgumentNullException(nameof(baseInstance));
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter ?? throw new ArgumentNullException(nameof(setter));
            _targetName = targetName ?? "<delegate>";
            _persistent = persistent;

            if (_persistent)
            {
                MAttributePersistence.Register(this, _baseInstance, _targetName);
            }
        }

        /// <summary>
        /// Gets the value of the attribute.
        /// </summary>
        public T Get()
        {
            return _getter(_baseInstance);
        }

        /// <summary>
        /// Sets the value of the attribute.
        /// </summary>
        public void Set(T value)
        {
            SetInternal(value, fromPersistence: false);
        }

        /// <summary>
        /// Sets the value of the attribute and marks it as dirty if not from persistence.
        /// </summary>
        void SetInternal(T value, bool fromPersistence)
        {
            _setter(_baseInstance, value);

            if (_persistent && !fromPersistence)
            {
                _hasLocalChanges = true;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                IMAttributePersistent API               //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        void IMAttributePersistent.Collect(string key, MAttributePersistence.Data data)
        {
            if (!_persistent || data == null)
                return;

            // Only persist if we either changed this attribute in this session,
            // or if it was already persisted before.
            bool alreadyDirty = data.Dirty.ContainsKey(key);
            if (!_hasLocalChanges && !alreadyDirty)
                return;

            var value = Get();
            var type = typeof(T);

            void MarkDirty()
            {
                data.Dirty[key] = true;
                _hasLocalChanges = false;
            }

            // Basic value types.
            if (type == typeof(int))
            {
                data.Ints[key] = (int)(object)value;
                MarkDirty();
                return;
            }

            if (type == typeof(bool))
            {
                data.Bools[key] = (bool)(object)value;
                MarkDirty();
                return;
            }

            if (type == typeof(float))
            {
                data.Floats[key] = (float)(object)value;
                MarkDirty();
                return;
            }

            if (type == typeof(TextObject))
            {
                if (value is TextObject to)
                {
                    data.Strings[key] = to.Value ?? string.Empty;
                }
                else
                {
                    data.Strings.Remove(key);
                }

                MarkDirty();

                return;
            }

            if (type == typeof(string))
            {
                data.Strings[key] = (string)(object)value ?? string.Empty;
                MarkDirty();
                return;
            }

            // Simple type example: Dictionary<string, int>.
            if (IsStringIntDictionaryType(type))
            {
                if (value is not Dictionary<string, int> dict || dict.Count == 0)
                {
                    data.DictStringInt.Remove(key);
                }
                else
                {
                    data.DictStringInt[key] = new Dictionary<string, int>(dict);
                }

                MarkDirty();

                return;
            }

            // MBObjectBase types are persisted as StringId.
            if (typeof(MBObjectBase).IsAssignableFrom(type))
            {
                if (value is not MBObjectBase mb || string.IsNullOrEmpty(mb.StringId))
                {
                    data.MbObjectIds.Remove(key);
                }
                else
                {
                    data.MbObjectIds[key] = mb.StringId;
                }

                MarkDirty();

                return;
            }

            // Any other class type is unsupported by design.
            if (type.IsClass)
            {
                Log.Error(
                    $"MAttribute: Cannot persist member '{_targetName}' on '{_baseInstance.GetType().Name}' because type '{type.FullName}' is not supported."
                );
            }
        }

        void IMAttributePersistent.Apply(string key, MAttributePersistence.Data data)
        {
            if (!_persistent || data == null)
                return;

            var type = typeof(T);

            if (type == typeof(int))
            {
                if (data.Ints.TryGetValue(key, out var i))
                    SetInternal((T)(object)i, fromPersistence: true);
                return;
            }

            if (type == typeof(bool))
            {
                if (data.Bools.TryGetValue(key, out var b))
                    SetInternal((T)(object)b, fromPersistence: true);
                return;
            }

            if (type == typeof(float))
            {
                if (data.Floats.TryGetValue(key, out var f))
                    SetInternal((T)(object)f, fromPersistence: true);
                return;
            }

            if (type == typeof(TextObject))
            {
                if (data.Strings.TryGetValue(key, out var s))
                {
                    var to = new TextObject(s ?? string.Empty);
                    SetInternal((T)(object)to, fromPersistence: true);
                }

                return;
            }

            if (type == typeof(string))
            {
                if (data.Strings.TryGetValue(key, out var s))
                    SetInternal((T)(object)s, fromPersistence: true);
                return;
            }

            if (IsStringIntDictionaryType(type))
            {
                if (data.DictStringInt.TryGetValue(key, out var src))
                {
                    var copy = new Dictionary<string, int>(src);
                    SetInternal((T)(object)copy, fromPersistence: true);
                }

                return;
            }

            if (typeof(MBObjectBase).IsAssignableFrom(type))
            {
                if (!data.MbObjectIds.TryGetValue(key, out var stringId))
                    return;

                if (string.IsNullOrEmpty(stringId))
                {
                    SetInternal(default, fromPersistence: true);
                    return;
                }

                var manager = MBObjectManager.Instance;
                if (manager == null)
                    return;

                // Generic overload with MBObjectBase constraint is fine here.
                var objBase = manager.GetObject<MBObjectBase>(stringId);
                if (objBase == null)
                {
                    Log.Warn(
                        $"MAttribute: Failed to resolve MBObjectBase '{stringId}' for member '{_targetName}' on '{_baseInstance.GetType().Name}'."
                    );
                    return;
                }

                // We already checked typeof(MBObjectBase).IsAssignableFrom(type),
                // so this cast is safe at runtime.
                if (objBase is T typed)
                    SetInternal(typed, fromPersistence: true);
                else
                    SetInternal((T)(object)objBase, fromPersistence: true);

                return;
            }
        }

        static bool IsStringIntDictionaryType(Type type)
        {
            if (!type.IsGenericType)
                return false;

            var def = type.GetGenericTypeDefinition();
            if (def != typeof(Dictionary<,>))
                return false;

            var args = type.GetGenericArguments();
            return args[0] == typeof(string) && args[1] == typeof(int);
        }
    }

    [SafeClass(IncludeDerived = true)]
    public static class MAttributePersistence
    {
        public sealed class Data
        {
            [SaveableField(1)]
            public Dictionary<string, int> Ints = [];

            [SaveableField(2)]
            public Dictionary<string, bool> Bools = [];

            [SaveableField(3)]
            public Dictionary<string, float> Floats = [];

            [SaveableField(4)]
            public Dictionary<string, string> Strings = [];

            [SaveableField(5)]
            public Dictionary<string, string> MbObjectIds = [];

            [SaveableField(6)]
            public Dictionary<string, Dictionary<string, int>> DictStringInt = [];

            [SaveableField(7)]
            public Dictionary<string, bool> Dirty = [];
        }

        static readonly Dictionary<string, IMAttributePersistent> _attributes = [];
        static Data _data = new();

        internal static void Reset()
        {
            _attributes.Clear();
            _data = new Data();
        }

        internal static void Register<T>(
            MAttribute<T> attribute,
            object baseInstance,
            string targetName
        )
        {
            if (attribute == null || baseInstance == null || string.IsNullOrEmpty(targetName))
                return;

            var key = BuildKey(baseInstance, targetName);
            _attributes[key] = attribute;

            // If we already have loaded data for this key, apply it immediately.
            if (_data != null)
            {
                ((IMAttributePersistent)attribute).Apply(key, _data);
            }
        }

        internal static void Sync(IDataStore dataStore)
        {
            if (dataStore == null)
                return;

            // 1) Collect current values into _data.
            CollectAll();

            // 2) Let TW save/load _data.
            dataStore.SyncData("ret_model_attributes", ref _data);

            // 3) Push loaded values back into any registered attributes.
            ApplyAll();
        }

        static void CollectAll()
        {
            foreach (var pair in _attributes)
            {
                pair.Value.Collect(pair.Key, _data);
            }
        }

        static void ApplyAll()
        {
            foreach (var pair in _attributes)
            {
                pair.Value.Apply(pair.Key, _data);
            }
        }

        internal static string BuildKey(object baseInstance, string targetName)
        {
            if (baseInstance is MBObjectBase mb)
            {
                var typeName = mb.GetType().Name;
                var stringId = mb.StringId ?? string.Empty;
                return $"{typeName}:{stringId}:{targetName}";
            }

            // Fallback for non MBObjectBase owners. This is not stable across sessions
            // but we also do not expect to persist them.
            var ownerType = baseInstance.GetType().Name;
            var hash = baseInstance.GetHashCode();
            return $"{ownerType}:{hash}:{targetName}";
        }
    }
}
