using System;
using System.Collections.Generic;
using System.Globalization;
using Retinues.GUI.Editor.Events;

namespace Retinues.Configuration
{
    /// <summary>
    /// High-level presets corresponding to the three built-in profiles.
    /// </summary>
    public enum ConfigPreset
    {
        Default,
        Freeform,
        Realistic,
    }

    /// <summary>
    /// Named configuration presets used for option overrides.
    /// </summary>
    public static class Presets
    {
        public const string Freeform = "freeform";
        public const string Realistic = "realistic";
    }

    /// <summary>
    /// Logical configuration section (group of options).
    /// </summary>
    public sealed class Section
    {
        private readonly Func<string> _name;

        internal Section(Func<string> name)
        {
            _name = name ?? (() => "General");
        }

        /// <summary>
        /// Localized section name.
        /// </summary>
        public string Name => _name();

        internal int Ordinal { get; set; }
    }

    /// <summary>
    /// Represents metadata and runtime accessors for a configuration option.
    /// </summary>
    public interface IOption
    {
        string Key { get; set; } // Assigned by with nameof(field)
        string Section { get; }
        string Name { get; }
        string Hint { get; }
        Type Type { get; }
        bool RequiresRestart { get; }
        double MinValue { get; }
        double MaxValue { get; }
        object Default { get; }
        IReadOnlyDictionary<string, object> PresetOverrides { get; }
        bool IsDisabled { get; }
        object DisabledOverrideBoxed { get; }
        IOption DependsOn { get; }
        object DependsOnValue { get; }
        UIEvent[] Fires { get; }

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
    /// Marker interface for multi-choice options (one-of-many).
    /// </summary>
    public interface IMultiChoiceOption : IOption
    {
        IReadOnlyList<object> Choices { get; }

        Func<object, string> ChoiceFormatter { get; }

        int SelectedIndex { get; set; }
    }

    /// <summary>
    /// Typed option wrapper exposing metadata and runtime getter/setter for T.
    /// </summary>
    public class Option<T> : IOption
    {
        private readonly Func<string> _section;
        private readonly Func<string> _name;
        private readonly Func<string> _hint;

        public Option(
            Func<string> section,
            Func<string> name,
            Func<string> hint,
            T @default,
            double minValue,
            double maxValue,
            bool requiresRestart,
            IReadOnlyDictionary<string, object> presetOverrides,
            bool disabled,
            T disabledOverride,
            IOption dependsOn = null,
            object dependsOnValue = null,
            UIEvent[] fires = null
        )
        {
            _section = section ?? (() => "General");
            _name = name ?? (() => string.Empty);
            _hint = hint ?? (() => string.Empty);

            RequiresRestart = requiresRestart;
            MinValue = minValue;
            MaxValue = maxValue;
            DefaultTyped = @default;

            PresetOverrides = presetOverrides ?? new Dictionary<string, object>();
            IsDisabled = disabled;
            DisabledOverride = disabledOverride;
            DependsOn = dependsOn;
            DependsOnValue = dependsOnValue;
            Fires = fires;

            // Fallbacks; real delegates are wired by SettingsManager.DiscoverOptions.
            Getter = () => DefaultTyped;
            Setter = value => { };
        }

        public string Key { get; set; } // Assigned by with nameof(field)

        // Metadata (read-only)
        public string Section => _section();
        public string Name => _name();
        public string Hint => _hint();

