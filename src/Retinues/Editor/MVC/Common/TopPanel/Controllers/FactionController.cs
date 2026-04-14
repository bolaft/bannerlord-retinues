using System.Collections.Generic;
using Retinues.Behaviors.Troops;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Interface.Services;
using Retinues.Settings;
using TaleWorlds.Core;

namespace Retinues.Editor.MVC.Common.TopPanel.Controllers
{
    /// <summary>
    /// Controller for faction and banner selection, export and related UI actions.
    /// </summary>
    public class FactionController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Banners                        //
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
        /// If the clan has no custom troops, prompts the player to initialize them first.
        /// </summary>
        private static void ApplyClanPlayer(WClan clan, WHero hero, WKingdom kingdom)
        {
            if (clan == null || hero == null || kingdom == null)
                return;

            // Safety: clamp to kingdom.
            if (clan.Base.Kingdom != kingdom.Base)
                clan = hero.Clan;

            if (clan.RootBasic == null && clan.RootElite == null)
            {
                PromptInitializeClanTroops(clan, hero, kingdom);
                return;
            }

            ApplyClanPlayerDirect(clan, kingdom);
        }

        /// <summary>
        /// First popup: informs the player the clan has no specific troops and what it currently
        /// falls back to. Cancel exits; Create proceeds to the second prompt.
        /// </summary>
        private static void PromptInitializeClanTroops(WClan clan, WHero hero, WKingdom kingdom)
        {
            // Kingdom troops are considered available when the feature is enabled AND roots exist.
            bool kingdomTroopsEnabled =
                Configuration.KingdomTroopsUnlock.Value != Configuration.TroopsUnlockMode.Disabled;
            bool kingdomHasTroops =
                kingdomTroopsEnabled && (kingdom?.RootBasic != null || kingdom?.RootElite != null);

            var fallbackName = kingdomHasTroops ? kingdom.Name : Player.Clan?.Name ?? string.Empty;

            var descKey = kingdomHasTroops
                ? "init_clan_troops_no_troops_kingdom"
                : "init_clan_troops_no_troops_clan";
            var descFallback = kingdomHasTroops
                ? "{CLAN} has no specific custom troops. It currently uses the {FALLBACK} kingdom troops.\n\nDo you wish to create specific troop trees for the {CLAN} clan?"
                : "{CLAN} has no specific custom troops. It currently uses the {FALLBACK} clan troops.\n\nDo you wish to create specific troop trees for the {CLAN} clan?";

            var description = L.T(descKey, descFallback)
                .SetTextVariable("CLAN", clan.Name)
                .SetTextVariable("FALLBACK", fallbackName);

            Inquiries.Popup(
                title: L.T("init_clan_troops_title", "No Custom Troops"),
                description: description,
                choice1Text: L.T("init_clan_troops_create", "Create"),
                choice2Text: GameTexts.FindText("str_cancel"),
                onChoice1: () => PromptInitializeClanTroopsMethod(clan, kingdom, kingdomHasTroops),
                onChoice2: () => { }
            );
        }

        /// <summary>
        /// Second popup: asks whether to copy kingdom/clan troops or start from scratch.
        /// </summary>
        private static void PromptInitializeClanTroopsMethod(
            WClan clan,
            WKingdom kingdom,
            bool kingdomHasTroops
        )
        {
            // Source for the "copy" option: kingdom troops if available, otherwise player clan.
            IBaseFaction source = kingdomHasTroops ? kingdom : (IBaseFaction)Player.Clan;
            var sourceName = source?.Name ?? string.Empty;

            Inquiries.Popup(
                title: L.T("init_clan_troops_method_title", "Initialize Troops"),
                description: L.T(
                        "init_clan_troops_method_text",
                        "How do you want to create the troops for {CLAN}?"
                    )
                    .SetTextVariable("CLAN", clan.Name),
                choice1Text: L.T("init_clan_troops_copy_source", "Copy {SOURCE} troops")
                    .SetTextVariable("SOURCE", sourceName),
                choice2Text: L.T("init_clan_troops_new", "Create new troops"),
                onChoice1: () =>
                {
                    TroopUnlockerBehavior.InitializeClanTroopsFromSource(clan, source);
                    ApplyClanPlayerDirect(clan, kingdom);
                },
                onChoice2: () =>
                {
                    TroopUnlockerBehavior.InitializeClanTroopsFromCultureRoots(clan);
                    ApplyClanPlayerDirect(clan, kingdom);
                }
            );
        }

