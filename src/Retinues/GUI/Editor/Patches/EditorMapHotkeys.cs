using System;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.InputSystem;

namespace Retinues.GUI.Editor.Patches
{
    /// <summary>
    /// Global-map hotkeys to open the editor from the campaign map.
    /// Shift+R -> Personal (player clan)
    /// </summary>
    [HarmonyPatch(typeof(MapState))]
    internal static class EditorMapHotkeys
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnMapModeTick")]
        private static void OnMapModeTick_Postfix(MapState __instance, float dt)
        {
            try
            {
                if (!Config.EnableEditorHotkey)
                    return;

                // Only when actually in a running campaign on the world map
                if (TaleWorlds.Core.Game.Current == null || Campaign.Current == null)
                    return;

                if (TaleWorlds.Core.Game.Current.GameStateManager.ActiveState != __instance)
                    return;

                if (!IsShiftHeld())
                    return;

                // Shift + R -> personal editor (player clan)
                if (Input.IsKeyReleased(InputKey.R))
                {
                    Log.Info(
                        "EditorMapHotkeys: Shift+R pressed on map, opening editor in Personal mode."
                    );
                    ClanScreen.LaunchEditor(EditorMode.Personal);
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        private static bool IsShiftHeld()
        {
            return Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift);
        }
    }
}