        public bool RequiresRestart { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public T DefaultTyped { get; set; }

        // Backing store supplied by SettingsManager at runtime
        internal Func<T> Getter { get; set; }
        internal Action<T> Setter { get; set; }

        // IOption
        public Type Type => typeof(T);
        public object Default => DefaultTyped;
        public object DisabledOverrideBoxed => DisabledOverride;
        public UIEvent[] Fires { get; set; }

        public IReadOnlyDictionary<string, object> PresetOverrides { get; set; }
        public bool IsDisabled { get; set; }
        public T DisabledOverride { get; set; }
        public IOption DependsOn { get; set; }
        public object DependsOnValue { get; set; }

        /// <summary>
        /// The current typed option value (uses the runtime-backed getter/setter).
        /// </summary>
        public T Value
        {
            get
            {
                if (Getter != null)
                    return Getter();
                return DefaultTyped;
            }
            set { Setter?.Invoke(value); }
        }

        /// <summary>
        /// Implicit conversion to the underlying value type for convenience.
        /// </summary>
        public static implicit operator T(Option<T> o) => o.Value;

        /// <summary>
        /// Return the current value boxed as an object.
        /// </summary>
        public object GetObject() => Value;

        /// <summary>
        /// Set the value from an object, converting to T using invariant culture.
        /// </summary>
        public void SetObject(object value)
        {
            var targetType = typeof(T);
            var t = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (value == null)
                return;

            try
            {
                if (t.IsInstanceOfType(value))
                {
                    Value = (T)value;
                    return;
                }

                if (t.IsEnum)
                {
                    if (value is string s)
                    {
                        var parsed = Enum.Parse(t, s, ignoreCase: true);
                        Value = (T)parsed;
                        return;
                    }

                    var underlying = Enum.GetUnderlyingType(t);
                    var num = Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
                    var boxedEnum = Enum.ToObject(t, num);
                    Value = (T)boxedEnum;
                    return;
                }

                var coerced = Convert.ChangeType(value, t, CultureInfo.InvariantCulture);
                Value = (T)coerced;
            }
            catch
            {
                // Keep current value on bad coercion (prevents dropdowns from going blank).
            }
        }

        /// <summary>
        /// Apply one of the built-in presets to this option only.
        /// </summary>
        public void ApplyPreset(ConfigPreset preset)
        {
            object value;

            switch (preset)
            {
                case ConfigPreset.Freeform:
                    if (!PresetOverrides.TryGetValue(Presets.Freeform, out value))
                        value = DefaultTyped;
                    break;

                case ConfigPreset.Realistic:
                    if (!PresetOverrides.TryGetValue(Presets.Realistic, out value))
                        value = DefaultTyped;
                    break;

                case ConfigPreset.Default:
                default:
                    value = DefaultTyped;
                    break;
            }

            SetObject(value);
        }
    }

    /// <summary>
    /// Multi-choice option: pick a single value from a fixed list of choices.
    /// </summary>
    public class MultiChoiceOption<T> : Option<T>, IMultiChoiceOption
    {
        private readonly List<T> _choices;
        private readonly Func<object, string> _choiceFormatter;

        public MultiChoiceOption(
            Func<string> section,
            Func<string> name,
            Func<string> hint,
            T @default,
            IList<T> choices,
            double minValue,
            double maxValue,
            bool requiresRestart,
            IReadOnlyDictionary<string, object> presetOverrides,
            bool disabled,
            T disabledOverride,
            IOption dependsOn,
            object dependsOnValue,
            Func<T, string> choiceFormatter,
            UIEvent[] fires
        )
            : base(
                section,
                name,
                hint,
                @default,
                minValue,
                maxValue,
                requiresRestart,
                presetOverrides,
                disabled,
                disabledOverride,
                dependsOn,
                dependsOnValue,
                fires
            )
        {
            if (choices != null)
                _choices = [.. choices];
            else
                _choices = [];

            if (choiceFormatter != null)
            {
                _choiceFormatter = delegate(object o)
                {
                    return choiceFormatter((T)o);
                };
            }
            else
            {
                _choiceFormatter = delegate(object o)
                {
                    if (o == null)
                        return string.Empty;
                    return o.ToString();
                };
            }
        }

        public IReadOnlyList<object> Choices
        {
            get
            {
                var list = new List<object>(_choices.Count);
                for (int i = 0; i < _choices.Count; i++)
                    list.Add(_choices[i]);
                return list;
            }
        }

        public Func<object, string> ChoiceFormatter => _choiceFormatter;

        public int SelectedIndex
        {
            get
            {
                T current = this.Value;
                for (int i = 0; i < _choices.Count; i++)
                {
                    if (EqualityComparer<T>.Default.Equals(_choices[i], current))
                        return i;
                }

                return -1;
            }
            set
            {
                if (_choices.Count == 0)
                    return;

                int index = value;
                if (index < 0)
                    index = 0;
                if (index >= _choices.Count)
                    index = _choices.Count - 1;

                this.Value = _choices[index];
            }
        }
    }
}
