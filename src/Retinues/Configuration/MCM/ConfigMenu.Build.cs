using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MCM.Abstractions.FluentBuilder;
using MCM.Common;
using Retinues.Utilities;

namespace Retinues.Configuration.MCM
{
    public static partial class ConfigMenu
    {
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

                            // Multi-choice options: dropdowns
                            if (opt is IMultiChoiceOption mc)
                            {
                                var dd = GetOrCreateDropdown(opt.Key, mc);

                                group.AddDropdown(
                                    id,
                                    name,
                                    selectedIndex: dd.SelectedIndex,
                                    @ref: new ProxyRef<Dropdown<string>>(() => dd, null),
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

        private static int ToIntMin(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v))
                return 0;
            return (int)Math.Floor(v);
        }

        private static int ToIntMax(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v))
                return 0;
            return (int)Math.Ceiling(v);
        }
    }
}
