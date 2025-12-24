using System;
using System.Collections.Generic;
using Retinues.Utilities;

namespace Retinues.Model
{
    /// <summary>
    /// Named priority levels for attribute deserialization.
    /// Higher values are applied earlier.
    /// </summary>
    public enum AttributePriority
    {
        Lowest = 0,
        Low = 25,
        Medium = 50,
        High = 75,
        Highest = 100,
    }

    [SafeClass(IncludeDerived = true)]
    public partial class MAttribute<T> : IMAttribute
    {
        /// <summary>
        /// Priority used for ordering deserialization. Higher values are applied first.
        /// Defaults to Medium.
        /// </summary>
        public AttributePriority Priority { get; internal set; }

        readonly List<IMAttribute> _dependents = [];
        readonly Func<T> _getter;
        readonly Action<T> _setter;
        readonly IModel _model;
        bool _dirty;
        readonly bool _persistent;

        public void Touch()
        {
            if (_persistent)
            {
                _dirty = true;

                // Propagate dirtiness to dependents
                foreach (var dep in _dependents)
                {
                    try
                    {
                        dep.MarkDirty();
                    }
                    catch { }
                }
            }
        }

        public string Name { get; internal set; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Get / Set                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the value of the attribute.
        /// </summary>
        public T Get() => _getter();

        /// <summary>
        /// Sets the value of the attribute.
        /// </summary>
        void SetValue(T value, bool markDirty)
        {
            if (markDirty && _persistent)
            {
                _dirty = true;

                // Propagate dirtiness to dependents
                foreach (var dep in _dependents)
                {
                    try
                    {
                        dep.MarkDirty();
                    }
                    catch { }
                }
            }

            _setter(value);
        }

        public void Set(T value) => SetValue(value, true);

        /// <summary>
        /// True if the attribute's setter has been called since creation/loading.
        /// </summary>
        public bool IsDirty => _dirty;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Constructors                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Creates an attribute that uses reflection to get/set the value on the given base instance.
        /// The constructed getter/setter capture the provided base instance and do not retain it as accessible state.
        /// </summary>
        public MAttribute(
            IModel model,
            string name,
            string target,
            bool persistent = true,
            AttributePriority priority = AttributePriority.Medium
        )
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            bool isProperty = Reflection.HasProperty(_model.Base, target);
            bool isField = Reflection.HasField(_model.Base, target);

            if (isField)
            {
                _getter = () => Reflection.GetFieldValue<T>(_model.Base, target);
                _setter = value => Reflection.SetFieldValue(_model.Base, target, value);
            }
            else if (isProperty)
            {
                _getter = () => Reflection.GetPropertyValue<T>(_model.Base, target);
                _setter = value => Reflection.SetPropertyValue(_model.Base, target, value);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Target '{target}' is neither a field nor a property on type '{_model.Base.GetType().Name}'."
                );
            }

            Name = name ?? throw new ArgumentNullException(nameof(name));
            _persistent = persistent;
            Priority = priority;
        }

        /// <summary>
        /// Creates an attribute from delegates that accept an explicit base instance; the delegates are wrapped
        /// into parameterless delegates by capturing the provided baseInstance (which may be null).
        /// </summary>
        public MAttribute(
            IModel model,
            string name,
            Func<object, T> getter,
            Action<object, T> setter,
            bool persistent = true,
            AttributePriority priority = AttributePriority.Medium
        )
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            if (getter == null)
                throw new ArgumentNullException(nameof(getter));
            if (setter == null)
                throw new ArgumentNullException(nameof(setter));

            _getter = () => getter(_model.Base);
            _setter = value => setter(_model.Base, value);
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _persistent = persistent;
            Priority = priority;
        }

        /// <summary>
        /// Creates an attribute from parameterless delegates.
        /// </summary>
        public MAttribute(
            IModel model,
            string name,
            Func<T> getter,
            Action<T> setter,
            bool persistent = true,
            AttributePriority priority = AttributePriority.Medium
        )
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter ?? throw new ArgumentNullException(nameof(setter));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _persistent = persistent;
            Priority = priority;
        }

        // IMAttribute implementation
        string IMAttribute.Name => Name;

        void IMAttribute.AddDependent(IMAttribute dependent)
        {
            if (dependent == null || ReferenceEquals(dependent, this))
                return;
            _dependents.Add(dependent);
        }

        void IMAttribute.MarkDirty()
        {
            if (_persistent)
                _dirty = true;
        }
    }
}
