using Retinues.Features.Agents;
using Retinues.Game.Events;
using Retinues.Mods;
using Retinues.Utils;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace OldRetinues.Game.Helpers
{
    /// <summary>
    /// Common mission filters used by retinue combat features.
    /// </summary>
    internal static class MissionHelper
    {
        private static Mission _cachedMission;
        private static bool _cachedIsCombatMission;
        private static Battle _cachedBattle;
        private static Mission _cachedBattleMission;
        private static PolicyToggleType _cachedBattleType;

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

                Log.Info($"Computed IsCombatMission: {_cachedIsCombatMission}");
            }

            return _cachedIsCombatMission;
        }

        /// <summary>
        /// Gets the battle type for the current mission.
        /// </summary>
        public static PolicyToggleType GetBattleType(Mission mission = null)
        {
            mission ??= Mission.Current;

            // No mission => no battle
            if (mission == null)
            {
                _cachedBattleMission = null;
                _cachedBattle = null;
                _cachedBattleType = PolicyToggleType.FieldBattle;
            }
            // Keep using the existing mission cache for "is this even a combat mission?"
            else if (!IsCombatMission(mission))
            {
                _cachedBattleMission = mission;
                _cachedBattle = null;
                _cachedBattleType = PolicyToggleType.FieldBattle;
                return _cachedBattleType;
            }
            // Here mission is a valid combat mission.
            // Rebuild the Battle + type only when the mission itself changes.
            else if (!ReferenceEquals(mission, _cachedBattleMission) || _cachedBattle == null)
            {
                _cachedBattleMission = mission;
                _cachedBattle = new Battle();
                _cachedBattleType = ComputeBattleType(_cachedBattle);

                Log.Info($"Computed BattleType: {_cachedBattleType}");
            }

            return _cachedBattleType;
        }

        /// <summary>
        /// Checks if the given mission is a combat mission.
        /// </summary>
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
                && mode != MissionMode.StartUp
            )
                return false;

            if (mode == MissionMode.StartUp)
            {
                Log.Info("StartUp mode detected.");
                if (MobileParty.MainParty?.MapEvent is null)
                {
                    Log.Info("No map event - not a combat mission.");
                    return false;
                }

                if (ModCompatibility.HasNavalDLC == false)
                {
                    Log.Info("No Naval DLC, can't be naval combat mission, skipping.");
                    return false;
                }
                else
                {
                    Log.Info("Naval DLC present, assuming naval combat mission.");
                }
            }

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

        /// <summary>
        /// Computes the battle type for the given battle.
        /// </summary>
        private static PolicyToggleType ComputeBattleType(Battle battle)
        {
            Log.Info("Computing battle type.");

            PolicyToggleType policy = PolicyToggleType.FieldBattle;

            if (battle?.IsSiege == true)
                policy = battle.PlayerIsDefender
                    ? PolicyToggleType.SiegeDefense
                    : PolicyToggleType.SiegeAssault;
            else if (battle?.IsNavalBattle == true)
                policy = PolicyToggleType.NavalBattle;

            return policy;
        }
    }
}
