using System.Linq;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
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
            var gsm = Game.Current?.GameStateManager;
            if (gsm == null)
                return;

            if ((args?.Mode ?? EditorMode.Universal) == EditorMode.Player)
            {
                // Player-mode availability must be based on the intended selection,
                // not always on the player clan. :contentReference[oaicite:3]{index=3}
                var gateFaction = ResolveGateFaction(args);

                if (!EditorAvailability.HasAnyCustomTreeTroops(gateFaction))
                {
                    Log.Info("Troops editor blocked: selected map-faction has no custom troops.");
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
                return GetPlayerClan();

            if (args.Faction != null)
                return args.Faction;

            if (args.Character != null && args.Character.InCustomTree)
                return args.Character.AssignedMapFaction ?? GetPlayerClan();

            if (args.Hero != null)
                return args.Hero.Clan ?? GetPlayerClan();

            return GetPlayerClan();
        }

        private static IBaseFaction GetPlayerClan()
        {
            var hero = Hero.MainHero;
            return hero?.Clan == null ? null : WClan.Get(hero.Clan);
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
