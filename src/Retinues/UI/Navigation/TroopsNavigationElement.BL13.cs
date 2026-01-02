#if BL13
using Retinues.Configuration;
using Retinues.Editor;
using Retinues.UI.Services;
using SandBox.View.Map.Navigation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace Retinues.UI.Navigation;

public sealed class TroopsNavigationElement(MapNavigationHandler handler)
    : MapNavigationElementBase(handler)
{
    public const string TroopsId = "troops";
    public override string StringId => TroopsId;
    public override bool IsActive => false; // set true when your screen/state is active
    public override bool IsLockingNavigation => false;
    public override bool HasAlert => false;

    protected override NavigationPermissionItem GetPermission()
    {
        // Keep custom reason when the handler hard-locks navigation.
        if (_handler.IsNavigationLocked)
            return new NavigationPermissionItem(
                isAuthorized: false,
                L.T("troops_editor_locked", "The troop editor is not available.")
            );

        // Grays out when Escape menu is open (and other vanilla lock cases).
        if (!MapNavigationHelper.IsNavigationBarEnabled(_handler))
            return new NavigationPermissionItem(isAuthorized: false, reasonString: null);

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

        EditorLauncher.Launch(EditorMode.Player);
    }

    public override void OpenView(params object[] parameters) => OpenView();

    public override void GoToLink() { }
}
#endif
