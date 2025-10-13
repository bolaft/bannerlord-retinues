using System;
using System.Collections.Generic;
using System.Globalization;
using Retinues.Utils;

namespace Retinues.Configuration
{
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
        object GetObject();
        void SetObject(object value);
    }

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

        public T Value
        {
            get => Getter();
            set => Setter(value);
        }

        public static implicit operator T(Option<T> o) => o.Value; // so: if (Config.PayForEquipment) { ... }

        // IOption adapters
        public object GetObject() => Value!;

        public void SetObject(object value) =>
            Setter((T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture));
    }
}
