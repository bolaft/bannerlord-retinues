using System.Linq;
using HarmonyLib;
using Retinues.Domain;
using Retinues.Settings;
using Retinues.Utilities;
using SandBox.View.Map;
using TaleWorlds.InputSystem;
using TaleWorlds.ScreenSystem;
#if BL12
using SandBox.GauntletUI.Map;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using Retinues.Editor;
#elif BL13 || BL14
using SandBox.View.Map.Navigation;
#endif

namespace Retinues.Editor.Integration.MapBar.Patches
{
    /// <summary>
    /// Enables opening the troop editor via the R hotkey on the map screen.
    /// </summary>
#if BL12
    /// <remarks>
    /// BL12: GauntletMapBarGlobalLayer.OnTick runs every frame on the map.
    /// We intercept the MapState branch (no panel open) and check the scene layer input,
    /// mirroring what BL13 does in MapScreen.TickNavigationInput.
    /// </remarks>
    [HarmonyPatch(typeof(GauntletMapBarGlobalLayer), "OnTick")]
    internal static class MapScreenTroopsHotkeyPatch
    {
        [HarmonyPostfix]
        private static void Postfix(GauntletMapBarGlobalLayer __instance)
        {
            // Keep the troops button enabled/active state in sync every frame.
            MapNavigationVMMixin.Current?.Refresh();

            if (!Configuration.EditorHotkey)
                return;

            // Only fire when on the plain map screen (no panel open).
            if (Game.Current?.GameStateManager?.ActiveState is not MapState)
                return;

            if (ScreenManager.TopScreen is not MapScreen mapScreen)
                return;

            var sceneLayer = mapScreen.SceneLayer;
            if (sceneLayer?.Input == null)
                return;

            // Match vanilla gating: don't fire while a Gauntlet textbox has keyboard focus.
            if (ScreenManager.FocusedLayer != sceneLayer)
                return;

            if (sceneLayer.Input.IsShiftDown() || sceneLayer.Input.IsControlDown())
                return;

            if (!sceneLayer.Input.IsKeyPressed(InputKey.R))
                return;

            if (ScreenManager.TopScreen is EditorScreen)
                return;

            var handler = Reflection.GetFieldValue<MapNavigationHandler>(
                __instance,
                "_mapNavigationHandler"
            );
            if (handler?.IsNavigationLocked == true)
                return;

            if (!MapNavigationVMMixin.HasEditableTroops())
                return;

            EditorLauncher.Launch(EditorMode.Player);
        }
    }
#elif BL13 || BL14
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
