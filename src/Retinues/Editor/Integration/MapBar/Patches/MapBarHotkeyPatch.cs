using HarmonyLib;
using Retinues.Settings;
using Retinues.Utilities;
using SandBox.GauntletUI.Map;
using TaleWorlds.InputSystem;
using TaleWorlds.ScreenSystem;
#if BL12
using System.Linq;
using Retinues.Domain;
using Retinues.Editor;
using SandBox.View.Map;
using TaleWorlds.Engine.GauntletUI;
#elif BL13 || BL14
using SandBox.View.Map.Navigation;
#endif

namespace Retinues.Editor.Integration.MapBar.Patches
{
    /// <summary>
    /// Enables opening the troop editor via the R hotkey on the map bar.
    /// </summary>
#if BL12
    /// <remarks>
    /// BL12: Fires when a panel screen is open (HandlePanelSwitching only runs then).
    /// The map-screen idle case is handled by <see cref="MapScreenTroopsHotkeyPatch"/>.
    /// </remarks>
    [HarmonyPatch(typeof(GauntletMapBarGlobalLayer), "HandlePanelSwitching")]
    internal static class MapBarHotkeyPatch
    {
        [HarmonyPostfix]
        private static void Postfix(GauntletMapBarGlobalLayer __instance)
        {
            if (!Configuration.EditorHotkey)
                return;

            var gauntletLayer = ScreenManager.TopScreen?.FindLayer<GauntletLayer>();
            if (gauntletLayer?.Input == null || gauntletLayer.IsFocusedOnInput())
                return;

            if (!gauntletLayer.Input.IsKeyReleased(InputKey.R))
                return;

            TryOpenEditor(__instance);
        }

        internal static void TryOpenEditor(GauntletMapBarGlobalLayer instance)
        {
            if (ScreenManager.TopScreen is EditorScreen)
                return;

            var handler = Reflection.GetFieldValue<MapNavigationHandler>(
                instance,
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
