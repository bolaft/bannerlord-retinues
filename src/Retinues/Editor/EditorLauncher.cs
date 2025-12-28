using System.Linq;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;

namespace Retinues.Editor
{
    public static class EditorLauncher
    {
        public static void Launch()
        {
            LaunchInternal(null);
        }

        public static void Launch(WCharacter character)
        {
            LaunchInternal(new EditorLaunchArgs(character));
        }

        public static void Launch(WHero hero)
        {
            LaunchInternal(new EditorLaunchArgs(hero));
        }

        public static void Launch(WClan clan)
        {
            LaunchInternal(new EditorLaunchArgs(clan));
        }

        public static void Launch(WCulture culture)
        {
            LaunchInternal(new EditorLaunchArgs(culture));
        }

        private static void LaunchInternal(EditorLaunchArgs args)
        {
            var gsm = Game.Current?.GameStateManager;
            if (gsm == null)
                return;

            ClosePreviousEditorInstances(gsm);

            var state = gsm.CreateState<EditorGameState>();
            state.LaunchArgs = args;

            gsm.PushState(state);
        }

        private static void ClosePreviousEditorInstances(GameStateManager gsm)
        {
            // Pop from the top until no EditorState remains.
            // This prevents stacking editor-on-editor (and also cleans up cases like editor + barber etc).
            while (gsm.GameStates.Any(s => s is EditorGameState))
            {
                var active = gsm.ActiveState;
                if (active == null)
                    break;

                gsm.PopState(active.Level);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Editor Hotkey                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool EnableEditorHotkey => Settings.EditorHotkey;

        public static void EditorHotkeyCheck()
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
                EditorLauncher.Launch();
            }
        }
    }
}
