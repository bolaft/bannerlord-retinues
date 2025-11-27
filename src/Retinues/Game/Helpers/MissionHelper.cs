using Retinues.Utils;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Game.Helpers
{
    /// <summary>
    /// Common mission filters used by retinue combat features.
    /// </summary>
    internal static class MissionHelper
    {
        private static Mission _cachedMission;
        private static bool _cachedIsCombatMission;

        /// <summary>
        /// Returns true for battle-like missions where combat features should apply
        /// (Battle, Duel, Deployment, Stealth; not tournaments/arena).
        /// </summary>
        public static bool IsCombatMission(Mission mission = null)
        {
            mission ??= Mission.Current;

            if (mission == null)
            {
                _cachedMission = null;
                _cachedIsCombatMission = false;
                return false;
            }

            if (!ReferenceEquals(mission, _cachedMission))
            {
                _cachedMission = mission;
                _cachedIsCombatMission = ComputeIsCombatMission(mission);
            }

            return _cachedIsCombatMission;
        }

        private static bool ComputeIsCombatMission(Mission mission)
        {
            var mode = mission.Mode;

            Log.Info($"Computing mission cache. Mode: {mode}");

            // Mode whitelist (Battle, Duel, Deployment, Stealth)
            if (
                mode != MissionMode.Battle
                && mode != MissionMode.Duel
                && mode != MissionMode.Deployment
                && mode != MissionMode.Stealth
            )
                return false;

            // Exclude tournaments / arena battles
            foreach (var behavior in mission.MissionBehaviors)
            {
                if (behavior is TournamentBehavior)
                    return false;

                var name = behavior.GetType().FullName?.ToLowerInvariant() ?? string.Empty;
                if (name.Contains("tournament") || name.Contains("arena"))
                    return false;
            }

            return true;
        }
    }
}
