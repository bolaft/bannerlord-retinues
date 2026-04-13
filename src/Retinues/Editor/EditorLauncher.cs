using System.Linq;
using Retinues.Domain;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Settings;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Editor
{
    /// <summary>
    /// Launcher for the editor GUI.
    /// </summary>
    public static class EditorLauncher
    {
        // Default faction to use when none is specified.
        public static IBaseFaction DefaultFaction => Player.Clan;

        /// <summary>
        /// Launch the editor with the given args.
        /// </summary>
        public static void Launch(EditorLaunchArgs args = null)
        {
            LaunchInternal(args ?? EditorLaunchArgs.Universal());
        }

        /// <summary>
        /// Launch the editor in the given mode.
        /// </summary>
        public static void Launch(EditorMode mode)
        {
            LaunchInternal(EditorLaunchArgs.ForMode(mode));
        }

        /// <summary>
        /// Launch the editor with the given args.
        /// </summary>
        private static void LaunchInternal(EditorLaunchArgs args)
        {
            var gsm = Game.Current?.GameStateManager;
            if (gsm == null)
                return;

            var mode = args?.Mode ?? EditorMode.Universal;

            if (!Configuration.EnableUniversalEditor && mode == EditorMode.Universal)
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

            ClosePreviousEditorInstances(gsm);

            var state = gsm.CreateState<EditorGameState>();
            state.LaunchArgs = args;

            gsm.PushState(state);
        }

        /// <summary>
        /// Try to downgrade a Universal editor launch to Player mode.
        /// </summary>
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

        /// <summary>
        /// Resolve the gate faction for the editor based on the launch args.
        /// </summary>
        public static IBaseFaction ResolveGateFaction(EditorLaunchArgs args)
        {
            if (args == null)
                return DefaultFaction;

            if (args.Faction != null)
                return args.Faction;

            if (args.Character != null && args.Character.IsFactionTroop)
                return args.Character.AssignedMapFaction ?? DefaultFaction;

            if (args.Hero != null)
                return args.Hero.Clan ?? DefaultFaction;

            return DefaultFaction;
        }

        /// <summary>
        /// Closes any previously opened editor instances.
        /// </summary>
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
