#if BL13
using SandBox.GauntletUI.Map;
using SandBox.View.Map.Navigation;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Game;
using Retinues.Utilities;
using Retinues.Editor;
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
            var el =
                handler?.GetElement(TroopsNavigationElement.TroopsId) as MapNavigationElementBase;
            if (el == null)
                return;

            if (!EditorAvailability.HasAnyCustomTreeTroops(Player.Clan))
                return;

            if (!el.Permission.IsAuthorized || el.IsActive)
                return;

            el.OpenView();
            __result = true;
        }
    }
}
#endif
