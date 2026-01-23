#if BL13
using System.Linq;
using Retinues.Settings;
using Retinues.Domain;
using Retinues.Interface.Services;
using SandBox.View;
using SandBox.View.Map.Navigation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;

namespace Retinues.Editor.Integration.MapBar
{
    /// <summary>
    /// Navigation element that opens the troop editor from the map bar.
    /// </summary>
    public sealed class TroopsNavigationElement(MapNavigationHandler handler)
        : MapNavigationElementBase(handler)
    {
        public const string TroopsId = "troops";
        public override string StringId => TroopsId;

        public override bool IsActive =>
            ScreenManager.TopScreen is EditorScreen
            && _game.GameStateManager.ActiveState is EditorGameState egs
            && egs.IsMapBarIntegrated;
        public override bool IsLockingNavigation => false;
        public override bool HasAlert => false;

        /// <summary>
        /// Determines whether the troop editor may be opened and returns a reason if not.
        /// </summary>
        protected override NavigationPermissionItem GetPermission()
        {
            if (_handler.IsNavigationLocked)
                return new NavigationPermissionItem(
                    isAuthorized: false,
                    L.T("troops_editor_locked", "The troop editor is not available.")
                );

            // Grays out when Escape menu is open (and other vanilla lock cases).
            if (!MapNavigationHelper.IsNavigationBarEnabled(_handler))
                return new NavigationPermissionItem(isAuthorized: false, reasonString: null);

            // Player mode requires at least one custom-tree troop to exist.
            if (Player.Clan.Troops.Count() == 0)
                return new NavigationPermissionItem(
                    isAuthorized: false,
                    L.T("troops_editor_no_custom", "No troops are available.")
                );

            // Vanilla behavior: don’t re-open if already active.
            if (IsActive)
                return new NavigationPermissionItem(isAuthorized: false, reasonString: null);

            return new NavigationPermissionItem(isAuthorized: true, reasonString: null);
        }

        /// <summary>
        /// Gets the tooltip text for the troops navigation button.
        /// </summary>
        protected override TextObject GetTooltip() =>
            Configuration.EditorHotkey
                ? L.T("troops_navigation_tooltip_hotkey", "Troops [R]")
                : L.T("troops_navigation_tooltip", "Troops");

        /// <summary>
        /// Gets the alert tooltip; unused for this element.
        /// </summary>
        protected override TextObject GetAlertTooltip() => TextObject.GetEmpty();

        /// <summary>
        /// Opens the troop editor if permitted, handling unsaved changes and screen switching.
        /// </summary>
        public override void OpenView()
        {
            if (!Permission.IsAuthorized)
                return;

            static void OpenEditor() => EditorLauncher.Launch(EditorMode.Player);

            // Match vanilla: warn about unsaved changes on the currently open panel.
            if (
                ScreenManager.TopScreen is IChangeableScreen changeable
                && changeable.AnyUnsavedChanges()
            )
            {
                InformationManager.ShowInquiry(
                    changeable.CanChangesBeApplied()
                        ? MapNavigationHelper.GetUnsavedChangedInquiry(OpenEditor)
                        : MapNavigationHelper.GetUnapplicableChangedInquiry()
                );
            }
            else
            {
                // Match vanilla: close current panel first (Clan/Party/etc), then open editor.
                MapNavigationHelper.SwitchToANewScreen(OpenEditor);
            }
        }

        /// <summary>
        /// Opens the troop editor (params overload).
        /// </summary>
        public override void OpenView(params object[] parameters) => OpenView();

        /// <summary>
        /// No-op navigation link action for the troops element.
        /// </summary>
        public override void GoToLink() { }
    }
}
#endif
