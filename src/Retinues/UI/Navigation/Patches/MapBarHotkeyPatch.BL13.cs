#if BL13
using SandBox.GauntletUI.Map;
using SandBox.View.Map.Navigation;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Utilities;
using TaleWorlds.InputSystem;

namespace Retinues.UI.Navigation.Patches
{
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

            if (!Settings.EditorHotkey)
                return;

            // Runs under vanilla mapbar gating (focused layer + not focused on input),
            // so it will NOT trigger while typing in text fields.
            if (!inputContext.IsKeyReleased(InputKey.R))
                return;

            var handler = Reflection.GetFieldValue<MapNavigationHandler>(
                __instance,
                "_mapNavigationHandler"
            );
            var el = handler?.GetElement(TroopsNavigationElement.TroopsId);
            el?.OpenView();

            if (el != null)
                __result = true;
        }
    }
}
#endif
