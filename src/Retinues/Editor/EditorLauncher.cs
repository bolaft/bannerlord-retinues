using System.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using TaleWorlds.Core;

namespace Retinues.Editor
{
    public static class EditorLauncher
    {
        public static void Launch(EditorMode mode = EditorMode.Universal)
        {
            LaunchInternal(new EditorLaunchArgs(mode));
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
    }
}
