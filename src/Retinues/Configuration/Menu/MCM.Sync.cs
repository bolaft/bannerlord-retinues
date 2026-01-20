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
        /// Ensure a dropdown exists for the given option and return the current instance.
        /// The returned instance is always "owned" by us (cached) and wired for copy-on-write.
        /// </summary>
        private static Dropdown<string> EnsureDropdown(string key, IMultiChoiceOption opt)
        {
            if (string.IsNullOrWhiteSpace(key) || opt == null)
                return null;

            var labels = BuildDropdownLabels(opt);
            int idx = ClampIndex(opt.SelectedIndex, labels.Count);

            if (_dropdownsByKey.TryGetValue(key, out var existing) && existing != null)
                return existing;

            var dd = new Dropdown<string>(labels, idx);
            InstallDropdownInstance(key, dd, wireCopyOnWrite: true);
            return dd;
        }

        /// <summary>
        /// Apply incoming MCM dropdown state to the option's dropdown (used when MCM applies presets).
        /// IMPORTANT: always install a fresh instance (copy-on-write) to avoid default snapshots sharing refs.
        /// </summary>
        private static void ApplyDropdownFromMCM(
            string key,
            IMultiChoiceOption opt,
            Dropdown<string> incoming
        )
        {
            if (string.IsNullOrWhiteSpace(key) || opt == null)
                return;

            if (_isSyncingWithMCM)
                return;

            try
            {
                _isSyncingWithMCM = true;

                if (incoming != null)
                {
                    // Build items from incoming (if possible), otherwise fallback to option labels
                    var items = TrySnapshotItems(incoming) ?? BuildDropdownLabels(opt);
                    int idx = ClampIndex(incoming.SelectedIndex, items.Count);

                    // Install a fresh instance into our cache + MCM property, then sync option to it
                    var dd = new Dropdown<string>(items, idx);
                    InstallDropdownInstance(key, dd, wireCopyOnWrite: true);

                    opt.SelectedIndex = idx;
                }
                else
                {
                    // No incoming: just reflect current option state
                    var items = BuildDropdownLabels(opt);
                    int idx = ClampIndex(opt.SelectedIndex, items.Count);

                    var dd = new Dropdown<string>(items, idx);
                    InstallDropdownInstance(key, dd, wireCopyOnWrite: true);
                }
            }
            finally
            {
                _isSyncingWithMCM = false;
            }
        }

        /// <summary>
        /// Build label list for a multi-choice option.
        /// </summary>
        private static List<string> BuildDropdownLabels(IMultiChoiceOption opt)
        {
            var labels = opt.Choices.Select(c => opt.ChoiceFormatter(c) ?? string.Empty).ToList();
            if (labels.Count == 0)
                labels.Add(string.Empty);
            return labels;
        }

        /// <summary>
        /// Clamp an index into [0..count-1] (count may be 0).
        /// </summary>
        private static int ClampIndex(int idx, int count)
        {
            if (idx < 0)
                idx = 0;

            if (count <= 0)
                return 0;

            if (idx >= count)
                idx = count - 1;

            return idx;
        }

        /// <summary>
        /// Snapshot items from a Dropdown without assuming its enumerator behavior is perfect.
        /// </summary>
        private static List<string> TrySnapshotItems(Dropdown<string> dd)
        {
            try
            {
                var items = new List<string>();
                foreach (var s in dd)
                    items.Add(s);

                if (items.Count == 0)
                    return null;

                return items;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Install the dropdown instance into:
        /// - our cache
        /// - the MCM settings property (reflection), when possible
        /// and optionally wire copy-on-write so selection changes replace the instance.
        /// </summary>
        private static void InstallDropdownInstance(string key, Dropdown<string> dd, bool wireCopyOnWrite)
        {
            if (string.IsNullOrWhiteSpace(key) || dd == null)
                return;

            _dropdownsByKey[key] = dd;

            // Push into MCM settings instance (so UI binds to our instance)
            TrySetMCMProperty(key, dd);

            if (!wireCopyOnWrite)
                return;

            dd.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(dd.SelectedIndex))
                    return;

                if (_isSyncingWithMCM)
                    return;

                if (!SettingsManager.TryGetOption(key, out var opt) || opt is not IMultiChoiceOption mc)
                    return;

                try
                {
                    _isSyncingWithMCM = true;

                    // Update underlying option first
                    mc.SelectedIndex = dd.SelectedIndex;

                    // Copy-on-write: replace the dropdown instance (prevents preset baselines sharing refs)
                    ReplaceDropdownInstanceFromCache(key, dd);
                }
                finally
                {
                    _isSyncingWithMCM = false;
                }
            };
        }

        /// <summary>
        /// Copy-on-write replacement: clone the given dropdown and install the clone.
        /// </summary>
        private static void ReplaceDropdownInstanceFromCache(string key, Dropdown<string> current)
        {
            if (string.IsNullOrWhiteSpace(key) || current == null)
                return;

            var items = TrySnapshotItems(current) ?? new List<string> { string.Empty };
            int idx = ClampIndex(current.SelectedIndex, items.Count);

            var clone = new Dropdown<string>(items, idx);
            InstallDropdownInstance(key, clone, wireCopyOnWrite: true);
        }

        /// <summary>
        /// Try to set the property on MCM's settings instance, if available and assignable.
        /// </summary>
        private static void TrySetMCMProperty(string key, Dropdown<string> value)
        {
            var settings = _MCMSettingsInstance;
            var type = _MCMSettingsType;
            if (settings == null || type == null)
                return;

            try
            {
                var prop = type.GetProperty(
                    key,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (prop == null || !prop.CanWrite)
                    return;

                if (!prop.PropertyType.IsAssignableFrom(typeof(Dropdown<string>)))
                    return;

                prop.SetValue(settings, value);
            }
            catch
            {
                // Best-effort; ignore.
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
        /// For multi-choice options, we only push the selected index to the dropdown.
        /// (The "Default" preset baseline issue is handled by removing MCM's auto-captured
        /// default preset and providing our own real default preset in the Builder.)
        /// </summary>
        private static void SyncOptionToMCM(string key, object value)
        {
            // Multi-choice: just push selection to the bound dropdown.
            if (SettingsManager.TryGetOption(key, out var opt) && opt is IMultiChoiceOption mc)
            {
                if (_isSyncingWithMCM)
                    return;

                try
                {
                    _isSyncingWithMCM = true;

                    var dd = EnsureDropdown(key, mc);
                    if (dd != null)
                        dd.SelectedIndex = ClampIndex(mc.SelectedIndex, dd.Count);
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
