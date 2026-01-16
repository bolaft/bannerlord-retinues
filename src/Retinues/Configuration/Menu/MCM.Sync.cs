using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using MCM.Common;
using Retinues.Utilities;

namespace Retinues.Configuration.Menu
{
    public static partial class MCM
    {
        /* ━━━━━━━━ Statics ━━━━━━━ */

        private static object _MCMSettingsInstance;
        private static Type _MCMSettingsType;
        private static bool _isSyncingWithMCM;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Dropdown                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly Dictionary<string, Dropdown<string>> _dropdownsByKey = new(
            StringComparer.OrdinalIgnoreCase
        );

        /// <summary>
        /// Get or create the dropdown for the given multi-choice option.
        /// </summary>
        private static Dropdown<string> GetOrCreateDropdown(string key, IMultiChoiceOption opt)
        {
            // Build labels
            var labels = opt.Choices.Select(c => opt.ChoiceFormatter(c) ?? string.Empty).ToList();
            if (labels.Count == 0)
                labels.Add(string.Empty);

            var selected = opt.SelectedIndex;
            if (selected < 0)
                selected = 0;
            if (selected >= labels.Count)
                selected = labels.Count - 1;

            if (!_dropdownsByKey.TryGetValue(key, out var dd))
            {
                dd = new Dropdown<string>(labels, selected);
                _dropdownsByKey[key] = dd;

                // When MCM UI changes SelectedIndex, update the option
                dd.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName != nameof(Dropdown<>.SelectedIndex))
                        return;

                    if (_isSyncingWithMCM)
                        return;

                    try
                    {
                        _isSyncingWithMCM = true;
                        opt.SelectedIndex = dd.SelectedIndex;
                    }
                    finally
                    {
                        _isSyncingWithMCM = false;
                    }
                };
            }
            else
            {
                // Keep existing instance (important), but refresh labels/selection
                dd.Clear();
                dd.AddRange(labels);
                dd.SelectedIndex = selected;
            }

            return dd;
        }

        /// <summary>
        /// Apply incoming MCM dropdown state to the option's dropdown.
        /// </summary>
        private static void ApplyDropdownFromMCM(
            string key,
            IMultiChoiceOption opt,
            Dropdown<string> incoming
        )
        {
            if (string.IsNullOrWhiteSpace(key) || opt == null)
                return;

            if (!_dropdownsByKey.TryGetValue(key, out var dd) || dd == null)
                return;

            if (_isSyncingWithMCM)
                return;

            try
            {
                _isSyncingWithMCM = true;

                if (incoming != null)
                {
                    bool hasItems = false;
                    try
                    {
                        hasItems = incoming.Count > 0;
                    }
                    catch
                    {
                        // ignore
                    }

                    // IMPORTANT: avoid wiping the dropdown when MCM gives us the same instance back.
                    if (hasItems && !ReferenceEquals(incoming, dd))
                    {
                        dd.Clear();
                        dd.AddRange(incoming);
                    }

                    int idx = incoming.SelectedIndex;
                    if (idx < 0)
                        idx = 0;
                    if (dd.Count > 0 && idx >= dd.Count)
                        idx = dd.Count - 1;

                    dd.SelectedIndex = idx;
                    opt.SelectedIndex = idx;
                }
                else
                {
                    int idx = opt.SelectedIndex;
                    if (idx < 0)
                        idx = 0;
                    if (dd.Count > 0 && idx >= dd.Count)
                        idx = dd.Count - 1;

                    dd.SelectedIndex = idx;
                }
            }
            finally
            {
                _isSyncingWithMCM = false;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Hook into MCM's settings instance to listen for changes.
        /// </summary>
        private static void HookMCMSettings(object settings)
        {
            if (settings == null)
                return;

            _MCMSettingsInstance = settings;
            _MCMSettingsType = settings.GetType();

            // Ensure we only have one handler attached
            SettingsManager.OptionChanged -= SyncOptionToMCM;
            SettingsManager.OptionChanged += SyncOptionToMCM;
        }

        /// <summary>
        /// Sync a changed option value to MCM.
        /// </summary>
        private static void SyncOptionToMCM(string key, object value)
        {
            if (
                SettingsManager.TryGetOption(key, out var opt)
                && opt is IMultiChoiceOption mc
                && _dropdownsByKey.TryGetValue(key, out var dd)
            )
            {
                if (_isSyncingWithMCM)
                    return;

                try
                {
                    _isSyncingWithMCM = true;

                    int idx = mc.SelectedIndex;
                    if (idx < 0)
                        idx = 0;

                    dd.SelectedIndex = idx;
                }
                finally
                {
                    _isSyncingWithMCM = false;
                }

                return;
            }

            var settings = _MCMSettingsInstance;
            var type = _MCMSettingsType;
            if (settings == null || type == null)
                return;

            // Prevent infinite recursion when we set the MCM property
            if (_isSyncingWithMCM)
                return;

            try
            {
                var prop = type.GetProperty(
                    key,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (prop == null || !prop.CanWrite)
                    return;

                _isSyncingWithMCM = true;

                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                var coerced = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
                prop.SetValue(settings, coerced);
            }
            catch (Exception e)
            {
                Log.Exception(e, "Failed to sync option to MCM.");
            }
            finally
            {
                _isSyncingWithMCM = false;
            }
        }
    }
}
