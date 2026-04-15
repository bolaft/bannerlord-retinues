using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Helpers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Common.TopPanel.Controllers;
using Retinues.Encyclopedia.Manual;
using Retinues.Interface.Components;
using Retinues.Interface.Services;
using Retinues.Settings;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Common.TopPanel.View
{
    /// <summary>
    /// ViewModel for the top panel of the editor GUI.
    /// </summary>
    public class TopPanelVM : EventListenerVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Title                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Faction)]
        [DataSourceProperty]
        public string EditorTitle => State.Faction.Name;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Tabs                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string EditorTabText => L.S("editor_tab_editor", "Editor");

        [DataSourceProperty]
        public string DoctrinesTabText => L.S("editor_tab_doctrines", "Doctrines");

        [DataSourceProperty]
        public string LibraryTabText => L.S("editor_tab_library", "Library");

        [DataSourceProperty]
        public string SettingsTabText => L.S("editor_tab_settings", "Settings");

        [DataSourceProperty]
        public bool IsPlayerMode => EditorState.Instance?.Mode == EditorMode.Player;

        // Selected state for the *top* tabs
        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool IsEditorTabSelected =>
            State.Page == EditorPage.Character || State.Page == EditorPage.Equipment;

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool IsDoctrinesTabSelected => State.Page == EditorPage.Doctrines;

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool IsLibraryTabSelected => State.Page == EditorPage.Library;

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool IsSettingsTabSelected => State.Page == EditorPage.Settings;

        [EventListener(UIEvent.Mode)]
        [DataSourceProperty]
        public bool IsDoctrinesTabVisible => IsPlayerMode && Configuration.EnableDoctrines;

        [EventListener(UIEvent.Mode)]
        [EventListener(UIEvent.Faction)]
        [DataSourceProperty]
        public bool IsEditorTabEnabled =>
            State.Mode != EditorMode.Player
            || Configuration.EnableRetinues
            || Player.Clan?.Troops.Any(t => t != null && !t.IsHero && t.IsFactionTroop) == true
            || Player.Kingdom?.Troops.Any(t => t != null && !t.IsHero && t.IsFactionTroop) == true;

        [EventListener(UIEvent.Mode)]
        [EventListener(UIEvent.Faction)]
        [DataSourceProperty]
        public Tooltip EditorTabDisabledHint =>
            !IsEditorTabEnabled
                ? new Tooltip(L.T("troops_editor_no_custom", "No troops are available."))
                : null;

        [DataSourceMethod]
        public void ExecuteSelectEditorTab() => State.SetPage();

        [DataSourceMethod]
        public void ExecuteSelectDoctrinesTab() => State.SetPage(EditorPage.Doctrines);

        [DataSourceMethod]
        public void ExecuteSelectLibraryTab() => State.SetPage(EditorPage.Library);

        [DataSourceMethod]
        public void ExecuteSelectSettingsTab() => State.SetPage(EditorPage.Settings);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Faction                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Get the banner image for a faction, or an empty image if none.
        /// </summary>
        private object GetFactionBanner(IBaseFaction faction)
        {
            if (faction?.Banner == null)
                return BannerHelper.EmptyImage;
            return BannerHelper.GetBannerImage(faction.Banner, nineGrid: true);
        }

        [EventListener(UIEvent.Faction)]
        [DataSourceProperty]
        public string FactionColor =>
            State.Mode == EditorMode.Player ? "#ffffffff"
            : (State.LeftBannerFaction as WCulture) == null ? "#ffffffff"
            : Utilities.Colors.UintColorToHex(((WCulture)State.LeftBannerFaction).Color, 0.4f);

        /* ━━━━━━ Left Banner ━━━━━ */

        [EventListener(UIEvent.Faction)]
        [DataSourceProperty]
        public string LeftBannerName =>
            State.Mode == EditorMode.Universal
                ? L.S("editor_culture_select", "Select Culture")
                : L.S("editor_clan_edit", "Edit Clan");

        [EventListener(UIEvent.Faction)]
        [DataSourceProperty]
        public object LeftBanner => GetFactionBanner(State.LeftBannerFaction);

        [EventListener(UIEvent.Faction)]
        [DataSourceProperty]
        public bool CanSelectLeftBanner => FactionController.SelectLeftBannerPopup.Allow(true);

        [EventListener(UIEvent.Faction)]
        [DataSourceProperty]
        public Tooltip LeftBannerHint => FactionController.SelectLeftBannerPopup.Tooltip(true);

        [DataSourceMethod]
        public void ExecuteSelectLeftBanner() =>
            FactionController.SelectLeftBannerPopup.Execute(true);

        [EventListener(UIEvent.Faction)]
        [DataSourceProperty]
        public bool IsLeftBannerVisible =>
            State.Mode == EditorMode.Universal || IsPlayerKingdomRuler();

        /* ━━━━━ Right Banner ━━━━━ */

        [EventListener(UIEvent.Faction)]
        [DataSourceProperty]
        public string RightBannerName =>
            State.Mode == EditorMode.Universal ? L.S("editor_clan_select", "Select Clan")
            : Player.Kingdom != null
                ? L.T("editor_kingdom_edit", "Edit {KINGDOM}")
                    .SetTextVariable("KINGDOM", Player.Kingdom.Name)
                    .ToString()
            : L.S("editor_kingdom_none", "No Kingdom");

        [EventListener(UIEvent.Faction)]
        [DataSourceProperty]
        public object RightBanner => GetFactionBanner(State.RightBannerFaction);

        [EventListener(UIEvent.Faction)]
        [DataSourceProperty]
        public bool CanSelectRightBanner => FactionController.SelectRightBannerPopup.Allow(true);

        [EventListener(UIEvent.Faction)]
        [DataSourceProperty]
        public Tooltip RightBannerHint => FactionController.SelectRightBannerPopup.Tooltip(true);

        [DataSourceMethod]
        public void ExecuteSelectRightBanner() =>
            FactionController.SelectRightBannerPopup.Execute(true);

        [EventListener(UIEvent.Faction)]
        [DataSourceProperty]
        public bool IsRightBannerVisible =>
            State.Mode == EditorMode.Universal || IsPlayerKingdomRuler();

        private static bool IsPlayerKingdomRuler()
        {
            var hero = WHero.Get(Hero.MainHero);
            var kingdom = hero?.Kingdom;

            if (hero == null || kingdom == null)
                return false;

            return ReferenceEquals(kingdom.Leader?.Base, hero.Base);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Faction Export                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Button<bool> ExportButton { get; } =
            new(
                action: ExportController.Export,
                arg: () => true,
                refresh: [UIEvent.Faction],
                label: L.S("button_export", "Export")
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Faction Reset                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Button<bool> ResetButton { get; } =
            new(
                action: ResetController.Reset,
                arg: () => true,
                refresh:
                [
                    UIEvent.Name,
                    UIEvent.Culture,
                    UIEvent.Skill,
                    UIEvent.Gender,
                    UIEvent.Item,
                    UIEvent.Mode,
                ],
                label: L.S("button_reset", "Reset"),
                visibilityGate: () => EditorState.Instance?.Mode != EditorMode.Player
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Delete Clan Troops                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Button<bool> DeleteClanTroopsButton { get; } =
            new(
                action: FactionController.DeleteClanTroopsAction,
                arg: () => true,
                refresh: [UIEvent.Faction, UIEvent.Mode],
                label: L.S("button_delete_clan_troops", "Clear"),
                visibilityGate: () => EditorState.Instance?.Mode == EditorMode.Player
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Manual                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string ManualButtonText => L.S("button_manual", "Manual");

        [DataSourceMethod]
        public void ExecuteOpenManual() => ManualLink.Open();
    }
}
