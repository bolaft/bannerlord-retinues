using System.Linq;
using Retinues.Domain.Factions;
using Retinues.Game;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Editor
{
    public static class EditorLauncher
    {
        public static void Launch(EditorLaunchArgs args = null)
        {
            LaunchInternal(args ?? EditorLaunchArgs.Universal());
        }

        public static void Launch(EditorMode mode)
        {
            LaunchInternal(EditorLaunchArgs.ForMode(mode));
        }

        private static void LaunchInternal(EditorLaunchArgs args)
        {
            var gsm = TaleWorlds.Core.Game.Current?.GameStateManager;
            if (gsm == null)
                return;

            if ((args?.Mode ?? EditorMode.Universal) == EditorMode.Player)
            {
                // Player-mode availability must be based on the intended selection,
                // not always on the player clan.
                var gateFaction = ResolveGateFaction(args);

                if (!EditorAvailability.HasAnyCustomTreeTroops(gateFaction))
                {
                    Log.Debug("Troops editor blocked: selected map-faction has no custom troops.");
                    return;
                }
            }

            ClosePreviousEditorInstances(gsm);

            var state = gsm.CreateState<EditorGameState>();
            state.LaunchArgs = args;

            gsm.PushState(state);
        }

        private static IBaseFaction ResolveGateFaction(EditorLaunchArgs args)
        {
            if (args == null)
                return Player.Clan;

            if (args.Faction != null)
                return args.Faction;

            if (args.Character != null && args.Character.InCustomTree)
                return args.Character.AssignedMapFaction ?? Player.Clan;

            if (args.Hero != null)
                return args.Hero.Clan ?? Player.Clan;

            return Player.Clan;
        }

        private static void ClosePreviousEditorInstances(GameStateManager gsm)
        {
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
