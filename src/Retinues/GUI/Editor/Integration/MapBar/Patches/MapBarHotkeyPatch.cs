using System.Linq;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Domain;
using Retinues.Utilities;
using SandBox.GauntletUI.Map;
using SandBox.View.Map.Navigation;
using TaleWorlds.InputSystem;

namespace Retinues.GUI.Editor.Integration.MapBar.Patches
{
    /// <summary>
    /// Enables opening the troop editor via the R hotkey on the map bar.
    /// </summary>
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

            if (Player.Clan.Troops.Count() == 0)
                return;

            if (!el.Permission.IsAuthorized || el.IsActive)
                return;

            el.OpenView();
            __result = true;
        }
    }
}
