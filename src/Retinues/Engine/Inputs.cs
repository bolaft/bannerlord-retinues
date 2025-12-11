using System;
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
    public static class Inputs
    {
        /// <summary>
        /// Checks for hotkey presses and triggers associated actions.
        /// </summary>
        public static void HotkeyCheck()
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Batch Input                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        const int DefaultBatchSize = 1;
        const int ShiftBatchSize = 5;
        const int ControlBatchSize = 1000;

        /// <summary>
        /// Determine batch input multiplier based on modifier keys.
        /// </summary>
        public static int BatchInput(int cap = int.MaxValue)
        {
            int batch;

            if (Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl))
                batch = ControlBatchSize;
            else if (Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift))
                batch = ShiftBatchSize;
            else
                batch = DefaultBatchSize;

            return Math.Min(batch, cap);
        }
    }
}
