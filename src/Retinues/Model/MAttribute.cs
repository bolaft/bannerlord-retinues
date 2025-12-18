using System;
using Retinues.Utilities;

namespace Retinues.Model
{
    internal interface IPersistentAttribute
    {
        string OwnerKey { get; }
        string AttributeKey { get; }
        bool IsDirty { get; }

        string PersistenceName { get; }
        string TargetName { get; }
        string ValueTypeName { get; }

        MPersistencePriority Priority { get; }
        string SerializerTypeName { get; }

        string Serialize();
        void ApplySerialized(string serialized);
        void ClearDirty();
    }

    [SafeClass(IncludeDerived = true)]
    public class MAttribute<T> : IPersistentAttribute
    {
        private readonly object _baseInstance;

        private readonly Func<object, T> _getter;
        private readonly Action<object, T> _setter;

        private readonly MSerializer<T> _serializer;

        private T _storedValue;

        public string PersistenceName { get; }
        public string TargetName { get; }
        public string ValueTypeName => typeof(T).FullName;

        public MPersistencePriority Priority { get; }
        public string SerializerTypeName => _serializer?.GetType().FullName;

        public string OwnerKey { get; }
        public string AttributeKey { get; }

        public bool IsPersistent { get; }
        public bool IsDirty { get; private set; }

        public bool IsStored { get; private set; }

        bool IPersistentAttribute.IsDirty => IsDirty;

        // Base-member attribute (reflects against targetName on baseInstance)
        public MAttribute(
            object baseInstance,
            string persistenceName,
            string targetName,
            string ownerKey = null,
            bool persistent = false,
            MPersistencePriority priority = MPersistencePriority.Normal,
            MSerializer<T> serializer = null
        )
        {
            _baseInstance = baseInstance ?? throw new ArgumentNullException(nameof(baseInstance));
            PersistenceName =
                persistenceName ?? throw new ArgumentNullException(nameof(persistenceName));
            TargetName = targetName ?? throw new ArgumentNullException(nameof(targetName));
            _serializer = serializer;

            Priority = priority;

            OwnerKey = ownerKey;
            IsPersistent = persistent && !string.IsNullOrEmpty(ownerKey);
            AttributeKey = BuildAttributeKey(ownerKey, PersistenceName);

            IsStored = false;

            bool isField = Reflection.HasField(_baseInstance, TargetName);
            bool isProperty = Reflection.HasProperty(_baseInstance, TargetName);

            if (isField)
            {
                _getter = obj => Reflection.GetFieldValue<T>(obj, TargetName);
                _setter = (obj, value) => Reflection.SetFieldValue(obj, TargetName, value);
            }
            else if (isProperty)
            {
                _getter = obj => Reflection.GetPropertyValue<T>(obj, TargetName);
                _setter = (obj, value) => Reflection.SetPropertyValue(obj, TargetName, value);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Target '{TargetName}' is neither a field nor a property on '{_baseInstance.GetType().FullName}'."
                );
            }

            if (IsPersistent)
                MPersistence.Register(this);
        }

        // Stored attribute (internal value only)
        public MAttribute(
            object baseInstance,
            string persistenceName,
            string targetName,
            T initialValue,
            string ownerKey = null,
            bool persistent = false,
            MPersistencePriority priority = MPersistencePriority.Normal,
            MSerializer<T> serializer = null
        )
        {
            _baseInstance = baseInstance ?? throw new ArgumentNullException(nameof(baseInstance));
            PersistenceName =
                persistenceName ?? throw new ArgumentNullException(nameof(persistenceName));
            TargetName = targetName ?? throw new ArgumentNullException(nameof(targetName));
            _serializer = serializer;

            Priority = priority;

            OwnerKey = ownerKey;
            IsPersistent = persistent && !string.IsNullOrEmpty(ownerKey);
            AttributeKey = BuildAttributeKey(ownerKey, PersistenceName);

            _storedValue = initialValue;
            IsStored = true;

            _getter = _ => _storedValue;
            _setter = (_, value) => _storedValue = value;

            if (IsPersistent)
                MPersistence.Register(this);
        }

        // Delegate attribute (getter/setter provided by wrapper)
        public MAttribute(
            object baseInstance,
            Func<object, T> getter,
            Action<object, T> setter,
            string persistenceName,
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

            PersistenceName =
                persistenceName ?? throw new ArgumentNullException(nameof(persistenceName));
            TargetName = targetName ?? "<delegate>";
            _serializer = serializer;

            Priority = priority;

            OwnerKey = ownerKey;
            IsPersistent = persistent && !string.IsNullOrEmpty(ownerKey);
            AttributeKey = BuildAttributeKey(ownerKey, PersistenceName);

            IsStored = false;

            if (IsPersistent)
                MPersistence.Register(this);
        }

        public T Get()
        {
            return _getter(_baseInstance);
        }

        public void Set(T value)
        {
            SetInternal(value, markDirty: true);
        }

        internal void SetFromPersistence(T value)
        {
            SetInternal(value, markDirty: false);
        }

        public void Touch()
        {
            IsDirty = true;

            if (IsPersistent)
                MPersistence.MarkDirty(this);

            Log.Info($"Attribute '{AttributeKey}' touched; marking dirty.");
        }

        private void SetInternal(T value, bool markDirty)
        {
            _setter(_baseInstance, value);

            if (!IsPersistent || !markDirty)
                return;

            IsDirty = true;
            MPersistence.MarkDirty(this);
        }

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
                var v = _serializer.Deserialize(serialized);
                SetFromPersistence(v);
                return;
            }

            var obj = MPersistence.DeserializeValue(serialized, typeof(T));
            SetFromPersistence((T)obj);
        }

        void IPersistentAttribute.ClearDirty()
        {
            IsDirty = false;
        }

        private static string BuildAttributeKey(string ownerKey, string persistenceName)
        {
            if (string.IsNullOrEmpty(ownerKey))
                return null;

            return $"{ownerKey}:{persistenceName}";
        }
    }
}
