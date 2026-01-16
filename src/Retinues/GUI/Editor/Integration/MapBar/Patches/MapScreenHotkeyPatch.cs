using System.Linq;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Domain;
using Retinues.Utilities;
using SandBox.View.Map;
using SandBox.View.Map.Navigation;
using TaleWorlds.InputSystem;
using TaleWorlds.ScreenSystem;

namespace Retinues.GUI.Editor.Integration.MapBar.Patches
{
    [HarmonyPatch(typeof(MapScreen), "TickNavigationInput")]
    internal static class MapScreenTroopsHotkeyPatch
    {
        [HarmonyPostfix]
        private static void Postfix(MapScreen __instance, ref bool __result)
        {
            if (__result)
                return;

            if (!Settings.EditorHotkey)
                return;

            var sceneLayer = __instance.SceneLayer;
            if (sceneLayer?.Input == null)
                return;

            // Match vanilla gating
            if (sceneLayer.Input.IsShiftDown() || sceneLayer.Input.IsControlDown())
                return;

            // Critical: prevents triggering while typing in Gauntlet textboxes (save name, search fields, etc.)
            if (ScreenManager.FocusedLayer != sceneLayer)
                return;

            if (!sceneLayer.Input.IsKeyPressed(InputKey.R))
                return;

            if (Player.Clan.Troops.Count() == 0)
                return;

            var handler = Reflection.GetFieldValue<MapNavigationHandler>(
                __instance,
                "_navigationHandler"
            );

            if (
                handler?.GetElement(TroopsNavigationElement.TroopsId)
                is not MapNavigationElementBase el
            )
                return;

            if (!el.Permission.IsAuthorized || el.IsActive)
                return;

            el.OpenView();
            __instance.MapCursor?.SetVisible(false);

            __result = true;
        }
    }
}
