using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using MCM.Abstractions.FluentBuilder;
using MCM.Common;
using Retinues.Troops;
using Retinues.Utils;
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

        // Import dropdown instance
        private static readonly Dropdown<string> _importDropdown = new(new List<string>(), 0);

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
                // (We'll try again next tick from SubModule)
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
            () => (T)Convert.ChangeType(_values[key], typeof(T), CultureInfo.InvariantCulture);

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

                    // refresh once before showing
                    RefreshImportDropdown();

                    // File name for export (editable)
                    group.AddText(
                        "ExportFileName",
                        L.S("mcm_ie_export_name", "File name"),
                        new ProxyRef<string>(
                            () => _exportName,
                            v =>
                            {
                                _exportName = string.IsNullOrWhiteSpace(v)
                                    ? SuggestDefaultExportName()
                                    : v.Trim();
                            }
                        ),
                        b =>
                            b.SetOrder(order++)
                                .SetHintText(
                                    L.S("mcm_ie_export_hint", "Example: troops_myCampaignRun")
                                )
                    );

                    // Export button
                    group.AddButton(
                        "ExportButton",
                        L.S("mcm_ie_export_btn_text", "Export Troops to XML"),
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

                                    try
                                    {
                                        Directory.CreateDirectory(TroopImportExport.DefaultDir);
                                        var used = TroopImportExport.ExportAllToXml(_exportName);
                                        if (!string.IsNullOrWhiteSpace(used))
                                            Log.Message($"Exported custom troops to: {used}");
                                        else
                                            Log.Message("Export failed, invalid file.");
                                    }
                                    catch (Exception e)
                                    {
                                        Log.Message("Export failed, see log for details.");
                                        Log.Exception(e);
                                    }

                                    RefreshImportDropdown();
                                },
                            _ => { }
                        ),
                        L.S("mcm_ie_export_btn", "Export"),
                        b => b.SetOrder(order++).SetRequireRestart(false)
                    );

                    // Import dropdown (files found in ExportsDir)
                    group.AddDropdown(
                        "ImportFileDropdown",
                        L.S("mcm_ie_import_dropdown", "Available files"),
                        0,
                        new ProxyRef<Dropdown<string>>(
                            () => _importDropdown, // always the same object
                            _ => { } // ignore setter; we manage it
                        ),
                        b => b.SetOrder(order++)
                    );

                    // Import button (asks for confirmation)
                    group.AddButton(
                        "ImportButton",
                        L.S("mcm_ie_import_btn_text", "Import Troops from XML"),
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

                                    var choice = _importDropdown?.SelectedValue;
                                    if (string.IsNullOrWhiteSpace(choice) || choice.StartsWith("<"))
                                    {
                                        Log.Message("No valid file selected for import.");
                                        return;
                                    }

                                    ConfirmTroopReplace(
                                        L.S("mcm_ie_import_confirm_title", "Import Troops"),
                                        L.S(
                                            "mcm_ie_import_confirm_body",
                                            "Importing new troop definitions will replace the existing ones. Are you sure?"
                                        ),
                                        () =>
                                        {
                                            try
                                            {
                                                // optional safety backup
                                                Directory.CreateDirectory(
                                                    TroopImportExport.DefaultDir
                                                );
                                                TroopImportExport.ExportAllToXml(
                                                    "backup_" + SuggestDefaultExportName()
                                                );

                                                // import
                                                var count = TroopImportExport.ImportFromXml(choice);
                                                Log.Message(
                                                    count > 0
                                                        ? $"Imported {count} root troop definitions from '{choice}'."
                                                        : $"No troops were imported from '{choice}'."
                                                );
                                            }
                                            catch (Exception e)
                                            {
                                                Log.Exception(e);
                                                Log.Message("Import failed, see log for details.");
                                            }
                                            RefreshImportDropdown();
                                        }
                                    );
                                },
                            _ => { }
                        ),
                        L.S("mcm_ie_import_btn", "Import"),
                        b => b.SetOrder(order++).SetRequireRestart(false)
                    );
                }
            );

            string[] order =
            {
                "Doctrines",
                "Restrictions",
                "Retinues",
                "Recruitment",
                "Equipment",
                "Unlocks",
                "Skills",
                "Skill Caps",
                "Skill Totals",
                "Debug",
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
                            var hint = opt.Hint;

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
        /// Build a dropdown populated with available import XML files.
        /// </summary>
        private static Dropdown<string> BuildImportDropdown()
        {
            try
            {
                Directory.CreateDirectory(TroopImportExport.DefaultDir);
                var files = Directory
                    .EnumerateFiles(
                        TroopImportExport.DefaultDir,
                        "*.xml",
                        SearchOption.TopDirectoryOnly
                    )
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .ToList();
                var labels = new List<string> { };
                labels.AddRange(files.Select(Path.GetFileName));
                return new Dropdown<string>(labels, 0);
            }
            catch
            {
                return new Dropdown<string>([], 0);
            }
        }

        /// <summary>
        /// Refresh the import dropdown contents from the export directory.
        /// </summary>
        private static void RefreshImportDropdown()
        {
            try
            {
                Directory.CreateDirectory(TroopImportExport.DefaultDir);

                var labels = Directory
                    .EnumerateFiles(
                        TroopImportExport.DefaultDir,
                        "*.xml",
                        SearchOption.TopDirectoryOnly
                    )
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .Select(Path.GetFileName)
                    .ToList();

                if (labels.Count == 0)
                    labels.Add("");

                // mutate the same instance the UI is bound to
                _importDropdown.Clear();
                _importDropdown.AddRange(labels);

                // nudge a property that actually raises INotifyPropertyChanged
                _importDropdown.SelectedIndex = 0;
            }
            catch (Exception e)
            {
                Log.Exception(e);
                _importDropdown.Clear();
                _importDropdown.Add("<error>");
                _importDropdown.SelectedIndex = 0;
            }
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
