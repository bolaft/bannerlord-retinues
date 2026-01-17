using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Helpers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.GUI.Components;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Editor.Modules.Common.TopPanel.Controllers.Faction;
using Retinues.GUI.Services;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.Modules.Common.TopPanel.View
{
    /// <summary>
    /// Root editor ViewModel; initializes shared state and child VMs.
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

        [DataSourceProperty]
        public bool IsDoctrinesTabVisible => IsPlayerMode && Settings.EnableDoctrines;

        [DataSourceMethod]
        public void ExecuteSelectEditorTab() => State.SetPage();

        [DataSourceMethod]
        public void ExecuteSelectDoctrinesTab() => State.SetPage(EditorPage.Doctrines);

        [DataSourceMethod]
        public void ExecuteSelectLibraryTab() => State.SetPage(EditorPage.Library);

        [DataSourceMethod]
        public void ExecuteSelectSettingsTab()
        {
            Log.Debug("Settings tab clicked. Opening MCM settings: Retinues.Settings");

            if (!Configuration.Menu.MCM.TryOpenSettings("Retinues.Settings"))
                Log.Warning("Failed to open MCM settings screen.");
        }

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
            : UintColorToHex(((WCulture)State.LeftBannerFaction).Color, 0.4f);

        private static string UintColorToHex(uint color, float towardWhite = 0.0f)
        {
            // 0xAARRGGBB
            byte a = (byte)((color >> 24) & 0xFF);
            byte r = (byte)((color >> 16) & 0xFF);
            byte g = (byte)((color >> 8) & 0xFF);
            byte b = (byte)(color & 0xFF);

            // clamp 0..1
            if (towardWhite < 0f)
                towardWhite = 0f;
            if (towardWhite > 1f)
                towardWhite = 1f;

            r = (byte)(r + (255 - r) * towardWhite);
            g = (byte)(g + (255 - g) * towardWhite);
            b = (byte)(b + (255 - b) * towardWhite);

            return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
        }

        /* ━━━━━━ Left Banner ━━━━━ */

        [EventListener(UIEvent.Faction)]
        [DataSourceProperty]
        public string LeftBannerName =>
            State.Mode == EditorMode.Universal
                ? (State.LeftBannerFaction as WCulture)?.Name?.ToString()
                    ?? L.S("editor_culture_select", "Select a Culture")
                : (State.LeftBannerFaction as WClan)?.Name?.ToString()
                    ?? L.S("editor_clan_select", "Select a Clan");

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

        [DataSourceProperty]
        public bool IsLeftBannerVisible => true;

        /* ━━━━━ Right Banner ━━━━━ */

        [EventListener(UIEvent.Faction)]
        [DataSourceProperty]
        public string RightBannerName =>
            State.Mode == EditorMode.Universal
                ? (State.RightBannerFaction as WClan)?.Name?.ToString()
                    ?? L.S("editor_clan_select", "Select a Clan")
                : (State.RightBannerFaction as WKingdom)?.Name?.ToString()
                    ?? L.S("editor_kingdom_none", "No Kingdom");

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
        public Button<bool> ExportFactionButton { get; } =
            new(
                action: FactionController.ExportFaction,
                arg: () => true,
                refresh: [UIEvent.Faction],
                sprite: "SPGeneral\\Skills\\gui_skills_icon_steward_tiny",
                color: "#f8eed1ff"
            );
    }
}
