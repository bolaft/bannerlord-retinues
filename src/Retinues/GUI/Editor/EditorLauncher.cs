using Retinues.GUI.Editor.VM;
using Retinues.Utils;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;

namespace Retinues.GUI.Editor
{
    public static class EditorLauncher
    {
        /// <summary>Open the Clan screen with the editor in Studio Mode.</summary>
        public static void OpenStudio()
        {
            try
            {
                Log.Info("Launching Editor in Studio Mode…");
                EditorVM.IsStudioMode = true; // single source of truth

                var gsm = TaleWorlds.Core.Game.Current?.GameStateManager;
                if (gsm == null)
                {
                    Log.Warn("GameStateManager is null, cannot open ClanState.");
                    return;
                }

                var clanState = gsm.CreateState<ClanState>();
                gsm.PushState(clanState);
            }
            catch (System.Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
