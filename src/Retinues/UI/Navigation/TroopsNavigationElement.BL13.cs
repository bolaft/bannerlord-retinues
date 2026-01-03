#if BL13
using SandBox.View;
using Retinues.Configuration;
using Retinues.Editor;
using Retinues.UI.Services;
using SandBox.View.Map.Navigation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;

namespace Retinues.UI.Navigation;

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
        if (!EditorAvailability.HasPlayerClanCustomTreeTroops())
            return new NavigationPermissionItem(
                isAuthorized: false,
                L.T("troops_editor_no_custom", "No troops are available.")
            );

        // Vanilla behavior: don’t re-open if already active.
        if (IsActive)
            return new NavigationPermissionItem(isAuthorized: false, reasonString: null);

        return new NavigationPermissionItem(isAuthorized: true, reasonString: null);
    }

    protected override TextObject GetTooltip() =>
        Settings.EditorHotkey
            ? L.T("troops_navigation_tooltip_hotkey", "Troops [R]")
            : L.T("troops_navigation_tooltip", "Troops");

    protected override TextObject GetAlertTooltip() => TextObject.GetEmpty();

    public override void OpenView()
    {
        if (!Permission.IsAuthorized)
            return;

        void OpenEditor() => EditorLauncher.Launch(EditorMode.Player);

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

    public override void OpenView(params object[] parameters) => OpenView();

    public override void GoToLink() { }
}
#endif
