using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MCM.Abstractions.Base.Global;
using MCM.Abstractions.FluentBuilder;
using MCM.Common;
using Retinues.Utilities;

namespace Retinues.Configuration.Menu
{
    public static partial class MCM
    {
        /* ━━━━━━━ Constants ━━━━━━ */

        private const string MCMId = "Retinues.Settings";
        private const string MCMDisplay = "Retinues";
        private const string MCMFolder = "Retinues";
        private const string MCMFormat = "xml";

        /* ━━━━━━━━ Statics ━━━━━━━ */

        private static FluentGlobalSettings _MCMSettings;

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

            // IMPORTANT:
            // MCM's fluent builder creates a built-in "Default" preset by capturing
            // the current property reference values at build time.
            // For mutable types (notably Dropdown<T>), that captured object can later be
            // mutated by the UI, making MCM think the current state is still "Default".
            //
            // Fix: remove MCM's auto-captured default preset and re-add our own "Default"
            // preset based on the real option defaults (opt.Default).
            builder.WithoutDefaultPreset();

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

                            // Multi-choice options: dropdowns
                            if (opt is IMultiChoiceOption mc)
                            {
                                // AddDropdown's selectedIndex is only a UI default; real default preset is handled below.
                                int defaultIndex = 0;
                                var choices = mc.Choices;

                                if (choices != null && choices.Count > 0)
                                {
                                    object def = opt.Default;

                                    if (def != null)
                                    {
                                        for (int i = 0; i < choices.Count; i++)
                                        {
                                            if (Equals(choices[i], def))
                                            {
                                                defaultIndex = i;
                                                break;
                                            }
                                        }
                                    }

                                    if (defaultIndex < 0)
                                        defaultIndex = 0;
                                    if (defaultIndex >= choices.Count)
                                        defaultIndex = choices.Count - 1;
                                }

                                group.AddDropdown(
                                    id,
                                    name,
                                    selectedIndex: defaultIndex,
                                    @ref: new ProxyRef<Dropdown<string>>(
                                        // IMPORTANT: do not capture a local dd here
                                        () => EnsureDropdown(opt.Key, mc),
                                        v => ApplyDropdownFromMCM(opt.Key, mc, v)
                                    ),
                                    builder: d =>
                                        d.SetOrder(oIndex++)
                                            .SetHintText(opt.Hint)
                                            .SetRequireRestart(opt.RequiresRestart)
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
                                        ToIntMin(opt.MinValue),
                                        ToIntMax(opt.MaxValue),
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
                                        minValue: (float)opt.MinValue,
                                        maxValue: (float)opt.MaxValue,
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

            /// <summary>
            /// Apply preset values from the given map.
            /// </summary>
            static void ApplyPreset(
                ISettingsPresetBuilder p,
                IReadOnlyDictionary<string, object> map
            )
            {
                foreach (var kv in map)
                    p.SetPropertyValue(kv.Key, kv.Value);
            }

            /// <summary>
            /// Get the preset value for the given option and preset key.
            /// </summary>
            static object MakePresetValue(IOption opt, string presetKey)
            {
                object desired;

                if (presetKey != null && opt.PresetOverrides != null)
                {
                    if (opt.PresetOverrides.TryGetValue(presetKey, out var v) && v != null)
                        desired = v;
                    else
                        desired = opt.Default;
                }
                else
                {
                    desired = opt.Default;
                }

                if (opt is IMultiChoiceOption mc)
                {
                    var choices = mc.Choices;
                    if (choices == null || choices.Count == 0)
                        return new Dropdown<string>(new[] { string.Empty }, 0);

                    int idx = 0;

                    if (desired != null)
                    {
                        for (int i = 0; i < choices.Count; i++)
                        {
                            if (Equals(choices[i], desired))
                            {
                                idx = i;
                                break;
                            }
                        }
                    }

                    var labels = choices
                        .Select(c => mc.ChoiceFormatter(c) ?? string.Empty)
                        .ToList();
                    if (labels.Count == 0)
                        labels.Add(string.Empty);

                    if (idx < 0)
                        idx = 0;
                    if (idx >= labels.Count)
                        idx = labels.Count - 1;

                    return new Dropdown<string>(labels, idx);
                }

                return desired;
            }

            var @default = all.ToDictionary(o => o.Key, o => MakePresetValue(o, presetKey: null));
            var freeform = all.ToDictionary(o => o.Key, o => MakePresetValue(o, Presets.Freeform));
            var realistic = all.ToDictionary(o => o.Key, o => MakePresetValue(o, Presets.Realistic));

            // Use MCM's built-in localization id for the Default preset label.
            builder.CreatePreset("default", "{=BaseSettings_Default}Default", p => ApplyPreset(p, @default));
            builder.CreatePreset(Presets.Freeform, "Freeform", p => ApplyPreset(p, freeform));
            builder.CreatePreset(Presets.Realistic, "Realistic", p => ApplyPreset(p, realistic));

            _MCMSettings = builder.BuildAsGlobal();
            if (_MCMSettings == null)
                return false;

            HookMCMSettings(_MCMSettings);
            _MCMSettings.Register();

            return true;
        }

        /// <summary>
        /// Convert a double to an int, rounding down, with NaN/Infinity handling.
        /// </summary>
        private static int ToIntMin(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v))
                return 0;
            return (int)Math.Floor(v);
        }

        /// <summary>
        /// Convert a double to an int, rounding up, with NaN/Infinity handling.
        /// </summary>
        private static int ToIntMax(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v))
                return 0;
            return (int)Math.Ceiling(v);
        }
    }
}
