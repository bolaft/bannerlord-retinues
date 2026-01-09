using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Retinues.Framework.Runtime;
using Retinues.Utilities;

namespace Retinues.Configuration
{
    /// <summary>
    /// Central manager for Retinues settings: option registry, values, presets, and logging.
    /// </summary>
    [SafeClass]
    public static partial class SettingsManager
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
            double minValue = 0,
            double maxValue = 1000,
            bool requiresRestart = false,
            IReadOnlyDictionary<string, object> presets = null,
            object freeform = null,
            object realistic = null,
            bool disabled = false,
            T disabledOverride = default,
            IOption dependsOn = null,
            object dependsOnValue = null,
            Retinues.Editor.Events.UIEvent[] fires = null
        )
        {
            Func<string> sectionFunc = null;
            if (section != null)
                sectionFunc = () => section.Name;

            // Merge: existing presets + short-hand params
            Dictionary<string, object> merged = null;

            if (presets != null)
                merged = presets.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            if (freeform != null)
            {
                merged ??= [];
                merged[Presets.Freeform] = CoerceToType(freeform, typeof(T));
            }

            if (realistic != null)
            {
                merged ??= [];
                merged[Presets.Realistic] = CoerceToType(realistic, typeof(T));
            }

            return new Option<T>(
                sectionFunc,
                name,
                hint,
                @default,
                minValue,
                maxValue,
                requiresRestart,
                merged,
                disabled,
                disabledOverride,
                dependsOn,
                dependsOnValue,
                fires
            );
        }

        /// <summary>
        /// Helper factory to create a MultiChoiceOption.
        /// </summary>
        public static MultiChoiceOption<T> CreateMultiChoiceOption<T>(
            Section section,
            Func<string> name,
            Func<string> hint,
            T @default,
            IList<T> choices,
            double minValue = 0,
            double maxValue = 1000,
            bool requiresRestart = false,
            IReadOnlyDictionary<string, object> presets = null,
            object freeform = null,
            object realistic = null,
            bool disabled = false,
            T disabledOverride = default,
            Func<T, string> choiceFormatter = null,
            IOption dependsOn = null,
            object dependsOnValue = null,
            Retinues.Editor.Events.UIEvent[] fires = null
        )
        {
            Func<string> sectionFunc = null;
            if (section != null)
                sectionFunc = () => section.Name;

            Dictionary<string, object> merged = null;

            if (presets != null)
                merged = presets.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            if (freeform != null)
            {
                merged ??= new Dictionary<string, object>();
                merged[Presets.Freeform] = CoerceToType(freeform, typeof(T));
            }

            if (realistic != null)
            {
                merged ??= new Dictionary<string, object>();
                merged[Presets.Realistic] = CoerceToType(realistic, typeof(T));
            }

            return new MultiChoiceOption<T>(
                sectionFunc,
                name,
                hint,
                @default,
                choices,
                minValue,
                maxValue,
                requiresRestart,
                merged,
                disabled,
                disabledOverride,
                dependsOn,
                dependsOnValue,
                choiceFormatter,
                fires
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

                Log.Debug("Retinues Config:");

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

                    Log.Debug($"[{sectionName}]");

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
                            Log.Debug($"{marker} {label} = {currentText} (DISABLED; override)");
                        }
                        else
                        {
                            Log.Debug($"{marker} {label} = {currentText} (default: {defaultText})");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "LogSettings failed.");
            }
        }

        private static string FormatConfigValue(object value)
        {
            if (value == null)
                return "null";
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Get / Set                       //
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
                if (_values.TryGetValue(key, out object old) && Equals(old, v))
                {
                    return;
                }

                _values[key] = v;

                try
                {
                    OptionChanged?.Invoke(key, v);
                }
                catch (Exception e)
                {
                    Log.Exception(e, "OptionChanged handler failed.");
                }

                try
                {
                    QueueUIEventsIfNeeded(key);
                }
                catch (Exception e)
                {
                    Log.Exception(e, "Option fires handler failed.");
                }
            };
        }
    }
}
