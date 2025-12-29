using System.Collections.Generic;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Editor.Events;
using Retinues.Framework.Model.Exports;
using Retinues.UI.Services;
using TaleWorlds.Core;

namespace Retinues.Editor.Controllers.Faction
{
    public class FactionController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Select Culture                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<bool> SelectCulturePopup { get; } =
            Action<bool>("SelectCulturePopup")
                .DefaultTooltip(L.T("editor_culture_select", "Select a Culture"))
                .AddCondition(
                    _ => WCulture.All != null,
                    L.T("no_cultures_text", "No cultures are loaded in the current game.")
                )
                .ExecuteWith(_ => ShowCulturePopup());

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
                onSelect: element => ApplyCulture(element?.Identifier as WCulture)
            );
        }

        private static void ApplyCulture(WCulture culture)
        {
            if (culture == null)
                return;

            if (State.Faction is WCulture c && culture == c)
                return;

            State.Clan = null;

            if (State.Culture != culture)
                State.Culture = culture;

            State.Faction = culture;

            EventManager.Fire(UIEvent.CultureFaction);
            EventManager.Fire(UIEvent.ClanFaction);
            EventManager.Fire(UIEvent.Faction);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Select Clan                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<bool> SelectClanPopup { get; } =
            Action<bool>("SelectClanPopup")
                .DefaultTooltip(L.T("editor_clan_select", "Select a Clan"))
                .AddCondition(
                    _ => WClan.All != null,
                    L.T("no_clans_text", "No clans are loaded in the current game.")
                )
                .ExecuteWith(_ => ShowClanPopup());

        private static void ShowClanPopup()
        {
            var elements = new List<InquiryElement>();

            foreach (var clan in WClan.All)
            {
                if (State.Culture != null && clan.Culture != State.Culture)
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
                onSelect: element => ApplyClan(element?.Identifier as WClan)
            );
        }

        private static void ApplyClan(WClan clan)
        {
            if (clan == null)
                return;

            if (State.Faction is WClan c && clan == c)
                return;

            if (State.Culture != clan.Culture)
                State.Culture = clan.Culture;

            State.Clan = clan;
            State.Faction = clan;

            EventManager.Fire(UIEvent.CultureFaction);
            EventManager.Fire(UIEvent.ClanFaction);
            EventManager.Fire(UIEvent.Faction);
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
