using System;
using HarmonyLib;
using Retinues.Utilities;
using TaleWorlds.MountAndBlade;

namespace Retinues.Behaviors.Agents.Patches
{
    /// <summary>
    /// Applies spawn-time overrides (equipment context rules + mixed gender) to agent spawning.
    /// </summary>
    [HarmonyPatch(typeof(Mission), "SpawnAgent")]
    internal static class SpawnResolverPatch
    {
        /// <summary>
        /// Prefix patch that applies spawn resolution to the agent build data before spawning.
        ///
        /// Runs at <see cref="Priority.Last"/> so appearance mods that cooperate on the same spawn
        /// (e.g. Banner Color Persistence, and the integrated variants in overhauls like Realm of
        /// Thrones) get to run first. Those mods bail out when the agent's spawn equipment has
        /// already been overridden; since we set an equipment override here, running before them
        /// would silently suppress their clan/banner color application. They set clothing colors and
        /// we set equipment items — separate fields — so letting them go first keeps both.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(
            Mission __instance,
            AgentBuildData agentBuildData,
            bool spawnFromAgentVisuals
        )
        {
            try
            {
                AgentSpawnResolver.ApplyTo(__instance, agentBuildData);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Agent spawn override failed.");
            }
        }
    }
}
