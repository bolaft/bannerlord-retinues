using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Exports;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Editor.Modules.Pages.Library.Services;
using Retinues.GUI.Editor.Shared.Controllers;
using Retinues.GUI.Services;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.GUI.Editor.Modules.Common.TopPanel.Controllers
{
    /// <summary>
    /// Controller for faction and banner selection, export and related UI actions.
    /// </summary>
    public class FactionController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Banners                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Opens the left-banner selection popup if available for the current mode/context.
        /// </summary>
        public static ControllerAction<bool> SelectLeftBannerPopup { get; } =
            Action<bool>("SelectLeftBannerPopup")
                .AddCondition(
                    _ => CanSelectLeftBanner(),
                    L.T("editor_left_banner_unavailable", "This selection is not available.")
                )
                .ExecuteWith(_ => ExecuteSelectLeftBanner());

        /// <summary>
        /// Opens the right-banner selection popup if available for the current mode/context.
        /// </summary>
        public static ControllerAction<bool> SelectRightBannerPopup { get; } =
            Action<bool>("SelectRightBannerPopup")
                .AddCondition(
                    _ => CanSelectRightBanner(),
                    L.T("editor_right_banner_unavailable", "This selection is not available.")
                )
                .ExecuteWith(_ => ExecuteSelectRightBanner());

        /// <summary>
        /// Returns whether the left banner may be selected in the current mode/context.
        /// </summary>
        private static bool CanSelectLeftBanner()
        {
            if (State.Mode == EditorMode.Universal)
                return WCulture.All != null;

            // Player: left banner is only selectable if the player is a kingdom ruler.
            return Player.IsRuler;
        }

        /// <summary>
        /// Returns whether the right banner may be selected in the current mode/context.
        /// </summary>
        private static bool CanSelectRightBanner()
        {
            if (State.Mode == EditorMode.Universal)
                return WClan.All != null;

            // Player: right banner exists only for kingdom rulers (click sets faction to kingdom).
            return Player.IsRuler;
        }

        /// <summary>
        /// Execute the left-banner selection action (culture picker or player kingdom clan picker).
        /// </summary>
        private static void ExecuteSelectLeftBanner()
        {
            if (State.Mode == EditorMode.Universal)
            {
                ShowCulturePopup();
                return;
            }

            // Player: select a clan from the player's kingdom.
            ShowPlayerKingdomClanPopup();
        }

        /// <summary>
        /// Execute the right-banner selection action (clan picker in universal mode or apply player's kingdom).
        /// </summary>
        private static void ExecuteSelectRightBanner()
        {
            if (State.Mode == EditorMode.Universal)
            {
                ShowClanPopupUniversal();
                return;
            }

            // Player: click sets faction to the player's kingdom.
            if (!Player.IsRuler)
                return;

            ApplyKingdom(Player.Kingdom);
        }

        /// <summary>
        /// Show a popup allowing the user to select a culture.
        /// </summary>
        private static void ShowCulturePopup()
        {
            var elements = new List<InquiryElement>();

            foreach (var culture in WCulture.All)
            {
                var imageIdentifier = culture?.ImageIdentifier;
                var name = culture?.Name;

                if (imageIdentifier == null || name == null)
                    continue;

                elements.Add(
                    new InquiryElement(
                        identifier: culture,
                        title: name,
                        imageIdentifier: imageIdentifier
                    )
                );
            }

            if (elements.Count == 0)
            {
                Inquiries.Popup(
                    L.T("no_cultures_title", "No Cultures Found"),
                    L.T("no_cultures_text", "No cultures are loaded in the current game.")
                );
                return;
            }

            Inquiries.SelectPopup(
                title: L.T("select_culture_title", "Select Culture"),
                elements: elements,
                onSelect: element => ApplyCultureUniversal(element?.Identifier as WCulture)
            );
        }

        /// <summary>
        /// Apply the selected culture in universal mode and update editor state.
        /// </summary>
        private static void ApplyCultureUniversal(WCulture culture)
        {
            if (culture == null)
                return;

            // Always clear the right banner when selecting a culture.
            State.RightBannerFaction = null;
            State.LeftBannerFaction = culture;
            State.Faction = culture;

            EventManager.Fire(UIEvent.Faction);
        }

        /// <summary>
        /// Show a popup allowing the user to select a clan (universal mode).
        /// </summary>
        private static void ShowClanPopupUniversal()
        {
            var elements = new List<InquiryElement>();

            var culture = State.LeftBannerFaction as WCulture;

            foreach (var clan in WClan.All)
            {
                if (culture != null && clan.Culture != culture)
                    continue;

                var imageIdentifier = clan?.ImageIdentifier;
                var name = clan?.Name;

                if (imageIdentifier == null || name == null)
                    continue;

                elements.Add(
                    new InquiryElement(
                        identifier: clan,
                        title: name,
                        imageIdentifier: imageIdentifier
                    )
                );
            }

            if (elements.Count == 0)
            {
                Inquiries.Popup(
                    L.T("no_clans_title", "No Clans Found"),
                    L.T("no_clans_text", "No clans are loaded in the current game.")
                );
                return;
            }

            Inquiries.SelectPopup(
                title: L.T("select_clan_title", "Select Clan"),
                elements: elements,
                onSelect: element => ApplyClanUniversal(element?.Identifier as WClan)
            );
        }

        /// <summary>
        /// Apply the selected clan in universal mode and update editor state.
        /// </summary>
        private static void ApplyClanUniversal(WClan clan)
        {
            if (clan == null)
                return;

            // Selecting a clan also updates the left culture banner for filtering.
            State.LeftBannerFaction = clan.Culture;
            State.RightBannerFaction = clan;
            State.Faction = clan;

            EventManager.Fire(UIEvent.Faction);
        }

        /// <summary>
        /// Show a popup to select a clan from the player's kingdom.
        /// </summary>
        private static void ShowPlayerKingdomClanPopup()
        {
            if (!Player.IsRuler)
                return;

            var elements = new List<InquiryElement>();

            foreach (var clan in WClan.All)
            {
                if (Player.Kingdom != null && clan.Base.Kingdom != Player.Kingdom.Base)
                    continue;

                var imageIdentifier = clan?.ImageIdentifier;
                var name = clan?.Name;

                if (imageIdentifier == null || name == null)
                    continue;

                elements.Add(
                    new InquiryElement(
                        identifier: clan,
                        title: name,
                        imageIdentifier: imageIdentifier
                    )
                );
            }

            if (elements.Count == 0)
            {
                Inquiries.Popup(
                    L.T("no_clans_title", "No Clans Found"),
                    L.T("no_clans_text", "No clans are loaded in the current game.")
                );
                return;
            }

            Inquiries.SelectPopup(
                title: L.T("select_clan_title", "Select Clan"),
                elements: elements,
                onSelect: element =>
                    ApplyClanPlayer(element?.Identifier as WClan, Player.Hero, Player.Kingdom)
            );
        }

        /// <summary>
        /// Apply the selected clan for the player, clamped to the provided kingdom if necessary.
        /// </summary>
        private static void ApplyClanPlayer(WClan clan, WHero hero, WKingdom kingdom)
        {
            if (clan == null || hero == null || kingdom == null)
                return;

            // Safety: clamp to kingdom.
            if (clan.Base.Kingdom != kingdom.Base)
                clan = hero.Clan;

            State.LeftBannerFaction = clan;
            State.RightBannerFaction = kingdom;
            State.Faction = clan;

            EventManager.Fire(UIEvent.Faction);
        }

        /// <summary>
        /// Apply the selected kingdom as the right banner and update editor state.
        /// </summary>
        private static void ApplyKingdom(WKingdom kingdom)
        {
            if (kingdom == null)
                return;

            State.RightBannerFaction = kingdom;
            State.Faction = kingdom;

            EventManager.Fire(UIEvent.Faction);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Export Faction                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Exports the currently selected faction to the library when possible.
        /// </summary>
        public static ControllerAction<bool> ExportFaction { get; } =
            Action<bool>("ExportFaction")
                .DefaultTooltip(
                    L.T(
                        "button_export_faction_tooltip",
                        "Save the selected faction and add it to the library."
                    )
                )
                .AddCondition(
                    _ => State.Faction != null,
                    L.T("export_faction_none", "No faction selected.")
                )
                .AddCondition(
                    _ => !string.IsNullOrWhiteSpace(State.Faction.StringId),
                    L.T("export_faction_no_id", "Selected faction is invalid.")
                )
                .ExecuteWith(_ => ExportFactionImpl());

        /// <summary>
        /// Performs the export flow: builds the export document and writes it to disk.
        /// </summary>
        private static void ExportFactionImpl()
        {
            var faction = State.Faction;

            if (faction == null)
                return;

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

        /// <summary>
        /// Builds a safe file path for the export name and returns validation errors if any.
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

            // Ensure extension (user can type it or not).
            if (!safe.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                safe += ".xml";

            safeFileName = safe;

            var dir = ExportLibrary.ExportDirectory;
            path = Path.Combine(dir, safeFileName);

            return true;
        }
    }
}
