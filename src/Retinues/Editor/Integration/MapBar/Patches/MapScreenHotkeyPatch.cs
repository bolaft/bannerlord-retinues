using System.Linq;
using HarmonyLib;
using Retinues.Domain;
using Retinues.Settings;
using Retinues.Utilities;
using SandBox.View.Map;
using TaleWorlds.InputSystem;
using TaleWorlds.ScreenSystem;
#if BL13 || BL14
using SandBox.View.Map.Navigation;
#endif

namespace Retinues.Editor.Integration.MapBar.Patches
{
    /// <summary>
    /// Enables opening the troop editor via the R hotkey on the map screen.
    /// </summary>
#if BL13 || BL14
    [HarmonyPatch(typeof(MapScreen), "TickNavigationInput")]
    internal static class MapScreenTroopsHotkeyPatch
    {
        [HarmonyPostfix]
        private static void Postfix(MapScreen __instance, ref bool __result)
        {
            if (__result)
                return;

            if (!Configuration.EditorHotkey)
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

            if (
                Player.Clan?.Troops.Any(t => t != null && !t.IsHero && t.IsFactionTroop) != true
                && Player.Kingdom?.Troops.Any(t => t != null && !t.IsHero && t.IsFactionTroop)
                    != true
            )
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
#else
    internal static class MapScreenTroopsHotkeyPatch { }
#endif
}
