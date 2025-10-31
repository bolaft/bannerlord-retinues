using System;
using System.Collections.Generic;
using System.Globalization;
using Retinues.Utils;

namespace Retinues.Configuration
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
    public sealed class Option<T> : IOption
    {
        // Lazy-localized text factories (evaluated at MCM build time)
        private readonly Func<string> _section;
        private readonly Func<string> _name;
        private readonly Func<string> _hint;

        public Option(
            Func<string> section,
            Func<string> name,
            string key,
            Func<string> hint,
            T @default,
            int minValue = 0,
            int maxValue = 1000,
            bool requiresRestart = false,
            IReadOnlyDictionary<string, object> presetOverrides = null
        )
        {
            _section = section ?? (() => L.S("mcm_section_general", "General"));
            _name = name ?? (() => string.Empty);
            _hint = hint ?? (() => string.Empty);
            Key = key;
            RequiresRestart = requiresRestart;
            MinValue = minValue;
            MaxValue = maxValue;
            DefaultTyped = @default;
            PresetOverrides = presetOverrides ?? new Dictionary<string, object>();
        }

        // Metadata (read-only)
        public string Section => _section();
        public string Name => _name();
        public string Key { get; }
        public string Hint => _hint();
        public bool RequiresRestart { get; }
        public int MinValue { get; }
        public int MaxValue { get; }
        public T DefaultTyped { get; }

        // Backing store supplied by ConfigSetup at runtime
        internal Func<T> Getter { get; set; } = () => default!;
        internal Action<T> Setter { get; set; } = _ => { };

        // IOption
        public Type Type => typeof(T);
        public object Default => DefaultTyped!;
        public IReadOnlyDictionary<string, object> PresetOverrides { get; }

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
    }
}
