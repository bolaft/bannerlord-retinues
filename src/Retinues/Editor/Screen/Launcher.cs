using Retinues.Model.Characters;
using Retinues.Model.Factions;
using TaleWorlds.Core;

namespace Retinues.Editor.Screen
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

            var state = gsm.CreateState<EditorState>();
            state.LaunchArgs = args;

            gsm.PushState(state);
        }
    }
}
