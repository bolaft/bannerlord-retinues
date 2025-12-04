using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using MCM.Abstractions;
using MCM.Abstractions.Base.Global;
using MCM.Abstractions.FluentBuilder;
using MCM.Common;
using Retinues.Utilities;

namespace Retinues.Configuration
{
    /// <summary>
    /// MCM interop wrapper for Retinues settings.
    /// Generic settings logic lives in SettingsManager and Settings.
    /// </summary>
    public static class MCM
    {
        private const string MCMId = "Retinues.Settings";
        private const string MCMDisplay = "Retinues";
        private const string MCMFolder = "Retinues";
        private const string MCMFormat = "xml";

        private static FluentGlobalSettings _MCMSettings;
        private static object _MCMSettingsInstance;
        private static Type _MCMSettingsType;
        private static bool _isSyncingWithMCM;
        private static bool _isRegistered;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Register the configuration page and options with MCM (returns true when successful).
        /// Can safely be called many times; after first success it is a cheap no-op.
        /// </summary>
        public static bool Register()
        {
            if (_isRegistered)
                return true;

            try
            {
                Log.Info("Attempting to register Retinues with MCM...");

                // Ensure SettingsManager has discovered all options
                var _ = SettingsManager.AllOptions;

                var ok = Build();
                if (!ok)
                {
                    // Builder not ready or provider refused the page this tick
                    return false;
                }

                _isRegistered = true;
                Log.Info("Retinues options registered with MCM.");
                return true;
            }
            catch (Exception e)
            {
                Log.Exception(e, "MCM.Register failed.");
                return false;
            }
        }

