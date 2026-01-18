using System.Collections.Generic;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Editor.Shared.Controllers;
using Retinues.GUI.Services;
using TaleWorlds.Core;

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
    }
}
