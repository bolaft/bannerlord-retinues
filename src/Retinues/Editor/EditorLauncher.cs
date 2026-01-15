using System.Linq;
using Retinues.Configuration;
using Retinues.Domain;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
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

            var mode = args?.Mode ?? EditorMode.Universal;

            if (!Settings.EnableUniversalEditor && mode == EditorMode.Universal)
            {
                var downgraded = TryDowngradeUniversalToPlayer(args);
                if (downgraded == null)
                {
                    Log.Debug("Troops editor blocked: Universal Editor is disabled in settings.");
                    return;
                }

                args = downgraded;
                mode = args.Mode;
            }

            if (mode == EditorMode.Player)
            {
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

        private static EditorLaunchArgs TryDowngradeUniversalToPlayer(EditorLaunchArgs args)
        {
            if (args == null)
                return null;

            if (args.Character != null && args.Character.IsFactionTroop)
            {
                var faction = args.Character.AssignedMapFaction ?? Player.Clan;
                return EditorLaunchArgs.Player(faction: faction, character: args.Character);
            }

            if (args.Faction is WClan || args.Faction is WKingdom)
                return EditorLaunchArgs.Player(faction: args.Faction);

            // Heroes are universal-only (appearance editing etc).
            if (args.Hero != null)
                return null;

            return null;
        }

        private static IBaseFaction ResolveGateFaction(EditorLaunchArgs args)
        {
            if (args == null)
                return Player.Clan;

            if (args.Faction != null)
                return args.Faction;

            if (args.Character != null && args.Character.IsFactionTroop)
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
