using System;
using Retinues.Utilities;

namespace Retinues.Model
{
    public class MAttribute<T>
    {
        readonly string _targetName;
        readonly object _baseInstance;

        readonly Func<object, T> _getter;
        readonly Action<object, T> _setter;

        /// <summary>
        /// Creates an attribute that uses reflection to get/set the value.
        /// </summary>
        public MAttribute(object baseInstance, string targetName)
        {
            _baseInstance = baseInstance ?? throw new ArgumentNullException(nameof(baseInstance));
            _targetName = targetName ?? throw new ArgumentNullException(nameof(targetName));

            bool IsProperty = Reflection.HasProperty(_baseInstance, _targetName);
            bool IsField = Reflection.HasField(_baseInstance, _targetName);

            if (IsField)
            {
                _getter = obj => Reflection.GetFieldValue<T>(obj, _targetName);
                _setter = (obj, value) => Reflection.SetFieldValue(obj, _targetName, value);
                return;
            }

            if (IsProperty)
            {
                _getter = obj => Reflection.GetPropertyValue<T>(obj, _targetName);
                _setter = (obj, value) => Reflection.SetPropertyValue(obj, _targetName, value);
                return;
            }

            throw new InvalidOperationException(
                $"Target '{_targetName}' is neither a field nor a property on type '{_baseInstance.GetType().Name}'."
            );
        }

        /// <summary>
        /// Creates an attribute that gets/sets the value using the given delegates.
        /// </summary>
        public MAttribute(
            object baseInstance,
            Func<object, T> getter,
            Action<object, T> setter,
            string targetName = null
        )
        {
            _baseInstance = baseInstance ?? throw new ArgumentNullException(nameof(baseInstance));
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter ?? throw new ArgumentNullException(nameof(setter));
            _targetName = targetName ?? "<delegate>";
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
            _setter(_baseInstance, value);
        }
    }
}
