using System;
using System.Collections.Generic;
using System.Globalization;
using Retinues.Editor.Events;

namespace Retinues.Settings
{
    /// <summary>
    /// Logical configuration section (group of options).
    /// </summary>
    public sealed class Section
    {
        private readonly Func<string> _name;
        private readonly Func<string> _description;

        /// <summary>
        /// Create a new configuration section using a name provider.
        /// </summary>
        internal Section(Func<string> name, Func<string> description = null)
        {
            _name = name ?? (() => "General");
            _description = description;
        }

        /// <summary>
        /// Localized section name.
        /// </summary>
        public string Name => _name();

        /// <summary>
        /// Optional localized section description.
        /// </summary>
        public string Description => _description?.Invoke();

        internal int Ordinal { get; set; }
    }

    /// <summary>
    /// Represents metadata and runtime accessors for a configuration option.
    /// </summary>
    public interface IOption
    {
        string Key { get; set; }

        string Section { get; }
        string Name { get; }
        string Description { get; }

        Type Type { get; }

        bool RequiresRestart { get; }
        double MinValue { get; }
        double MaxValue { get; }

        object Default { get; }

        bool IsDisabled { get; }
        object DisabledOverrideBoxed { get; }

        IOption DependsOn { get; }
        object DependsOnValue { get; }

        UIEvent[] Fires { get; }

        /// <summary>
        /// Optional list of choices (typically used for enum dropdowns).
        /// </summary>
        IReadOnlyList<object> Choices { get; }

        /// <summary>
        /// Optional list of choice entries (value + localized label + optional hint).
        /// </summary>
        IReadOnlyList<(object value, string label, string hint)> ChoiceEntries { get; }

        /// <summary>
        /// Get the current option value as an object.
        /// </summary>
        object GetObject();

        /// <summary>
        /// Set the option value from an object, converting as necessary.
        /// </summary>
        void SetObject(object value);
    }

    /// <summary>
    /// Typed option wrapper exposing metadata and runtime value for T.
    /// </summary>
    public sealed class Option<T> : IOption
    {
        private readonly Func<string> _section;
        private readonly Func<string> _name;
        private readonly Func<string> _description;

        private readonly List<(T value, string label, string hint)> _choices;

        private T _value;

        /// <summary>
        /// Construct a typed option with metadata, default value and behavior.
        /// </summary>
        internal Option(
            Func<string> section,
            Func<string> name,
            Func<string> description,
            T @default,
            double minValue,
            double maxValue,
            bool requiresRestart,
            bool disabled,
            T disabledOverride,
            IReadOnlyList<(T value, string label, string hint)> choices,
            IOption dependsOn,
            object dependsOnValue,
            UIEvent[] fires,
            Action<IOption, object, object> onChanged
        )
        {
            _section = section ?? (() => "General");
            _name = name ?? (() => string.Empty);
            _description = description;

            RequiresRestart = requiresRestart;
            MinValue = minValue;
            MaxValue = maxValue;

            DefaultTyped = @default;
            _value = @default;

            IsDisabled = disabled;
            DisabledOverride = disabledOverride;

            DependsOn = dependsOn;
            DependsOnValue = dependsOnValue;

            Fires = fires;

            if (choices != null)
                _choices = [.. choices];
            else if (IsEnumType(typeof(T)))
            {
                var values = (T[])Enum.GetValues(UnwrapNullable(typeof(T)));
                _choices = new List<(T value, string label, string hint)>(values.Length);
                foreach (var v in values)
                    _choices.Add((v, v == null ? string.Empty : v.ToString(), string.Empty));
            }
            else
                _choices = null;

            OnChanged = onChanged;
        }

        // Wired by SettingsManager discovery
        public string Key { get; set; }

        public string Section => _section();
        public string Name => _name();
        public string Description => _description?.Invoke();

        public Type Type => typeof(T);

        public bool RequiresRestart { get; }
        public double MinValue { get; }
        public double MaxValue { get; }

        public T DefaultTyped { get; }
        public object Default => DefaultTyped;

        public bool IsDisabled { get; set; }
        public T DisabledOverride { get; set; }
        public object DisabledOverrideBoxed => DisabledOverride;

        public IOption DependsOn { get; set; }
        public object DependsOnValue { get; set; }

        public UIEvent[] Fires { get; set; }

        private Action<IOption, object, object> OnChanged { get; }