        /// <summary>
        /// Applies clan faction state directly without any troop checks.
        /// </summary>
        private static void ApplyClanPlayerDirect(WClan clan, WKingdom kingdom)
        {
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
        //             Delete Clan Troops (Player Mode)            //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Deletes custom troops for the current non-player clan (Player mode only).
        /// </summary>
        public static ControllerAction<bool> DeleteClanTroopsAction { get; } =
            Action<bool>("DeleteClanTroopsAction")
                .AddCondition(
                    _ => State.Mode == EditorMode.Player,
                    L.T("delete_clan_troops_player_only", "Only available in Player mode.")
                )
                .AddCondition(
                    _ => State.Faction is not WKingdom,
                    L.T("delete_clan_troops_kingdom", "Kingdom troops cannot be deleted.")
                )
                .AddCondition(
                    _ =>
                        !(
                            State.Faction is WClan fc
                            && Player.Clan != null
                            && ReferenceEquals(fc.Base, Player.Clan.Base)
                        ),
                    L.T(
                        "delete_clan_troops_player_clan",
                        "The player clan's troops cannot be deleted."
                    )
                )
                .AddCondition(
                    _ => State.Faction is WClan c && (c.RootBasic != null || c.RootElite != null),
                    L.T("delete_clan_troops_no_troops", "This clan has no custom troops.")
                )
                .ExecuteWith(_ => ShowDeleteClanTroopsPopup());

        /// <summary>
        /// Shows a confirmation popup before deleting the clan's custom troops.
        /// </summary>
        private static void ShowDeleteClanTroopsPopup()
        {
            var clan = State.Faction as WClan;
            if (clan == null)
                return;

            bool kingdomTroopsEnabled =
                Configuration.KingdomTroopsUnlock.Value != Configuration.TroopsUnlockMode.Disabled;
            var kingdom = Player.Kingdom;
            bool kingdomHasTroops =
                kingdomTroopsEnabled && (kingdom?.RootBasic != null || kingdom?.RootElite != null);

            var fallbackName = kingdomHasTroops ? kingdom.Name : Player.Clan?.Name ?? string.Empty;

            var descKey = kingdomHasTroops
                ? "delete_clan_troops_body_kingdom"
                : "delete_clan_troops_body_clan";
            var descFallback = kingdomHasTroops
                ? "Delete the custom troops for {CLAN}?\n\nThe clan will revert to using the {FALLBACK} kingdom troops.\n\nThis cannot be undone."
                : "Delete the custom troops for {CLAN}?\n\nThe clan will revert to using the {FALLBACK} clan troops.\n\nThis cannot be undone.";

            var description = L.T(descKey, descFallback)
                .SetTextVariable("CLAN", clan.Name)
                .SetTextVariable("FALLBACK", fallbackName);

            Inquiries.Popup(
                title: L.T("delete_clan_troops_title", "Clear Custom Troops"),
                description: description,
                confirmText: L.T("delete_clan_troops_confirm", "Delete"),
                cancelText: GameTexts.FindText("str_cancel"),
                onConfirm: () => ExecuteDeleteClanTroops(clan)
            );
        }

        /// <summary>
        /// Performs the deletion and switches the editor back to the player clan.
        /// </summary>
        private static void ExecuteDeleteClanTroops(WClan clan)
        {
            TroopUnlockerBehavior.DeleteClanTroops(clan);

            // Switch back to player clan after deletion.
            var playerClan = Player.Clan;
            var kingdom = Player.Kingdom;
            if (playerClan != null)
                ApplyClanPlayerDirect(playerClan, kingdom);
        }
    }
}
