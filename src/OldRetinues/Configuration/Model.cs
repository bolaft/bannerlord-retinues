using System;
using System.Collections.Generic;
using System.Globalization;
using Retinues.Utils;

namespace OldRetinues.Configuration
{
    /// <summary>
    /// Represents metadata and runtime accessors for a configuration option.
    /// </summary>
    public interface IOption
    {
        string Section { get; }
        string Name { get; }
        string Key { get; }
        string Hint { get; }
        Type Type { get; }
        bool RequiresRestart { get; }
        int MinValue { get; }
        int MaxValue { get; }
        object Default { get; }
        IReadOnlyDictionary<string, object> PresetOverrides { get; }
        bool IsDisabled { get; }
        object DisabledOverrideBoxed { get; }

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
    /// Typed option wrapper exposing metadata and runtime getter/setter for T.
    /// </summary>
    public sealed class Option<T>(
        Func<string> section,
        Func<string> name,
        string key,
        Func<string> hint,
        T @default,
        int minValue = 0,
        int maxValue = 1000,
        bool requiresRestart = false,
        IReadOnlyDictionary<string, object> presetOverrides = null,
        bool disabled = false,
        T disabledOverride = default
    ) : IOption
    {
        // Lazy-localized text factories (evaluated at MCM build time)
        private readonly Func<string> _section =
            section ?? (() => L.S("mcm_section_general", "General"));
        private readonly Func<string> _name = name ?? (() => string.Empty);
        private readonly Func<string> _hint = hint ?? (() => string.Empty);

        // Metadata (read-only)
        public string Section => _section();
        public string Name => _name();
        public string Key { get; } = key;
        public string Hint => _hint();
        public bool RequiresRestart { get; } = requiresRestart;
        public int MinValue { get; } = minValue;
        public int MaxValue { get; } = maxValue;
        public T DefaultTyped { get; } = @default;

        // Backing store supplied by ConfigSetup at runtime
        internal Func<T> Getter { get; set; } = () => default!;
        internal Action<T> Setter { get; set; } = _ => { };

        // IOption
        public Type Type => typeof(T);
        public object Default => DefaultTyped!;
        public IReadOnlyDictionary<string, object> PresetOverrides { get; } =
            presetOverrides ?? new Dictionary<string, object>();
        public bool IsDisabled { get; } = disabled;
        public T DisabledOverride { get; } = disabledOverride;
        public object DisabledOverrideBoxed => DisabledOverride!;

        /// <summary>
        /// The current typed option value (uses the runtime-backed getter/setter).
        /// </summary>
        public T Value
        {
            get => Getter();
            set => Setter(value);
        }

        /// <summary>
        /// Implicit conversion to the underlying value type for convenience.
        /// </summary>
        public static implicit operator T(Option<T> o) => o.Value;

        // IOption adapters
        /// <summary>
        /// Return the current value boxed as an object.
        /// </summary>
        public object GetObject() => Value!;

        /// <summary>
        /// Set the value from an object, converting to T using invariant culture.
        /// </summary>
        public void SetObject(object value) =>
            Setter((T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture));

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
                        value = DefaultTyped!;
                    break;

                case ConfigPreset.Realistic:
                    if (!PresetOverrides.TryGetValue(Presets.Realistic, out value))
                        value = DefaultTyped!;
                    break;

                case ConfigPreset.Default:
                default:
                    value = DefaultTyped!;
                    break;
            }

            SetObject(value);
        }
    }
}