        /// <summary>
        /// The current typed option value.
        /// If disabled, getter returns the disabled override.
        /// </summary>
        public T Value
        {
            get
            {
                if (IsDisabled)
                    return DisabledOverride;

                return _value;
            }
            set
            {
                // Keep the stored value even when disabled, so it restores if later enabled.
                T next = ClampIfNumeric(value);

                if (EqualityComparer<T>.Default.Equals(_value, next))
                    return;

                T old = _value;
                _value = next;

                try
                {
                    OnChanged?.Invoke(this, old, next);
                }
                catch
                {
                    // Edge-safe: settings should never crash callers.
                }
            }
        }

        /// <summary>
        /// Implicit conversion to the underlying value type for convenience.
        /// </summary>
        public static implicit operator T(Option<T> o) => o.Value;

        /// <summary>
        /// Returns the available choices for the option as boxed objects.
        /// </summary>
        public IReadOnlyList<object> Choices
        {
            get
            {
                if (_choices == null || _choices.Count == 0)
                    return null;

                var list = new List<object>(_choices.Count);
                for (int i = 0; i < _choices.Count; i++)
                    list.Add(_choices[i].value);

                return list;
            }
        }

        /// <summary>
        /// Returns the available choices (value + label + hint) for the option.
        /// </summary>
        public IReadOnlyList<(object value, string label, string hint)> ChoiceEntries
        {
            get
            {
                if (_choices == null || _choices.Count == 0)
                    return null;

                var list = new List<(object value, string label, string hint)>(_choices.Count);
                for (int i = 0; i < _choices.Count; i++)
                {
                    var (value, label, hint) = _choices[i];
                    list.Add((value, label, hint));
                }

                return list;
            }
        }

        /// <summary>
        /// Get the current option value as an object.
        /// </summary>
        public object GetObject() => Value;

        /// <summary>
        /// Set the current option value from an object, coercing as needed.
        /// </summary>
        public void SetObject(object value)
        {
            if (value == null)
                return;

            try
            {
                Value = (T)CoerceToType(value, typeof(T));
            }
            catch
            {
                // Keep current value on bad coercion.
            }
        }

        /// <summary>
        /// Returns true when the provided type (after unwrapping nullable) is an enum.
        /// </summary>
        private static bool IsEnumType(Type t)
        {
            t = UnwrapNullable(t);
            return t.IsEnum;
        }

        /// <summary>
        /// Unwraps a nullable type to its underlying type, or returns the original.
        /// </summary>
        private static Type UnwrapNullable(Type t)
        {
            return Nullable.GetUnderlyingType(t) ?? t;
        }

        /// <summary>
        /// Coerce a boxed value to the specified target type using invariant culture.
        /// </summary>
        private static object CoerceToType(object value, Type targetType)
        {
            if (targetType == null)
                return value;

            var t = UnwrapNullable(targetType);

            if (value == null)
                return null;

            if (t.IsInstanceOfType(value))
                return value;

            if (t.IsEnum)
            {
                if (value is string s)
                    return Enum.Parse(t, s, ignoreCase: true);

                var underlying = Enum.GetUnderlyingType(t);
                var num = Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
                return Enum.ToObject(t, num);
            }

            return Convert.ChangeType(value, t, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Clamp numeric values to the configured min/max range when applicable.
        /// </summary>
        private T ClampIfNumeric(T value)
        {
            var t = UnwrapNullable(typeof(T));

            try
            {
                if (t == typeof(int))
                {
                    int v = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                    int min = (int)Math.Round(MinValue);
                    int max = (int)Math.Round(MaxValue);
                    if (max < min)
                        max = min;
                    if (v < min)
                        v = min;
                    if (v > max)
                        v = max;
                    return (T)(object)v;
                }

                if (t == typeof(float))
                {
                    float v = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                    float min = (float)MinValue;
                    float max = (float)MaxValue;
                    if (max < min)
                        max = min;
                    if (v < min)
                        v = min;
                    if (v > max)
                        v = max;
                    return (T)(object)v;
                }

                if (t == typeof(double))
                {
                    double v = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    double min = MinValue;
                    double max = MaxValue;
                    if (max < min)
                        max = min;
                    if (v < min)
                        v = min;
                    if (v > max)
                        v = max;
                    return (T)(object)v;
                }
            }
            catch
            {
                // Ignore clamp failures.
            }

            return value;
        }
    }
}