        /// <summary>
        /// Ask MCM to persist the current Retinues settings to disk.
        /// Uses MCM's DefaultSettingsProvider pipeline, so it behaves exactly
        /// like pressing 'Save' in the MCM UI.
        /// </summary>
        public static void Save()
        {
            try
            {
                if (_MCMSettings == null)
                {
                    Log.Info("MCM.Save called but _mcmSettings is null; skipping.");
                    return;
                }

                var provider = BaseSettingsProvider.Instance;
                if (provider == null)
                {
                    Log.Info(
                        "MCM.Save called but BaseSettingsProvider.Instance is null; skipping."
                    );
                    return;
                }

                provider.SaveSettings(_MCMSettings);
            }
            catch (Exception e)
            {
                Log.Exception(e, "MCM.Save failed.");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Builder                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Build the MCM menu and register option groups and controls.
        /// Returns true when the page was successfully registered.
        /// </summary>
        private static bool Build()
        {
            // Create and configure the fluent builder
            var builder = BaseSettingsBuilder.Create(MCMId, MCMDisplay);
            if (builder == null)
            {
                Log.Info(
                    "MCM not ready (BaseSettingsBuilder.Create returned null). Skipping registration this tick."
                );
                return false;
            }

            // Some providers require these to surface the page
            builder
                .SetFolderName(MCMFolder) // "Retinues"
                .SetFormat(MCMFormat); // "xml"

            IReadOnlyList<IOption> all = SettingsManager.AllOptions;

            // Group options by Section
            foreach (var groupBySection in all.GroupBy(o => o.Section))
            {
                builder.CreateGroup(
                    groupBySection.Key,
                    group =>
                    {
                        // Use Settings section declaration order
                        int groupOrder = SettingsManager.GetSectionOrdinal(groupBySection.Key);
                        group.SetGroupOrder(groupOrder);

                        int oIndex = 0;

                        foreach (
                            var opt in groupBySection.OrderBy(o =>
                                SettingsManager.GetOrdinal(o.Key)
                            )
                        )
                        {
                            string id = opt.Key;
                            string name = opt.Name;

                            // Disabled options: force override and do not add UI control
                            if (opt.IsDisabled)
                            {
                                opt.SetObject(opt.DisabledOverrideBoxed);
                                continue;
                            }

                            // Multi-choice options: not yet wired as dropdowns in MCM
                            if (opt is IMultiChoiceOption)
                            {
                                Log.Info(
                                    $"Option '{opt.Key}' is multi-choice; not exposed to MCM UI."
                                );
                                continue;
                            }

                            switch (Type.GetTypeCode(opt.Type))
                            {
                                case TypeCode.Boolean:
                                    group.AddBool(
                                        id,
                                        name,
                                        new ProxyRef<bool>(
                                            () =>
                                            {
                                                object v = opt.GetObject();
                                                return Convert.ToBoolean(
                                                    v,
                                                    CultureInfo.InvariantCulture
                                                );
                                            },
                                            v => opt.SetObject(v)
                                        ),
                                        b =>
                                            b.SetOrder(oIndex++)
                                                .SetHintText(opt.Hint)
                                                .SetRequireRestart(opt.RequiresRestart)
                                    );
                                    break;

                                case TypeCode.Int32:
                                    group.AddInteger(
                                        id,
                                        name,
                                        opt.MinValue,
                                        opt.MaxValue,
                                        new ProxyRef<int>(
                                            () =>
                                            {
                                                object v = opt.GetObject();
                                                return Convert.ToInt32(
                                                    v,
                                                    CultureInfo.InvariantCulture
                                                );
                                            },
                                            v => opt.SetObject(v)
                                        ),
                                        b =>
                                            b.SetOrder(oIndex++)
                                                .SetHintText(opt.Hint)
                                                .SetRequireRestart(opt.RequiresRestart)
                                    );
                                    break;

                                case TypeCode.Single:
                                case TypeCode.Double:
                                    group.AddFloatingInteger(
                                        id,
                                        name,
                                        minValue: opt.MinValue,
                                        maxValue: opt.MaxValue,
                                        new ProxyRef<float>(
                                            () =>
                                            {
                                                object v = opt.GetObject();
                                                return Convert.ToSingle(
                                                    v,
                                                    CultureInfo.InvariantCulture
                                                );
                                            },
                                            v => opt.SetObject(v)
                                        ),
                                        b =>
                                            b.SetOrder(oIndex++)
                                                .SetHintText(opt.Hint)
                                                .SetRequireRestart(opt.RequiresRestart)
                                    );
                                    break;

                                case TypeCode.String:
                                    group.AddText(
                                        id,
                                        name,
                                        new ProxyRef<string>(
                                            () =>
                                            {
                                                object v = opt.GetObject();
                                                if (v == null)
                                                    return string.Empty;
                                                return v.ToString();
                                            },
                                            v => opt.SetObject(v)
                                        ),
                                        b =>
                                            b.SetOrder(oIndex++)
                                                .SetHintText(opt.Hint)
                                                .SetRequireRestart(opt.RequiresRestart)
                                    );
                                    break;
                            }
                        }
                    }
                );
            }

            static void ApplyPreset(
                ISettingsPresetBuilder p,
                IReadOnlyDictionary<string, object> map
            )
            {
                foreach (var kv in map)
                    p.SetPropertyValue(kv.Key, kv.Value);
            }

            var freeform = all.ToDictionary(
                o => o.Key,
                o => o.PresetOverrides.TryGetValue(Presets.Freeform, out var v) ? v : o.Default
            );
            var realistic = all.ToDictionary(
                o => o.Key,
                o => o.PresetOverrides.TryGetValue(Presets.Realistic, out var v) ? v : o.Default
            );

            builder.CreatePreset(Presets.Freeform, "Freeform", p => ApplyPreset(p, freeform));
            builder.CreatePreset(Presets.Realistic, "Realistic", p => ApplyPreset(p, realistic));

            _MCMSettings = builder.BuildAsGlobal();
            if (_MCMSettings == null)
                return false;

            HookMCMSettings(_MCMSettings);
            _MCMSettings.Register();

            return true;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

        private static void SyncOptionToMCM(string key, object value)
        {
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
