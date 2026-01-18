using System;
using System.IO;
using System.Text;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Exports;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Editor.Modules.Common.TopPanel.Helpers;
using Retinues.GUI.Editor.Modules.Pages.Library.Services;
using Retinues.GUI.Editor.Shared.Controllers;
using Retinues.GUI.Services;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.GUI.Editor.Modules.Common.TopPanel.Controllers
{
    /// <summary>
    /// Controller for exporting either the selected faction or any troop of that faction,
    /// based on a user choice popup.
    /// </summary>
    public class ExportController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Export                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Controller action to start the export flow (choose faction or troop).
        /// </summary>
        public static ControllerAction<bool> Export { get; } =
            Action<bool>("Export")
                .DefaultTooltip(
                    L.T(
                        "button_export_tooltip",
                        "Export the selected faction or troop to the library."
                    )
                )
                .AddCondition(
                    _ => CanExportFaction() || CanExportAnyTroop(),
                    L.T("export_none_available", "Nothing can be exported right now.")
                )
                .ExecuteWith(_ => ShowExportTargetPopup());

        /// <summary>
        /// Returns true when the current state has a faction that can be exported.
        /// </summary>
        private static bool CanExportFaction()
        {
            if (State.Faction == null)
                return false;

            return !string.IsNullOrWhiteSpace(State.Faction.StringId);
        }

        /// <summary>
        /// Returns true when the given troop can be exported.
        /// </summary>
        private static bool CanExportTroop(WCharacter c)
        {
            if (c == null)
                return false;

            if (c.IsHero)
                return false;

            return true;
        }

        /// <summary>
        /// Returns true when any troop in the current faction can be exported.
        /// </summary>
        private static bool CanExportAnyTroop()
        {
            var faction = State.Faction;
            if (faction == null)
                return false;

            foreach (var wc in faction.Troops)
            {
                if (CanExportTroop(wc))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Show a popup prompting the user to choose export target (faction or troop).
        /// </summary>
        private static void ShowExportTargetPopup()
        {
            var faction = State.Faction;
            if (faction == null)
                return;

            var elements = TargetsHelper.BuildFactionAndTroopsElements(
                faction: faction,
                isFactionEnabled: _ => CanExportFaction(),
                factionDisabledHint: L.S(
                    "export_target_faction_disabled",
                    "No valid faction selected."
                ),
                isTroopEnabled: CanExportTroop,
                troopDisabledHint: _ =>
                    L.S("export_target_troop_disabled", "Heroes cannot be exported.")
            );

            Inquiries.SelectPopup(
                title: L.T("export_choose_target_title", "Export"),
                description: L.T("export_choose_target_body", "Export which of the following?"),
                elements: elements,
                onSelect: element =>
                {
                    if (element?.Identifier is not Target target)
                        return;

                    switch (target.Kind)
                    {
                        case TargetKind.Faction:
                            ExportFactionImpl();
                            break;
                        case TargetKind.Troop:
                            ExportCharacterImpl(target.Troop);
                            break;
                    }
                }
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Export Faction                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Build and write the selected faction export file, prompting for filename.
        /// </summary>
        private static void ExportFactionImpl()
        {
            var faction = State.Faction;
            if (faction == null)
                return;

            if (!CanExportFaction())
            {
                Inquiries.Popup(
                    title: L.T("export_failed_title", "Export Failed"),
                    description: L.T("export_faction_invalid", "Selected faction is invalid.")
                );
                return;
            }

            if (!FactionExporter.TryBuildExport(faction, out var doc, out var err))
            {
                Inquiries.Popup(
                    title: L.T("export_failed_title", "Export Failed"),
                    description: L.T("export_failed_generic", "Could not export: {ERROR}.")
                        .SetTextVariable("ERROR", err ?? L.S("unknown_error", "Unknown error."))
                );

                return;
            }

            var defaultName = faction.Name;
            if (string.IsNullOrWhiteSpace(defaultName))
                defaultName = faction.StringId;

            Inquiries.TextInputPopup(
                title: L.T("export_name_title", "Export Name"),
                description: L.T("export_name_desc", "Choose a file name for this export:"),
                defaultInput: defaultName,
                onConfirm: input =>
                {
                    input = (input ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        Inquiries.Popup(
                            L.T("invalid_name_title", "Invalid Name"),
                            L.T("invalid_name_body", "The name cannot be empty.")
                        );
                        return;
                    }

                    if (
                        !TryBuildExportPath(
                            input,
                            out var path,
                            out var safeFileName,
                            out var error
                        )
                    )
                    {
                        Inquiries.Popup(
                            L.T("invalid_name_title", "Invalid Name"),
                            error ?? L.T("invalid_name_body", "The name cannot be empty.")
                        );
                        return;
                    }

                    void Write()
                    {
                        try
                        {
                            var dir = Path.GetDirectoryName(path);
                            if (!string.IsNullOrWhiteSpace(dir))
                                Directory.CreateDirectory(dir);

                            File.WriteAllText(path, doc.ToString(), new UTF8Encoding(false));

                            Inquiries.Popup(
                                title: L.T("export_done_title", "Export Complete"),
                                description: L.T("export_done_desc", "Export written to:\n{PATH}")
                                    .SetTextVariable("PATH", path)
                            );

                            EventManager.Fire(UIEvent.Library);
                        }
                        catch (Exception ex)
                        {
                            Log.Exception(ex);

                            Inquiries.Popup(
                                title: L.T("export_failed_title", "Export Failed"),
                                description: L.T(
                                    "export_failed_exception",
                                    "The file could not be written."
                                )
                            );
                        }
                    }

                    if (File.Exists(path))
                    {
                        Inquiries.Popup(
                            title: L.T("export_overwrite_title", "File Already Exists"),
                            description: L.T(
                                    "export_overwrite_desc",
                                    "A file named '{NAME}' already exists.\n\nDo you want to overwrite it?"
                                )
                                .SetTextVariable("NAME", safeFileName),
                            confirmText: L.T("overwrite_confirm", "Overwrite"),
                            cancelText: GameTexts.FindText("str_cancel"),
                            onConfirm: Write
                        );

                        return;
                    }

                    Write();
                }
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Export Character                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Build and write the selected character export file, prompting for filename.
        /// </summary>
        private static void ExportCharacterImpl(WCharacter c)
        {
            if (c == null)
                return;

            if (!CanExportTroop(c))
            {
                Inquiries.Popup(
                    title: L.T("export_failed_title", "Export Failed"),
                    description: L.T(
                        "export_character_no_heroes",
                        "Hero data is tied to the save state."
                    )
                );
                return;
            }

            if (!CharacterExporter.TryBuildExport(c, out var doc, out var err))
            {
                Inquiries.Popup(
                    title: L.T("export_failed_title", "Export Failed"),
                    description: L.T("export_failed_generic", "Could not export: {ERROR}.")
                        .SetTextVariable("ERROR", err ?? L.S("unknown_error", "Unknown error."))
                );

                return;
            }

            var defaultName = c.Name;
            if (string.IsNullOrWhiteSpace(defaultName))
                defaultName = c.StringId;

            Inquiries.TextInputPopup(
                title: L.T("export_name_title", "Export Name"),
                description: L.T("export_name_desc", "Choose a file name for this export:"),
                defaultInput: defaultName,
                onConfirm: input =>
                {
                    input = (input ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        Inquiries.Popup(
                            L.T("invalid_name_title", "Invalid Name"),
                            L.T("invalid_name_body", "The name cannot be empty.")
                        );
                        return;
                    }

                    if (
                        !TryBuildExportPath(
                            input,
                            out var path,
                            out var safeFileName,
                            out var error
                        )
                    )
                    {
                        Inquiries.Popup(
                            L.T("invalid_name_title", "Invalid Name"),
                            error ?? L.T("invalid_name_body", "The name cannot be empty.")
                        );
                        return;
                    }

                    void Write()
                    {
                        try
                        {
                            var dir = Path.GetDirectoryName(path);
                            if (!string.IsNullOrWhiteSpace(dir))
                                Directory.CreateDirectory(dir);

                            File.WriteAllText(path, doc.ToString(), new UTF8Encoding(false));

                            Inquiries.Popup(
                                title: L.T("export_done_title", "Export Complete"),
                                description: L.T("export_done_desc", "Export written to:\n{PATH}")
                                    .SetTextVariable("PATH", path)
                            );

                            EventManager.Fire(UIEvent.Library);
                        }
                        catch (Exception ex)
                        {
                            Log.Exception(ex);

                            Inquiries.Popup(
                                title: L.T("export_failed_title", "Export Failed"),
                                description: L.T(
                                    "export_failed_exception",
                                    "The file could not be written."
                                )
                            );
                        }
                    }

                    if (File.Exists(path))
                    {
                        Inquiries.Popup(
                            title: L.T("export_overwrite_title", "File Already Exists"),
                            description: L.T(
                                    "export_overwrite_desc",
                                    "A file named '{NAME}' already exists.\n\nDo you want to overwrite it?"
                                )
                                .SetTextVariable("NAME", safeFileName),
                            confirmText: L.T("overwrite_confirm", "Overwrite"),
                            cancelText: GameTexts.FindText("str_cancel"),
                            onConfirm: Write
                        );

                        return;
                    }

                    Write();
                }
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Utilities                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Construct a safe export path and filename from user input; returns false with an error on failure.
        /// </summary>
        private static bool TryBuildExportPath(
            string inputFileName,
            out string path,
            out string safeFileName,
            out TextObject error
        )
        {
            path = null;
            safeFileName = null;
            error = null;

            var raw = (inputFileName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                error = L.T("invalid_name_body", "The name cannot be empty.");
                return false;
            }

            var safe = FileSystem.SanitizeFileName(raw, string.Empty);
            safe = (safe ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(safe))
            {
                error = L.T(
                    "invalid_name_sanitize",
                    "This name contains no valid file characters."
                );
                return false;
            }

            if (!safe.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                safe += ".xml";

            safeFileName = safe;

            var dir = ExportLibrary.ExportDirectory;
            path = Path.Combine(dir, safeFileName);

            return true;
        }
    }
}
