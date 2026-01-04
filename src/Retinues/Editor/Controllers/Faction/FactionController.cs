using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Editor.Events;
using Retinues.Framework.Model.Exports;
using Retinues.Game;
using Retinues.UI.Services;
using TaleWorlds.Core;

namespace Retinues.Editor.Controllers.Faction
{
    public class FactionController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Banners                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<bool> SelectLeftBannerPopup { get; } =
            Action<bool>("SelectLeftBannerPopup")
                .AddCondition(
                    _ => CanSelectLeftBanner(),
                    L.T("editor_left_banner_unavailable", "This selection is not available.")
                )
                .ExecuteWith(_ => ExecuteSelectLeftBanner());

        public static EditorAction<bool> SelectRightBannerPopup { get; } =
            Action<bool>("SelectRightBannerPopup")
                .AddCondition(
                    _ => CanSelectRightBanner(),
                    L.T("editor_right_banner_unavailable", "This selection is not available.")
                )
                .ExecuteWith(_ => ExecuteSelectRightBanner());

        private static bool CanSelectLeftBanner()
        {
            if (State.Mode == EditorMode.Universal)
                return WCulture.All != null;

            // Player: left banner is only selectable if the player is a kingdom ruler.
            return Player.IsRuler;
        }

        private static bool CanSelectRightBanner()
        {
            if (State.Mode == EditorMode.Universal)
                return WClan.All != null;

            // Player: right banner exists only for kingdom rulers (click sets faction to kingdom).
            return Player.IsRuler;
        }

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Universal Left                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

        private static void ApplyCultureUniversal(WCulture culture)
        {
            if (culture == null)
                return;

            // Always clear the right banner when selecting a culture.
            State.RightBannerFaction = null;
            State.LeftBannerFaction = culture;
            State.Faction = culture;

            EventManager.FireBatch(() =>
            {
                EventManager.Fire(UIEvent.CultureFaction);
                EventManager.Fire(UIEvent.ClanFaction);
                EventManager.Fire(UIEvent.Faction);
            });
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Universal Right                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

        private static void ApplyClanUniversal(WClan clan)
        {
            if (clan == null)
                return;

            // Selecting a clan also updates the left culture banner for filtering.
            State.LeftBannerFaction = clan.Culture;
            State.RightBannerFaction = clan;
            State.Faction = clan;

            EventManager.FireBatch(() =>
            {
                EventManager.Fire(UIEvent.CultureFaction);
                EventManager.Fire(UIEvent.ClanFaction);
                EventManager.Fire(UIEvent.Faction);
            });
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Player Left                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

            EventManager.FireBatch(() =>
            {
                EventManager.Fire(UIEvent.CultureFaction);
                EventManager.Fire(UIEvent.ClanFaction);
                EventManager.Fire(UIEvent.Faction);
            });
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Player Right                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void ApplyKingdom(WKingdom kingdom)
        {
            if (kingdom == null)
                return;

            State.RightBannerFaction = kingdom;
            State.Faction = kingdom;

            EventManager.FireBatch(() =>
            {
                EventManager.Fire(UIEvent.ClanFaction);
                EventManager.Fire(UIEvent.Faction);
            });
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Export Faction                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<bool> ExportFaction { get; } =
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
                .ExecuteWith(_ =>
                {
                    MImportExport.ExportFaction(State.Faction.StringId);
                    EventManager.Fire(UIEvent.Library);
                });
    }
}
