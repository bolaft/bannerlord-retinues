using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Retinues.Editor.Events;
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
            UIEvent[] fires = null
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
            UIEvent[] fires = null
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
                merged ??= [];
                merged[Presets.Freeform] = CoerceToType(freeform, typeof(T));
            }

            if (realistic != null)
            {
                merged ??= [];
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Get / Set                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Used via reflection, do not remove.
        private static Func<T> MakeGetter<T>(string key)
        {
            return delegate
            {
                if (!_values.TryGetValue(key, out object raw) || raw == null)
                    return default;

                var targetType = typeof(T);
                var t = Nullable.GetUnderlyingType(targetType) ?? targetType;

                object boxed = raw;

                try
                {
                    if (!t.IsInstanceOfType(boxed))
                    {
                        if (t.IsEnum)
                        {
                            if (boxed is string s)
                            {
                                boxed = Enum.Parse(t, s, ignoreCase: true);
                            }
                            else
                            {
                                var underlying = Enum.GetUnderlyingType(t);
                                var num = Convert.ChangeType(
                                    boxed,
                                    underlying,
                                    CultureInfo.InvariantCulture
                                );
                                boxed = Enum.ToObject(t, num);
                            }
                        }
                        else
                        {
                            boxed = Convert.ChangeType(boxed, t, CultureInfo.InvariantCulture);
                        }
                    }
                }
                catch
                {
                    return default;
                }

                T value = (T)boxed;

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
