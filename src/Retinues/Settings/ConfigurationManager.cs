using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Retinues.Editor;
using Retinues.Editor.Events;
using Retinues.Framework.Runtime;
using Retinues.Interface.Services;
using Retinues.Utilities;
using TaleWorlds.Localization;

namespace Retinues.Settings
{
    /// <summary>
    /// Central manager for Retinues settings: option registry, values, visibility, events, and logging.
    /// </summary>
    [SafeClass]
    public static class ConfigurationManager
    {
        private static readonly List<IOption> _options = [];
        private static readonly Dictionary<string, IOption> _byKey = new(
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
        private static bool _loadingConfig;

        private static readonly Dictionary<string, object> _restartWarnedValueByKey = new(
            StringComparer.OrdinalIgnoreCase
        );

        private const double SaveDebounceSeconds = 0.2;
        private static bool _pendingConfigSave;
        private static double _configSaveDueTime;

        /// <summary>
        /// Global config change event.
        /// </summary>
        public static event Action<string, object> OptionChanged;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Access                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// All discovered sections (ordered by declaration order).
        /// </summary>
        public static IReadOnlyList<Section> Sections
        {
            get
            {
                DiscoverOptions();
                return _sections;
            }
        }

        /// <summary>
        /// All discovered options (ordered by declaration order).
        /// </summary>
        public static IReadOnlyList<IOption> Options
        {
            get
            {
                DiscoverOptions();
                return _options;
            }
        }

        /// <summary>
        /// Returns all options in the given section (ordered by declaration order).
        /// </summary>
        public static IReadOnlyList<IOption> GetOptionsInSection(string sectionName)
        {
            DiscoverOptions();

            sectionName ??= string.Empty;

            // Small list; allocate per call (panel rebuild only).
            var list = new List<IOption>();

            for (int i = 0; i < _options.Count; i++)
            {
                var opt = _options[i];
                if (opt == null)
                    continue;

                if (
                    !string.Equals(
                        opt.Section ?? string.Empty,
                        sectionName,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                    continue;

                list.Add(opt);
            }

            // Preserve declaration order.
            list.Sort((a, b) => GetOrdinal(a?.Key).CompareTo(GetOrdinal(b?.Key)));
            return list;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Creation API                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Helper factory to create a Section. Intended to be used from Settings.
        /// </summary>
        public static Section CreateSection(Func<string> name, Func<string> description = null) =>
            new(name, description);

        /// <summary>
        /// Helper factory to create a typed option. Enums work naturally as dropdowns.
        /// Provide choices only if you want a subset or custom labels/hints.
        /// </summary>
        public static Option<T> CreateOption<T>(
            Section section,
            Func<string> name,
            T @default,
            Func<string> description = null,
            double minValue = 0,
            double maxValue = 1000,
            bool disabled = false,
            T disabledOverride = default,
            IReadOnlyList<(T value, string label, string hint)> choices = null,
            IOption dependsOn = null,
            object dependsOnValue = null,
            object dependsOnDisabledOverride = null,
            UIEvent[] fires = null,
            Action<object, object> onChange = null,
            bool requiresRestart = false,
            object presetFreeform = null,
            object presetRealistic = null
        )
        {
            Func<string> sectionFunc = null;
            if (section != null)
                sectionFunc = () => section.Name;

            void OnChangedWrapper(IOption opt, object oldValue, object newValue)
            {
                // Mirror default behavior: no side-effects during load/init.
                if (_loadingConfig)
                    return;

                try
                {
                    OnOptionChanged(opt, oldValue, newValue);
                }
                catch
                {
                    // Edge-safe.
                }

                if (onChange == null)
                    return;

                try
                {
                    onChange(oldValue, newValue);
                }
                catch (Exception e)
                {
                    Log.Exception(e, "CreateOption onChange callback failed.");
                }
            }

            return new Option<T>(
                section: sectionFunc,
                name: name,
                description: description,
                @default: @default,
                minValue: minValue,
                maxValue: maxValue,
                requiresRestart: requiresRestart,
                disabled: disabled,
                disabledOverride: disabledOverride,
                choices: choices,
                dependsOn: dependsOn,
                dependsOnValue: dependsOnValue,
                dependsOnDisabledOverride: dependsOnDisabledOverride,
                presetFreeform: presetFreeform,
                presetRealistic: presetRealistic,
                fires: fires,
                onChanged: OnChangedWrapper
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Presets                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Applies the given preset to all options, then saves.
        /// </summary>
        public static void ApplyPreset(SettingsPreset preset)
        {
            DiscoverOptions();

            try
            {
                _loadingConfig = true;

                for (int i = 0; i < _options.Count; i++)
                {
                    var opt = _options[i];
                    if (opt == null)
                        continue;

                    object value = preset switch
                    {
                        SettingsPreset.Freeform => opt.FreeformValue,
                        SettingsPreset.Realistic => opt.RealisticValue,
                        _ => opt.Default,
                    };

                    opt.SetObject(value);
                }
            }
            finally
            {
                _loadingConfig = false;
            }

            ConfigurationPersistence.SaveOnChange(_sections, _options);

            // Notify OptionVMs of every changed value so they refresh immediately.
            for (int i = 0; i < _options.Count; i++)
            {
                var opt = _options[i];
                if (opt?.Key == null)
                    continue;

                try
                {
                    OptionChanged?.Invoke(opt.Key, opt.GetObject());
                }
                catch { }
            }

            // Also fire the broad UI events each option declares.
            if (EditorScreen.IsOpen)
            {
                for (int i = 0; i < _options.Count; i++)
                {
                    var opt = _options[i];
                    if (opt?.Key != null)
                        FireUIEventsIfNeeded(opt.Key);
                }
            }
        }

        /// <summary>
        /// Counts how many options would change from their current value if the preset were applied.
        /// </summary>
        public static int GetPresetChangeCount(SettingsPreset preset)
        {
            DiscoverOptions();

            int count = 0;
            for (int i = 0; i < _options.Count; i++)
            {
                var opt = _options[i];
                if (opt == null)
                    continue;

                object presetValue = preset switch
                {
                    SettingsPreset.Freeform => opt.FreeformValue,
                    SettingsPreset.Realistic => opt.RealisticValue,
                    _ => opt.Default,
                };

                if (!Equals(opt.GetObject(), presetValue))
                    count++;
            }

            return count;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Discovery                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Discovers all static Section and IOption fields in the Settings class via reflection.
        /// Preserves declaration ordering using metadata tokens.
        /// </summary>
        internal static void DiscoverOptions()
        {
            if (_discovered)
                return;

            try
            {
                Type settingsType = typeof(Configuration);
                FieldInfo[] fields = settingsType.GetFields(
                    BindingFlags.Public | BindingFlags.Static
                );

                int ordinal = 0;

                foreach (FieldInfo f in fields.OrderBy(fi => fi.MetadataToken))
                {
                    if (typeof(Section).IsAssignableFrom(f.FieldType))
                    {
                        if (f.GetValue(null) is not Section section)
                            continue;

                        section.Ordinal = _sections.Count;
                        _sections.Add(section);

                        string sectionName = section.Name;
                        _ = section.Description;
                        if (
                            !string.IsNullOrWhiteSpace(sectionName)
                            && !_sectionOrdinalByName.ContainsKey(sectionName)
                        )
                            _sectionOrdinalByName[sectionName] = section.Ordinal;

                        continue;
                    }

                    if (!typeof(IOption).IsAssignableFrom(f.FieldType))
                        continue;

                    if (f.GetValue(null) is not IOption opt)
                        continue;

                    string key = f.Name;
                    opt.Key = key;

                    _options.Add(opt);
                    _byKey[key] = opt;
                    _ordinalByKey[key] = ordinal++;

                    _ = opt.Name;
                    _ = opt.Description;
                }

                _discovered = true;

                try
                {
                    _loadingConfig = true;
                    ConfigurationPersistence.LoadOrInit(_sections, _options);
                }
                finally
                {
                    _loadingConfig = false;
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "DiscoverOptions failed.");
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
        /// Gets the ordinal of a section by name.
        /// </summary>
        internal static int GetSectionOrdinal(string sectionName)
        {
            if (_sectionOrdinalByName.TryGetValue(sectionName, out int ordinal))
                return ordinal;
            return int.MaxValue;
        }

        /// <summary>
        /// Gets the ordinal of an option by key.
        /// </summary>
        internal static int GetOrdinal(string key)
        {
            if (_ordinalByKey.TryGetValue(key, out int idx))
                return idx;
            return int.MaxValue;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const double EventThrottleSeconds = 0.1;

        private static readonly Dictionary<string, double> _lastUiEventFireTime = new(
            StringComparer.OrdinalIgnoreCase
        );
        private static readonly HashSet<string> _pendingUiEventKeys = new(
            StringComparer.OrdinalIgnoreCase
        );

        /// <summary>
        /// Handles option value changes by persisting and notifying listeners.
        /// </summary>
        private static void OnOptionChanged(IOption opt, object oldValue, object newValue)
        {
            try
            {
                if (_loadingConfig)
                    return;

                string key = opt?.Key;
                if (string.IsNullOrWhiteSpace(key))
                    return;

                try
                {
                    DiscoverOptions();

                    // Sliders (int/float/double) can fire many changes while dragging.
                    // Debounce disk writes for these by ~0.2s.
                    if (EditorScreen.IsOpen && IsSliderType(opt.Type))
                    {
                        _pendingConfigSave = true;
                        _configSaveDueTime = NowSeconds() + SaveDebounceSeconds;
                    }
                    else
                    {
                        _pendingConfigSave = false;
                        ConfigurationPersistence.SaveOnChange(_sections, _options);
                    }
                }
                catch
                {
                    // ignore
                }

                try
                {
                    OptionChanged?.Invoke(key, newValue);
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

                try
                {
                    if (
                        opt.RequiresRestart
                        && (
                            !_restartWarnedValueByKey.TryGetValue(key, out var warnedVal)
                            || !Equals(warnedVal, newValue)
                        )
                    )
                    {
                        _restartWarnedValueByKey[key] = newValue;

                        Inquiries.Popup(
                            title: new TextObject("Restart may be required"),
                            description: new TextObject(
                                "Restarting the game may be required for this change to take effect."
                            ),
                            pauseGame: true,
                            delayUntilOnWorldMap: false
                        );
                    }
                }
                catch (Exception e)
                {
                    Log.Exception(e, "RequiresRestart warning popup failed.");
                }
            }
            catch
            {
                // Edge-safe.
            }
        }

        /// <summary>
        /// Gets a monotonic timestamp in seconds.
        /// </summary>
        private static double NowSeconds()
        {
            return Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        }

        /// <summary>
        /// Queues or immediately fires UI update events for an option key.
        /// </summary>
        private static void QueueUIEventsIfNeeded(string key)
        {
            if (!EditorScreen.IsOpen)
                return;

            DiscoverOptions();

            if (!_byKey.TryGetValue(key, out var opt) || opt == null)
                return;

            if (opt.Fires == null || opt.Fires.Length == 0)
                return;

            if (opt.Type != typeof(float) && opt.Type != typeof(double))
            {
                _lastUiEventFireTime[key] = NowSeconds();
                FireUIEventsIfNeeded(key);
                return;
            }

            double now = NowSeconds();

            if (!_lastUiEventFireTime.TryGetValue(key, out double last))
                last = 0;

            if (now - last >= EventThrottleSeconds)
            {
                _lastUiEventFireTime[key] = now;
                FireUIEventsIfNeeded(key);
                return;
            }

            _pendingUiEventKeys.Add(key);
        }

        /// <summary>
        /// Performs periodic processing for debounced saves and throttled UI events.
        /// </summary>
        internal static void Tick()
        {
            if (!EditorScreen.IsOpen)
                return;

            try
            {
                FlushPendingConfigSaveIfDue();
            }
            catch
            {
                // ignore
            }

            if (_pendingUiEventKeys.Count == 0)
                return;

            double now = NowSeconds();

            foreach (var key in _pendingUiEventKeys.ToArray())
            {
                if (!_lastUiEventFireTime.TryGetValue(key, out double last))
                    last = 0;

                if (now - last < EventThrottleSeconds)
                    continue;

                _pendingUiEventKeys.Remove(key);
                _lastUiEventFireTime[key] = now;

                try
                {
                    FireUIEventsIfNeeded(key);
                }
                catch (Exception e)
                {
                    Log.Exception(e, "EventManager.Fire failed for throttled event.");
                }
            }
        }

        /// <summary>
        /// Fires configured UI events for the option associated with the given key.
        /// </summary>
        private static void FireUIEventsIfNeeded(string key)
        {
            if (!EditorScreen.IsOpen)
                return;

            DiscoverOptions();

            if (!_byKey.TryGetValue(key, out var opt) || opt == null)
                return;

            if (opt.Fires == null || opt.Fires.Length == 0)
                return;

            foreach (var ev in opt.Fires)
            {
                try
                {
                    EventManager.Fire(ev);
                }
                catch (Exception e)
                {
                    Log.Exception(e, "EventManager.Fire failed for event.");
                }
            }
        }

        /// <summary>
        /// Determines whether a type is treated as a slider for debouncing purposes.
        /// </summary>
        private static bool IsSliderType(Type t)
        {
            return t == typeof(int) || t == typeof(float) || t == typeof(double);
        }

        /// <summary>
        /// Flushes a pending debounced configuration save when due.
        /// </summary>
        private static void FlushPendingConfigSaveIfDue()
        {
            if (!_pendingConfigSave)
                return;

            double now = NowSeconds();
            if (now < _configSaveDueTime)
                return;

            _pendingConfigSave = false;

            DiscoverOptions();
            ConfigurationPersistence.SaveOnChange(_sections, _options);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Logging                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Writes the current configuration state to the log.
        /// </summary>
        public static void LogSettings()
        {
            try
            {
                DiscoverOptions();

                Log.Info("Retinues Config:");

                var grouped = new Dictionary<string, List<IOption>>();
                for (int i = 0; i < _options.Count; i++)
                {
                    IOption opt = _options[i];
                    if (!grouped.TryGetValue(opt.Section, out List<IOption> list))
                    {
                        list = [];
                        grouped[opt.Section] = list;
                    }

                    list.Add(opt);
                }

                foreach (var kv in grouped.OrderBy(g => GetSectionOrdinal(g.Key)))
                {
                    string sectionName = kv.Key;
                    List<IOption> options = kv.Value;

                    Log.Info($"[{sectionName}]");

                    options.Sort((a, b) => GetOrdinal(a.Key).CompareTo(GetOrdinal(b.Key)));

                    foreach (var opt in options)
                    {
                        object current = opt.GetObject();
                        object def = opt.Default;

                        string currentText = Convert.ToString(
                            current,
                            CultureInfo.InvariantCulture
                        );
                        string defaultText = Convert.ToString(def, CultureInfo.InvariantCulture);

                        bool changed = !Equals(current, def);
                        string marker = changed ? "*" : " ";

                        string label = string.IsNullOrWhiteSpace(opt.Name)
                            ? opt.Key
                            : opt.Name + " [" + opt.Key + "]";

                        if (opt.IsDisabled)
                            Log.Info($"{marker} {label} = {currentText} (DISABLED; override)");
                        else
                            Log.Info($"{marker} {label} = {currentText} (default: {defaultText})");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "LogSettings failed.");
            }
        }
    }
}
