using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using MCM.Abstractions.FluentBuilder;
using MCM.Common;
using Retinues.GUI.Helpers;
using Retinues.Safety.Sanitizer;
using Retinues.Troops;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Configuration
{
    public static partial class Config
    {
        private const string McmId = "Retinues.Settings";
        private const string McmDisplay = "Retinues";
        private const string McmFolder = "Retinues";
        private const string McmFormat = "xml";

        // Runtime registry built via reflection from the static Option<T> fields
        private static readonly List<IOption> _all = [];
        private static readonly Dictionary<string, IOption> _byKey = new(
            StringComparer.OrdinalIgnoreCase
        );

        // Declaration order storage for options (to preserve field order)
        private static readonly Dictionary<string, object> _values = new(
            StringComparer.OrdinalIgnoreCase
        );

        // at top with other registries
        private static readonly Dictionary<string, int> _ordinalByKey = new(
            StringComparer.OrdinalIgnoreCase
        );

        /// <summary>
        /// Register the configuration page and options with MCM (returns true when successful).
        /// </summary>
        public static bool RegisterWithMCM()
        {
            try
            {
                Log.Info("Config.RegisterWithMCM: attempting to register with MCM…");
                DiscoverOptions(); // no-op after first call

                var ok = BuildMcmMenu();
                if (ok)
                {
                    Log.Info("Config.RegisterWithMCM: Config options registered with MCM.");
                    return true;
                }

                // Builder not ready or provider refused the page this tick
                return false;
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return false; // don't crash game loop; retry loop will stop logging spam
            }
        }

        /// <summary>
        /// Discover Option<T> fields via reflection and wire their getters/setters.
        /// </summary>
        private static void DiscoverOptions()
        {
            if (_all.Count > 0)
                return;

            var fields = typeof(Config)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => typeof(IOption).IsAssignableFrom(f.FieldType))
                .OrderBy(f => f.MetadataToken);

            var ordinal = 0;
            foreach (var f in fields)
            {
                var opt = (IOption)f.GetValue(null);
                _all.Add(opt);
                _byKey[opt.Key] = opt;
                _ordinalByKey[opt.Key] = ordinal++; // remember discovery order

                // initialize backing store
                _values[opt.Key] = opt.Default;

                // Option<T> type + T
                var optionType = f.FieldType;
                var tArg = optionType.GenericTypeArguments[0];

                // Getter/Setter are PROPERTIES, not fields
                var getterProp = optionType.GetProperty(
                    "Getter",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                var setterProp = optionType.GetProperty(
                    "Setter",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                // Build typed delegates that capture this option's key
                var makeGetter = typeof(Config)
                    .GetMethod(nameof(MakeGetter), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(tArg);
                var makeSetter = typeof(Config)
                    .GetMethod(nameof(MakeSetter), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(tArg);

                var getterDelegate = makeGetter.Invoke(null, new object[] { opt.Key });
                var setterDelegate = makeSetter.Invoke(null, new object[] { opt.Key });

                getterProp!.SetValue(opt, getterDelegate);
                setterProp!.SetValue(opt, setterDelegate);
            }
        }

        /// <summary>
        /// Builds a Func<T> that reads the backing value for the given key.
        /// </summary>
        private static Func<T> MakeGetter<T>(string key) =>
            () =>
            {
                var raw = (T)
                    Convert.ChangeType(_values[key], typeof(T), CultureInfo.InvariantCulture);

                if (
                    _byKey.TryGetValue(key, out var iopt)
                    && iopt is Option<T> optT
                    && optT.IsDisabled
                )
                    return optT.DisabledOverride;

                return raw;
            };

        /// <summary>
        /// Builds an Action<T> that writes the given value into the backing store for the key.
        /// </summary>
        private static Action<T> MakeSetter<T>(string key) => v => _values[key] = v!;

        /// <summary>
        /// Build the MCM menu and register option groups and controls.
        /// </summary>
        private static bool BuildMcmMenu()
        {
            // Create and configure the fluent builder
            var builder = BaseSettingsBuilder.Create(McmId, McmDisplay);
            if (builder is null)
            {
                Log.Warn(
                    "MCM not ready (BaseSettingsBuilder.Create returned null). Skipping registration this tick."
                );
                return false;
            }

            // Some providers require these to surface the page
            builder
                .SetFolderName(McmFolder) // "Retinues"
                .SetFormat(McmFormat); // "xml"

            // Import / Export (utility group pinned first)
            builder.CreateGroup(
                L.S("mcm_section_import_export", "Import & Export"),
                group =>
                {
                    group.SetGroupOrder(-100); // keep at the very top
                    var order = 0;

                    void NotifyNotInCampaign()
                    {
                        InformationManager.ShowInquiry(
                            new InquiryData(
                                L.S("not_in_campaign_title", "No Campaign Loaded"),
                                L.S(
                                    "not_in_campaign_body",
                                    "A save must be loaded first before using import/export functions."
                                ),
                                true, // isAffirmativeOptionShown
                                false, // isNegativeOptionShown
                                GameTexts.FindText("str_ok").ToString(),
                                null,
                                () => { },
                                null
                            )
                        );
                    }

                    // Export button (context-aware; offers Custom-only or Custom+Cultures)
                    group.AddButton(
                        "ExportButton",
                        L.S("mcm_ie_export_btn_text", "Export Troops to XML"),
                        new ProxyRef<Action>(
                            () =>
                                () =>
                                {
                                    if (!InCampaign())
                                    {
                                        NotifyNotInCampaign();
                                        return;
                                    }

                                    try
                                    {
                                        Directory.CreateDirectory(TroopImportExport.DefaultDir);
                                        TroopImportExport.PromptAndExport(
                                            suggestedName: string.IsNullOrWhiteSpace(_exportName)
                                                ? TroopImportExport.SuggestTimestampName("troops")
                                                : _exportName
                                        );
                                    }
                                    catch (Exception e)
                                    {
                                        Notifications.Popup(
                                            L.T("export_fail_title", "Export Failed"),
                                            L.T("export_fail_body", e.Message)
                                        );
                                    }
                                },
                            _ => { }
                        ),
                        L.S("mcm_ie_export_btn", "Export"),
                        b =>
                            b.SetOrder(order++)
                                .SetRequireRestart(false)
                                .SetHintText(
                                    L.S(
                                        "mcm_ie_export_btn_hint",
                                        "Exports troop definitions to XML."
                                    )
                                )
                    );

                    // Import button (validated picker; if both sections → ask which to load)
                    group.AddButton(
                        "ImportFromXml",
                        L.S("mcm_ie_import_pick_btn_text", "Import Troops from XML"),
                        new ProxyRef<Action>(
                            () =>
                                () =>
                                {
                                    if (!InCampaign())
                                    {
                                        NotifyNotInCampaign();
                                        return;
                                    }

                                    try
                                    {
                                        TroopImportExport.PickAndImportUnified();
                                    }
                                    catch (Exception e)
                                    {
                                        Notifications.Popup(
                                            L.T("import_fail_title", "Import Failed"),
                                            L.T("import_fail_body", e.Message)
                                        );
                                    }
                                },
                            _ => { }
                        ),
                        L.S("mcm_ie_import_btn", "Import"),
                        b =>
                            b.SetOrder(order++)
                                .SetRequireRestart(false)
                                .SetHintText(
                                    L.S(
                                        "mcm_ie_import_btn_hint",
                                        "Imports troop definitions from XML."
                                    )
                                )
                    );
                }
            );

            string[] order =
            {
                L.S("mcm_section_global_editor", "Global Editor"),
                L.S("mcm_section_doctrines", "Doctrines"),
                L.S("mcm_section_fluff", "Immersion & Fluff"),
                L.S("mcm_section_retinues", "Retinues"),
                L.S("mcm_section_recruitment", "Recruitment"),
                L.S("mcm_section_equipment", "Equipment"),
                L.S("mcm_section_unlocks", "Unlocks"),
                L.S("mcm_section_skills", "Skills"),
                L.S("mcm_section_restrictions", "Restrictions"),
                L.S("mcm_section_skill_caps", "Skill Caps"),
                L.S("mcm_section_skill_totals", "Skill Totals"),
                L.S("mcm_section_debug", "Debug"),
            };

            var index = order
                .Select((name, i) => (name, i))
                .ToDictionary(x => x.name, x => x.i, StringComparer.InvariantCulture);

            foreach (var groupBySection in _all.GroupBy(o => o.Section))
            {
                builder.CreateGroup(
                    groupBySection.Key,
                    group =>
                    {
                        group.SetGroupOrder(
                            index.TryGetValue(groupBySection.Key, out var i) ? i : int.MaxValue
                        );
                        var order = 0;
                        foreach (
                            var opt in groupBySection.OrderBy(o =>
                                _ordinalByKey.TryGetValue(o.Key, out var i) ? i : int.MaxValue
                            )
                        )
                        {
                            var id = opt.Key;
                            var name = opt.Name;

                            // select hint (disabled or regular)
                            var hint = opt.IsDisabled ? opt.DisabledHint : opt.Hint;

                            // If disabled, force the value and skip adding the control entirely
                            if (opt.IsDisabled)
                            {
                                _values[id] = opt.DisabledOverrideBoxed;
                                // Do not add any UI control for this option.
                                continue;
                            }

                            switch (Type.GetTypeCode(opt.Type))
                            {
                                case TypeCode.Boolean:
                                    group.AddBool(
                                        id,
                                        name,
                                        new ProxyRef<bool>(
                                            () => (bool)_values[id],
                                            v => _values[id] = v
                                        ),
                                        b =>
                                            b.SetOrder(order++)
                                                .SetHintText(hint)
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
                                                Convert.ToInt32(
                                                    _values[id],
                                                    CultureInfo.InvariantCulture
                                                ),
                                            v => _values[id] = v
                                        ),
                                        b =>
                                            b.SetOrder(order++)
                                                .SetHintText(hint)
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
                                                Convert.ToSingle(
                                                    _values[id],
                                                    CultureInfo.InvariantCulture
                                                ),
                                            v => _values[id] = v
                                        ),
                                        b =>
                                            b.SetOrder(order++)
                                                .SetHintText(hint)
                                                .SetRequireRestart(opt.RequiresRestart)
                                    );
                                    break;

                                case TypeCode.String:
                                    group.AddText(
                                        id,
                                        name,
                                        new ProxyRef<string>(
                                            () => (string)_values[id],
                                            v => _values[id] = v
                                        ),
                                        b =>
                                            b.SetOrder(order++)
                                                .SetHintText(hint)
                                                .SetRequireRestart(opt.RequiresRestart)
                                    );
                                    break;
                            }
                        }

                        // Append a Danger Zone action at the very end of the Debug section
                        if (groupBySection.Key.Equals("Debug", StringComparison.OrdinalIgnoreCase))
                        {
                            // Push to the very end of the group
                            var tailOrder = order + 999;

                            // Single action with explanatory hint, click-gated to in-campaign only.
                            group.AddButton(
                                "Danger_RemoveAllCustomTroopData",
                                L.S("mcm_debug_remove_all_title", "Purge Custom Troop Data"),
                                new ProxyRef<Action>(
                                    () =>
                                        () =>
                                        {
                                            if (!InCampaign())
                                            {
                                                Log.Message(
                                                    "Not in a running campaign. Load a save first."
                                                );
                                                return;
                                            }

                                            // Irreversible confirmation
                                            ConfirmTroopReplace(
                                                L.S(
                                                    "mcm_debug_remove_all_confirm_title",
                                                    "Remove all custom troop data?"
                                                ),
                                                L.S(
                                                    "mcm_debug_remove_all_confirm_body",
                                                    $"This will permanently purge all Retinues custom troops from the current world so you can safely uninstall the mod.\n\nThis operation is IRREVERSIBLE. Backup your save before proceeding."
                                                ),
                                                () =>
                                                {
                                                    try
                                                    {
                                                        // Hard sanitize everything
                                                        SanitizerBehavior.Sanitize(
                                                            replaceAllCustom: true
                                                        );
                                                        Log.Message(
                                                            "All custom troop data has been removed. Save & exit before uninstalling the mod."
                                                        );
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Log.Message(
                                                            "Sanitization failed, see log for details."
                                                        );
                                                        Log.Exception(e);
                                                    }
                                                }
                                            );
                                        },
                                    _ => { }
                                ),
                                L.S("mcm_debug_remove_all_btn", "Purge"),
                                b =>
                                    b.SetOrder(tailOrder)
                                        .SetRequireRestart(false)
                                        .SetHintText(
                                            L.S(
                                                "mcm_debug_remove_all_hint",
                                                "Will purge of all Retinues custom troop data in the current save so the mod can be uninstalled safely."
                                            )
                                        )
                            );
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

            // Build value maps
            var defaults = _all.ToDictionary(o => o.Key, o => o.Default);
            var freeform = _all.ToDictionary(
                o => o.Key,
                o => o.PresetOverrides.TryGetValue(Presets.Freeform, out var v) ? v : o.Default
            );
            var realistic = _all.ToDictionary(
                o => o.Key,
                o => o.PresetOverrides.TryGetValue(Presets.Realistic, out var v) ? v : o.Default
            );

            // Add the presets to the page
            builder.CreatePreset(Presets.Freeform, "Freeform", p => ApplyPreset(p, freeform));
            builder.CreatePreset(Presets.Realistic, "Realistic", p => ApplyPreset(p, realistic));

            // Presets (unchanged) …
            var settings = builder.BuildAsGlobal();

            // 2) Helpful stats
            var countsBySection = _all.GroupBy(x => x.Section)
                .ToDictionary(g => g.Key, g => g.Count());

            if (settings is null)
                return false;

            settings.Register();

            return true;
        }

        // Import/Export support (self-contained)
        private static string _exportName = SuggestDefaultExportName();

        /// <summary>
        /// Return whether a campaign is currently running.
        /// </summary>
        private static bool InCampaign()
        {
            try
            {
                return TaleWorlds.CampaignSystem.Campaign.Current != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Suggest a default file name for exports based on the current timestamp.
        /// </summary>
        private static string SuggestDefaultExportName()
        {
            var ts = DateTime.Now.ToString("yyyy_MM_dd_HH_mm");
            return $"troops_{ts}.xml";
        }

        /// <summary>
        /// Show a confirmation inquiry before replacing troop definitions, then invoke onConfirm.
        /// </summary>
        private static void ConfirmTroopReplace(string title, string body, Action onConfirm)
        {
            var inquiry = new InquiryData(
                title,
                body,
                true,
                true,
                L.S("continue", "Continue"),
                L.S("cancel", "Cancel"),
                onConfirm,
                () => { }
            );
            InformationManager.ShowInquiry(inquiry);
        }
    }
}
