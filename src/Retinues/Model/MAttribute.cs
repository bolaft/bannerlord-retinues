using System;
using Retinues.Utilities;

namespace Retinues.Model
{
    [SafeClass(IncludeDerived = true)]
    public class MAttribute<T> : IPersistentAttribute
    {
        readonly string _targetName;
        readonly object _baseInstance;

        readonly Func<object, T> _getter;
        readonly Action<object, T> _setter;

        public MPersistencePriority Priority { get; }
        readonly MSerializer<T> _serializer;

        T _storedValue;

        public string Name => _targetName;
        public bool IsStored { get; private set; }

        public bool IsPersistent { get; }
        public bool IsDirty { get; private set; }

        public void Touch() => IsDirty = true;

        public string OwnerKey { get; }
        public string AttributeKey { get; }

        bool IPersistentAttribute.IsDirty => IsDirty;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Constructors                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Creates an attribute that uses reflection to get/set the value.
        /// </summary>
        public MAttribute(
            object baseInstance,
            string targetName,
            string ownerKey = null,
            bool persistent = false,
            MPersistencePriority priority = MPersistencePriority.Normal,
            MSerializer<T> serializer = null
        )
        {
            _baseInstance = baseInstance ?? throw new ArgumentNullException(nameof(baseInstance));
            _targetName = targetName ?? throw new ArgumentNullException(nameof(targetName));
            _serializer = serializer;

            Priority = priority;

            OwnerKey = ownerKey;
            IsPersistent = persistent && !string.IsNullOrEmpty(ownerKey);
            AttributeKey = BuildAttributeKey(ownerKey, _targetName, typeof(T));

            IsStored = false;

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

            if (IsPersistent)
                MPersistence.Register(this, priority, _serializer?.GetType().FullName);
        }

        /// <summary>
        /// Creates an attribute that stores its value inside itself.
        /// </summary>
        public MAttribute(
            object baseInstance,
            string targetName,
            T initialValue,
            string ownerKey = null,
            bool persistent = false,
            MPersistencePriority priority = MPersistencePriority.Normal,
            MSerializer<T> serializer = null
        )
        {
            _baseInstance = baseInstance ?? throw new ArgumentNullException(nameof(baseInstance));
            _targetName = targetName ?? throw new ArgumentNullException(nameof(targetName));
            _serializer = serializer;

            Priority = priority;

            OwnerKey = ownerKey;
            IsPersistent = persistent && !string.IsNullOrEmpty(ownerKey);
            AttributeKey = BuildAttributeKey(ownerKey, _targetName, typeof(T));

            _storedValue = initialValue;
            IsStored = true;

            _getter = _ => _storedValue;
            _setter = (_, value) => _storedValue = value;

            if (IsPersistent)
                MPersistence.Register(this, priority, _serializer?.GetType().FullName);
        }

        /// <summary>
        /// Creates an attribute that gets/sets the value using the given delegates.
        /// </summary>
        public MAttribute(
            object baseInstance,
            Func<object, T> getter,
            Action<object, T> setter,
            string targetName = null,
            string ownerKey = null,
            bool persistent = false,
            MPersistencePriority priority = MPersistencePriority.Normal,
            MSerializer<T> serializer = null
        )
        {
            _baseInstance = baseInstance ?? throw new ArgumentNullException(nameof(baseInstance));
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter ?? throw new ArgumentNullException(nameof(setter));
            _targetName = targetName ?? "<delegate>";
            _serializer = serializer;

            Priority = priority;

            OwnerKey = ownerKey;
            IsPersistent = persistent && !string.IsNullOrEmpty(ownerKey);
            AttributeKey = BuildAttributeKey(ownerKey, _targetName, typeof(T));

            IsStored = false;

            if (IsPersistent)
                MPersistence.Register(this, priority, _serializer?.GetType().FullName);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         API                            //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public T Get() => _getter(_baseInstance);

        public void Set(T value) => SetInternal(value, markDirty: true);

        internal void SetFromPersistence(T value) => SetInternal(value, markDirty: false);

        private void SetInternal(T value, bool markDirty)
        {
            _setter(_baseInstance, value);

            if (!IsPersistent || !markDirty)
                return;

            IsDirty = true;
            MPersistence.MarkDirty(this);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Persistence plumbing                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        string IPersistentAttribute.Serialize()
        {
            if (_serializer != null)
                return _serializer.Serialize(this);

            var value = Get();
            return MPersistence.SerializeValue(value, typeof(T));
        }

        void IPersistentAttribute.ApplySerialized(string serialized)
        {
            if (_serializer != null)
            {
                var value = _serializer.Deserialize(serialized);
                SetFromPersistence(value);
                return;
            }

            var obj = MPersistence.DeserializeValue(serialized, typeof(T));
            SetFromPersistence((T)obj);
        }

        void IPersistentAttribute.ClearDirty()
        {
            IsDirty = false;
        }

        private static string BuildAttributeKey(string ownerKey, string name, Type type)
        {
            if (string.IsNullOrEmpty(ownerKey))
                return null;

            return $"{ownerKey}:{name}:{type.FullName}";
        }
    }
}
