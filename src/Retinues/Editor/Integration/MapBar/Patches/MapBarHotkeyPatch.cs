using HarmonyLib;
using Retinues.Settings;
using Retinues.Utilities;
using SandBox.GauntletUI.Map;
using TaleWorlds.InputSystem;
#if BL13 || BL14
using SandBox.View.Map.Navigation;
#endif

namespace Retinues.Editor.Integration.MapBar.Patches
{
    /// <summary>
    /// Enables opening the troop editor via the R hotkey on the map bar.
    /// </summary>
#if BL13 || BL14
    [HarmonyPatch(typeof(GauntletMapBarGlobalLayer), "HandlePanelSwitchingInput")]
    internal static class MapBarHotkeyPatch
    {
        [HarmonyPostfix]
        private static void Postfix(
            GauntletMapBarGlobalLayer __instance,
            InputContext inputContext,
            ref bool __result
        )
        {
            if (__result)
                return;

            if (!Configuration.EditorHotkey)
                return;

            // Runs under vanilla mapbar gating (focused layer + not focused on input),
            // so it will NOT trigger while typing in text fields.
            if (!inputContext.IsKeyReleased(InputKey.R))
                return;

            // BL13: map bar uses INavigationHandler, but the concrete type is SandBox.View.Map.Navigation.MapNavigationHandler.
            var handler =
                Reflection.GetFieldValue<object>(__instance, "_mapNavigationHandler")
                as MapNavigationHandler;
            var el =
                handler?.GetElement(TroopsNavigationElement.TroopsId) as MapNavigationElementBase;
            if (el == null)
                return;

            if (!el.Permission.IsAuthorized || el.IsActive)
                return;

            el.OpenView();
            __result = true;
        }
    }
#else
    internal static class MapBarHotkeyPatch { }
#endif
}
