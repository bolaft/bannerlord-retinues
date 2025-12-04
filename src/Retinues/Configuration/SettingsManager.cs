using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Retinues.Utilities;

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
    /// Marker interface for multi-choice options (one-of-many).
    /// </summary>
    public interface IMultiChoiceOption : IOption
    {
        /// <summary>
        /// Available choices for this option (boxed).
        /// </summary>
        IReadOnlyList<object> Choices { get; }

        /// <summary>
        /// Formatter to render a choice as a label.
        /// </summary>
        Func<object, string> ChoiceFormatter { get; }

        /// <summary>
        /// Index of the currently selected choice.
        /// </summary>
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
            int minValue,
            int maxValue,
            bool requiresRestart,
            IReadOnlyDictionary<string, object> presetOverrides,
            bool disabled,
            T disabledOverride
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
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public T DefaultTyped { get; set; }

        // Backing store supplied by SettingsManager at runtime
        internal Func<T> Getter { get; set; }
        internal Action<T> Setter { get; set; }

        // IOption
        public Type Type => typeof(T);
        public object Default => DefaultTyped;
        public object DisabledOverrideBoxed => DisabledOverride;

        public IReadOnlyDictionary<string, object> PresetOverrides { get; set; }
        public bool IsDisabled { get; set; }
        public T DisabledOverride { get; set; }

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

        // IOption adapters
        /// <summary>
        /// Return the current value boxed as an object.
        /// </summary>
        public object GetObject() => Value;

        /// <summary>
        /// Set the value from an object, converting to T using invariant culture.
        /// </summary>
        public void SetObject(object value)
        {
            T typed = (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            Value = typed;
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
            int minValue,
            int maxValue,
            bool requiresRestart,
            IReadOnlyDictionary<string, object> presetOverrides,
            bool disabled,
            T disabledOverride,
            Func<T, string> choiceFormatter
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
                disabledOverride
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

    /// <summary>
    /// Central manager for Retinues settings: option registry, values, presets, and logging.
    /// </summary>
    public static class SettingsManager
    {
        private static readonly List<IOption> _all = [];
        private static readonly Dictionary<string, IOption> _byKey = new(
            StringComparer.OrdinalIgnoreCase
        );
        private static readonly Dictionary<string, object> _values = new(
            StringComparer.OrdinalIgnoreCase
        );
        private static readonly Dictionary<string, int> _ordinalByKey = new(
            StringComparer.OrdinalIgnoreCase
        );
        private static readonly List<Section> _sections = [];
        private static readonly Dictionary<string, int> _sectionOrdinalByName = new(
            StringComparer.OrdinalIgnoreCase
        );

        private static bool _discovered;

        /// <summary>
        /// Global config change event.
        /// </summary>
        public static event Action<string, object> OptionChanged;

        /// <summary>
        /// All discovered options (ordered by declaration order).
        /// </summary>
        public static IReadOnlyList<IOption> AllOptions
        {
            get
            {
                DiscoverOptions();
                return _all;
            }
        }

        /// <summary>
        /// Get an option by key (case-insensitive). Throws if missing or wrong type.
        /// </summary>
        public static Option<T> GetOption<T>(string key)
        {
            if (!TryGetOption(key, out IOption opt))
                throw new KeyNotFoundException("Unknown option key '" + key + "'.");

            if (opt is not Option<T> typed)
                throw new InvalidCastException(
                    "Option '" + key + "' is of type " + opt.Type + ", not " + typeof(T) + "."
                );

            return typed;
        }

        /// <summary>
        /// Try to get an option by key (case-insensitive).
        /// </summary>
        public static bool TryGetOption(string key, out IOption option)
        {
            DiscoverOptions();
            return _byKey.TryGetValue(key, out option);
        }

        /// <summary>
        /// Helper factory to create a Section. Intended to be used from Settings.
        /// </summary>
        public static Section CreateSection(Func<string> name) => new(name);

        /// <summary>
        /// Helper factory to create an Option. Intended to be used from Settings.
        /// </summary>
        public static Option<T> CreateOption<T>(
            Section section,
            Func<string> name,
            Func<string> hint,
            T @default,
            int minValue = 0,
            int maxValue = 1000,
            bool requiresRestart = false,
            IReadOnlyDictionary<string, object> presets = null,
            bool disabled = false,
            T disabledOverride = default
        )
        {
            Func<string> sectionFunc = null;
            if (section != null)
                sectionFunc = () => section.Name;

            return new Option<T>(
                sectionFunc,
                name,
                hint,
                @default,
                minValue,
                maxValue,
                requiresRestart,
                presets,
                disabled,
                disabledOverride
            );
        }

        /// <summary>
        /// Helper factory to create a MultiChoiceOption&lt;T&gt;.
        /// </summary>
        public static MultiChoiceOption<T> CreateMultiChoiceOption<T>(
            Func<string> section,
            Func<string> name,
            Func<string> hint,
            T @default,
            IList<T> choices,
            int minValue,
            int maxValue,
            bool requiresRestart,
            IReadOnlyDictionary<string, object> presets,
            bool disabled,
            T disabledOverride,
            Func<T, string> choiceFormatter
        )
        {
            return new MultiChoiceOption<T>(
                section,
                name,
                hint,
                @default,
                choices,
                minValue,
                maxValue,
                requiresRestart,
                presets,
                disabled,
                disabledOverride,
                choiceFormatter
            );
        }

        /// <summary>
        /// Apply one of the three built-in presets to all options.
        /// </summary>
        public static void ApplyPresetsToAll(ConfigPreset preset)
        {
            DiscoverOptions();

            string presetKey = null;
            if (preset == ConfigPreset.Freeform)
                presetKey = Presets.Freeform;
            else if (preset == ConfigPreset.Realistic)
                presetKey = Presets.Realistic;

            for (int i = 0; i < _all.Count; i++)
            {
                IOption opt = _all[i];
                object value;

                if (presetKey == null)
                {
                    value = opt.Default;
                }
                else if (!opt.PresetOverrides.TryGetValue(presetKey, out value))
                {
                    value = opt.Default;
                }

                opt.SetObject(value);
            }
        }

        /// <summary>
        /// Log the current configuration values grouped by section.
        /// </summary>
        public static void LogSettings()
        {
            try
            {
                DiscoverOptions();

                Log.Info("Retinues Config:");

                var grouped = new Dictionary<string, List<IOption>>();
                for (int i = 0; i < _all.Count; i++)
                {
                    IOption opt = _all[i];
                    if (!grouped.TryGetValue(opt.Section, out List<IOption> list))
                    {
                        list = [];
                        grouped[opt.Section] = list;
                    }

                    list.Add(opt);
                }

                // Preserve section declaration order using Section ordinals.
                foreach (var kv in grouped.OrderBy(g => GetSectionOrdinal(g.Key)))
                {
                    string sectionName = kv.Key;
                    List<IOption> options = kv.Value;

                    Log.Info($"[{sectionName}]");

                    options.Sort(
                        (a, b) =>
                        {
                            int ia = GetOrdinal(a.Key);
                            int ib = GetOrdinal(b.Key);
                            return ia.CompareTo(ib);
                        }
                    );

                    foreach (var opt in options)
                    {
                        object current = opt.GetObject();
                        object def = opt.Default;

                        string currentText = FormatConfigValue(current);
                        string defaultText = FormatConfigValue(def);

                        bool changed = !Equals(current, def);
                        string marker = changed ? "*" : " ";

                        string label;
                        if (string.IsNullOrWhiteSpace(opt.Name))
                            label = opt.Key;
                        else
                            label = opt.Name + " [" + opt.Key + "]";

                        if (opt.IsDisabled)
                        {
                            Log.Info($"{marker} {label} = {currentText} (DISABLED; override)");
                        }
                        else
                        {
                            Log.Info($"{marker} {label} = {currentText} (default: {defaultText})");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "LogSettings failed.");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Internal wiring                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal static void DiscoverOptions()
        {
            if (_discovered)
                return;

            try
            {
                Type settingsType = typeof(Settings); // <== renamed class
                FieldInfo[] fields = settingsType.GetFields(
                    BindingFlags.Public | BindingFlags.Static
                );

                int ordinal = 0;

                foreach (FieldInfo f in fields.OrderBy(fi => fi.MetadataToken))
                {
                    // Sections first: used to define ordering.
                    if (typeof(Section).IsAssignableFrom(f.FieldType))
                    {
                        if (f.GetValue(null) is not Section section)
                            continue;

                        section.Ordinal = _sections.Count;
                        _sections.Add(section);

                        string sectionName = section.Name;
                        if (
                            !string.IsNullOrWhiteSpace(sectionName)
                            && !_sectionOrdinalByName.ContainsKey(sectionName)
                        )
                        {
                            _sectionOrdinalByName[sectionName] = section.Ordinal;
                        }

                        continue;
                    }

                    // Options
                    if (!typeof(IOption).IsAssignableFrom(f.FieldType))
                        continue;

                    object raw = f.GetValue(null);
                    if (raw == null)
                        continue;

                    IOption opt = (IOption)raw;

                    // Autogenerated key from field name.
                    string key = f.Name;
                    opt.Key = key;

                    _all.Add(opt);
                    _byKey[key] = opt;
                    _ordinalByKey[key] = ordinal++;
                    _values[key] = opt.Default;

                    Type optType = opt.GetType();
                    PropertyInfo getterProp = optType.GetProperty(
                        "Getter",
                        BindingFlags.Instance | BindingFlags.NonPublic
                    );
                    PropertyInfo setterProp = optType.GetProperty(
                        "Setter",
                        BindingFlags.Instance | BindingFlags.NonPublic
                    );
                    if (getterProp == null || setterProp == null)
                        continue;

                    Type valueType = opt.Type;
                    MethodInfo makeGetterMethod = typeof(SettingsManager)
                        .GetMethod("MakeGetter", BindingFlags.NonPublic | BindingFlags.Static)
                        .MakeGenericMethod(valueType);
                    MethodInfo makeSetterMethod = typeof(SettingsManager)
                        .GetMethod("MakeSetter", BindingFlags.NonPublic | BindingFlags.Static)
                        .MakeGenericMethod(valueType);

                    object getterDelegate = makeGetterMethod.Invoke(null, new object[] { key });
                    object setterDelegate = makeSetterMethod.Invoke(null, new object[] { key });

                    getterProp.SetValue(opt, getterDelegate, null);
                    setterProp.SetValue(opt, setterDelegate, null);
                }

                _discovered = true;
            }
            catch (Exception e)
            {
                Log.Exception(e, "DiscoverOptions failed.");
            }
        }

        internal static int GetSectionOrdinal(string sectionName)
        {
            if (_sectionOrdinalByName.TryGetValue(sectionName, out int ordinal))
                return ordinal;
            return int.MaxValue;
        }

        internal static int GetOrdinal(string key)
        {
            if (_ordinalByKey.TryGetValue(key, out int idx))
                return idx;
            return int.MaxValue;
        }

        private static string FormatConfigValue(object value)
        {
            if (value == null)
                return "null";
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Get / Set                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Used via reflection, do not remove.
        private static Func<T> MakeGetter<T>(string key)
        {
            return delegate
            {
                if (!_values.TryGetValue(key, out object raw))
                    return default;

                T value = (T)Convert.ChangeType(raw, typeof(T), CultureInfo.InvariantCulture);

                if (_byKey.TryGetValue(key, out IOption opt))
                {
                    if (opt is Option<T> optT && optT.IsDisabled)
                        return optT.DisabledOverride;
                }

                return value;
            };
        }

        // Used via reflection, do not remove.
        private static Action<T> MakeSetter<T>(string key)
        {
            return delegate(T v)
            {
                _values[key] = v;

                try
                {
                    OptionChanged?.Invoke(key, v);
                }
                catch (Exception e)
                {
                    Log.Exception(e, "OptionChanged handler failed.");
                }
            };
        }
    }
}
