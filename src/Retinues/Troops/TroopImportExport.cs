using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Helpers;
using Retinues.Safety.Legacy;
using Retinues.Troops.Save;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.ObjectSystem;

namespace Retinues.Troops
{
    /// <summary>
    /// Unified import/export (single root) + validated pickers + context-aware flows.
    /// </summary>
    [SafeClass]
    public static class TroopImportExport
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Constants                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly string DefaultDir = Path.Combine(
            ModuleHelper.GetModuleFullPath("Retinues"),
            "Exports"
        );

        private const string RootUnified = "RetinuesTroops";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          DTOs                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public struct FactionExportData
        {
            public FactionSaveData clanData;
            public FactionSaveData kingdomData;

            public readonly bool HasAny => clanData != null || kingdomData != null;
        }

        /// <summary>
        /// Unified package (single root). Either section can be null/empty.
        /// </summary>
        [XmlRoot(RootUnified)]
        public class RetinuesTroopsPackage
        {
            public FactionExportData Factions { get; set; }
            public List<FactionSaveData> Cultures { get; set; } = [];

            public bool HasFactions => Factions.HasAny;
            public bool HasCultures => Cultures != null && Cultures.Count > 0;
        }

        public enum ImportScope
        {
            CustomOnly,
            CulturesOnly,
            Both,
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Utilities                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void EnsureDir() =>
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(DefaultDir, "x")) ?? ".");

        public static string SuggestTimestampName(string prefix) =>
            $"{prefix}_{DateTime.Now:yyyy_MM_dd_HH_mm}.xml";

        private static string NormalizePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return Path.Combine(DefaultDir, "troops.xml");

            var name = fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                ? fileName
                : fileName + ".xml";

            return Path.Combine(DefaultDir, name);
        }

        /// <summary>
        /// Does this xml file have the expected unified root?
        /// </summary>
        private static bool IsUnifiedExport(string absPath)
        {
            try
            {
                using var fs = File.OpenRead(absPath);
                using var xr = XmlReader.Create(
                    fs,
                    new XmlReaderSettings { IgnoreComments = true }
                );
                while (xr.Read())
                {
                    if (xr.NodeType == XmlNodeType.Element)
                        return xr.Name == RootUnified;
                }
            }
            catch
            {
                // ignore; treated as invalid
            }
            return false;
        }

        public static List<string> ListValidUnifiedFilesNewestFirst()
        {
            EnsureDir();
            return
            [
                .. Directory
                    .EnumerateFiles(DefaultDir, "*.xml", SearchOption.TopDirectoryOnly)
                    .Where(p => IsUnifiedExport(p) || LegacyTroopImporter.IsLegacyExport(p))
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .Select(Path.GetFileName),
            ];
        }

        private static bool TryResolveExistingPath(string fileName, out string absPath)
        {
            absPath = null!;

            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            // If caller passed an absolute/relative path that exists, trust it
            if (File.Exists(fileName))
            {
                absPath = Path.GetFullPath(fileName);
                return true;
            }

            // Try inside our DefaultDir
            var p = Path.Combine(DefaultDir, fileName);
            if (File.Exists(p))
            {
                absPath = p;
                return true;
            }

            // Try adding .xml
            if (!fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                p = Path.Combine(DefaultDir, fileName + ".xml");
                if (File.Exists(p))
                {
                    absPath = p;
                    return true;
                }
            }

            return false;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                           XML                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void SerializeUnifiedToFile(RetinuesTroopsPackage payload, string absPath)
        {
            var serializer = new XmlSerializer(typeof(RetinuesTroopsPackage));
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            };

            EnsureDir();
            using var fs = File.Create(absPath);
            using var writer = XmlWriter.Create(fs, settings);
            serializer.Serialize(writer, payload);
        }

        private static RetinuesTroopsPackage DeserializeUnifiedFromFile(string absPath)
        {
            var serializer = new XmlSerializer(typeof(RetinuesTroopsPackage));
            using var fs = File.OpenRead(absPath);
            return (RetinuesTroopsPackage)serializer.Deserialize(fs);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Export                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static string ExportUnified(
            string fileName,
            bool includeCustom,
            bool includeCultures
        )
        {
            if (!includeCustom && !includeCultures)
                throw new InvalidOperationException("Nothing selected to export.");

            var filePath = NormalizePath(fileName);

            var pkg = new RetinuesTroopsPackage();

            if (includeCustom)
            {
                pkg.Factions = new FactionExportData
                {
                    clanData = new FactionSaveData(Player.Clan),
                    kingdomData = new FactionSaveData(Player.Kingdom),
                };
            }

            if (includeCultures)
            {
                var cultures =
                    MBObjectManager
                        .Instance.GetObjectTypeList<CultureObject>()
                        ?.OrderBy(c => c?.Name?.ToString())
                        .ToList()
                    ?? [];

                foreach (var culture in cultures)
                    pkg.Cultures.Add(new FactionSaveData(new WCulture(culture)));
            }

            SerializeUnifiedToFile(pkg, filePath);
            return Path.GetFullPath(filePath);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Import                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static bool ImportUnified(string fileName, ImportScope scope)
        {
            if (!TryResolveExistingPath(fileName, out var path))
                return false; // file missing -> caller shows generic error

            RetinuesTroopsPackage pkg;

            if (LegacyTroopImporter.IsLegacyExport(path))
            {
                // Legacy: Troops -> TroopSaveData -> LegacyTroopSaveData -> FactionSaveData
                pkg = LegacyTroopImporter.LoadLegacyPackage(path);
                if (pkg == null)
                    throw new InvalidOperationException(
                        $"Failed to load legacy export '{fileName}'."
                    );
            }
            else
            {
                // Unified: <RetinuesTroops ...>
                pkg = DeserializeUnifiedFromFile(path);
            }

            if (pkg == null)
                throw new InvalidOperationException(
                    $"File not found or invalid format: '{fileName}'."
                );

            // Apply per scope (unchanged)
            if (scope is ImportScope.CustomOnly or ImportScope.Both)
            {
                if (pkg.HasFactions)
                {
                    pkg.Factions.clanData?.Apply(Player.Clan);
                    pkg.Factions.kingdomData?.Apply(Player.Kingdom);
                }
            }

            if (scope is ImportScope.CulturesOnly or ImportScope.Both)
            {
                if (pkg.HasCultures)
                {
                    foreach (var f in pkg.Cultures)
                        f.Apply();
                }
            }

            return true;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Picker                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Shows a validated file picker listing only RetinuesTroops files and invokes onChoice(name).
        /// </summary>
        public static void ShowUnifiedPicker(
            string title,
            string body,
            string confirmText,
            Action<string> onChoice
        )
        {
            EnsureDir();
            var files = ListValidUnifiedFilesNewestFirst();
            if (files.Count == 0)
            {
                Notifications.Popup(
                    L.T("no_exports_title", "No Exports Found"),
                    L.T(
                        "no_exports_body",
                        "No valid export files were found in the Exports folder."
                    )
                );
                return;
            }

            var elements = files.Select(f => new InquiryElement(f, f, null)).ToList();

            var inquiry = new MultiSelectionInquiryData(
                title,
                body,
                elements,
                isExitShown: true,
                minSelectableOptionCount: 1,
                maxSelectableOptionCount: 1,
                affirmativeText: confirmText,
                negativeText: L.S("cancel", "Cancel"),
                affirmativeAction: sel =>
                {
                    var name = sel?.FirstOrDefault()?.Identifier as string;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        Notifications.Popup(
                            L.T("no_selection_title", "No Selection"),
                            L.T("no_selection_body", "No file was selected.")
                        );
                        return;
                    }
                    onChoice(name);
                },
                negativeAction: _ => { }
            );

            MBInformationManager.ShowMultiSelectionInquiry(inquiry);
        }

        /// <summary>
        /// Export flow prompting scope based on context (true = MCM, false = Editor Studio),
        /// then prompt for file name, then export and show a popup.
        /// </summary>
        public static void PromptAndExport(string suggestedName = null)
        {
            // Unified multiselect: Player Troops (custom clan+kingdom) and Culture Troops
            var elements = new List<InquiryElement>
            {
                new("custom", L.S("exp_player_troops", "Player Troops"), null, true, null),
                new("cultures", L.S("exp_culture_troops", "Culture Troops"), null, true, null),
            };

            MBInformationManager.ShowMultiSelectionInquiry(
                new MultiSelectionInquiryData(
                    L.S("export_which_title", "Export Selection"),
                    L.S("export_which_body", "Select one or both."),
                    elements,
                    isExitShown: true,
                    minSelectableOptionCount: 1,
                    maxSelectableOptionCount: 2,
                    L.S("continue", "Continue"),
                    L.S("cancel", "Cancel"),
                    selected =>
                    {
                        var ids = selected?.Select(e => e.Identifier as string).ToHashSet() ?? [];
                        var includeCustom = ids.Contains("custom");
                        var includeCultures = ids.Contains("cultures");

                        InformationManager.ShowTextInquiry(
                            new TextInquiryData(
                                L.S("enter_file_name", "Enter a file name:"),
                                string.Empty,
                                true, // affirmative
                                true, // negative
                                L.S("confirm", "Confirm"),
                                L.S("cancel", "Cancel"),
                                name =>
                                {
                                    try
                                    {
                                        EnsureDir();
                                        var file = string.IsNullOrWhiteSpace(name)
                                            ? SuggestTimestampName("troops")
                                            : name.Trim();
                                        var used = ExportUnified(
                                            file,
                                            includeCustom,
                                            includeCultures
                                        );
                                        Notifications.Popup(
                                            L.T("export_done_title", "Export Completed"),
                                            L.T("export_done_body", "Exported to: {PATH}.")
                                                .SetTextVariable("PATH", used)
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
                                () => { },
                                defaultInputText: suggestedName ?? SuggestTimestampName("troops")
                            )
                        );
                    },
                    _ => { }
                )
            );
        }

        /// <summary>
        /// Import flow: validated picker -> if both sections, ask scope according to context -> confirm -> import -> popup.
        /// </summary>
        public static void PickAndImportUnified(Action afterImport = null)
        {
            ShowUnifiedPicker(
                L.S("import_pick_title", "Import Troops"),
                L.S("import_pick_body", "Select an exported file to import."),
                L.S("import", "Import"),
                choice =>
                {
                    // Peek the package to know what's inside (both? one?)
                    RetinuesTroopsPackage pkg;
                    try
                    {
                        var abs = Path.Combine(DefaultDir, choice);

                        if (LegacyTroopImporter.IsLegacyExport(abs))
                            pkg = LegacyTroopImporter.LoadLegacyPackage(abs);
                        else
                            pkg = DeserializeUnifiedFromFile(abs);
                    }
                    catch (Exception e)
                    {
                        Notifications.Popup(
                            L.T("import_fail_title", "Import Failed"),
                            L.T("import_fail_body", e.Message)
                        );
                        return;
                    }

                    if (!pkg.HasFactions && !pkg.HasCultures)
                    {
                        Notifications.Popup(
                            L.T("import_empty_title", "Nothing To Import"),
                            L.T("import_empty_body", "The file contains no troops.")
                        );
                        return;
                    }

                    // If both present, ask scope based on context
                    void ConfirmAndImport(ImportScope scope)
                    {
                        var confirm = new InquiryData(
                            L.S("confirm_import_title", "Apply Import?"),
                            L.S(
                                "confirm_import_body",
                                "This will replace existing troop definitions."
                            ),
                            true,
                            true,
                            L.S("continue", "Continue"),
                            L.S("cancel", "Cancel"),
                            () =>
                            {
                                try
                                {
                                    bool success = ImportUnified(choice, scope);
                                    if (!success)
                                        throw new Exception();

                                    Notifications.Popup(
                                        L.T("import_done_title", "Import Completed"),
                                        L.T(
                                                "import_done_body",
                                                "File {FILE} imported successfully."
                                            )
                                            .SetTextVariable("FILE", choice)
                                    );
                                    afterImport?.Invoke();
                                }
                                catch (Exception e)
                                {
                                    Notifications.Popup(
                                        L.T("import_fail_title", "Import Failed"),
                                        L.T("import_fail_body", e.Message)
                                    );
                                }
                            },
                            () => { }
                        );
                        InformationManager.ShowInquiry(confirm);
                    }

                    if (pkg.HasFactions && pkg.HasCultures)
                    {
                        var els = new List<InquiryElement>
                        {
                            new(
                                "custom",
                                L.S("imp_player_troops", "Player Troops"),
                                null,
                                true,
                                null
                            ),
                            new(
                                "cultures",
                                L.S("imp_culture_troops", "Culture Troops"),
                                null,
                                true,
                                null
                            ),
                        };

                        MBInformationManager.ShowMultiSelectionInquiry(
                            new MultiSelectionInquiryData(
                                L.S("import_which_title", "Import Selection"),
                                L.S("import_which_body", "Select one or both."),
                                els,
                                isExitShown: true,
                                minSelectableOptionCount: 1,
                                maxSelectableOptionCount: 2,
                                L.S("continue", "Continue"),
                                L.S("cancel", "Cancel"),
                                picked =>
                                {
                                    var ids =
                                        picked?.Select(e => e.Identifier as string).ToHashSet()
                                        ?? [];
                                    ImportScope scope =
                                        ids.Contains("custom") && ids.Contains("cultures")
                                            ? ImportScope.Both
                                        : ids.Contains("custom") ? ImportScope.CustomOnly
                                        : ImportScope.CulturesOnly;

                                    ConfirmAndImport(scope);
                                },
                                _ => { }
                            )
                        );
                    }
                    else
                    {
                        // Only one section present -> determine scope automatically
                        var scope = pkg.HasFactions
                            ? ImportScope.CustomOnly
                            : ImportScope.CulturesOnly;
                        ConfirmAndImport(scope);
                    }
                }
            );
        }
    }
}
