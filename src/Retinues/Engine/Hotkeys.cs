using Retinues.Configuration;
using Retinues.GUI.ClanScreen;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;

namespace Retinues.Engine
{
    [SafeClass]
    public static class Hotkeys
    {
        /// <summary>
        /// Checks for hotkey presses and triggers associated actions.
        /// </summary>
        public static void Check()
        {
            EditorHotkeyCheck();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Editor Hotkey                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool EnableEditorHotkey => Settings.EditorHotkey;

        private static void EditorHotkeyCheck()
        {
            if (!EnableEditorHotkey)
                return;

            // Only on campaign map
            if (Game.Current?.GameStateManager.ActiveState is not MapState)
                return;

            // Must be in a campaign
            if (Campaign.Current == null)
                return;

            // Shift + R to open the editor
            if (Input.IsKeyDown(InputKey.LeftShift) && Input.IsKeyReleased(InputKey.R))
            {
                Log.Info("Editor launched via hotkey.");
                ClanScreenMixin.Launch();
            }
        }
    }
}
