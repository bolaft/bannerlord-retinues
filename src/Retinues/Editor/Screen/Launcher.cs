using TaleWorlds.Core;

namespace Retinues.Editor.Screen
{
    public static class RetinuesEditorLauncher
    {
        public static void Launch()
        {
            var gsm = Game.Current?.GameStateManager;
            if (gsm == null)
                return;

            gsm.PushState(gsm.CreateState<EditorState>());
        }
    }
}
