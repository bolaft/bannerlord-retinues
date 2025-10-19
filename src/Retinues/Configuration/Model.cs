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
    public sealed class Option<T>(
        string section,
        string name,
        string key,
        string hint,
        T @default,
        int minValue = 0,
        int maxValue = 1000,
        bool requiresRestart = false,
        IReadOnlyDictionary<string, object> presetOverrides = null
    ) : IOption
    {
        // Metadata (read-only)
        public string Section { get; } = section ?? L.S("mcm_section_general", "General");
        public string Name { get; } = name;
        public string Key { get; } = key;
        public string Hint { get; } = hint;
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
