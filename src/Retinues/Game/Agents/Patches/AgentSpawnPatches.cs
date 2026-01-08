using System;
using HarmonyLib;
using Retinues.Utilities;
using TaleWorlds.MountAndBlade;

namespace Retinues.Game.Agents.Patches
{
    /// <summary>
    /// Applies spawn-time overrides (equipment context rules + mixed gender) to agent spawning.
    /// </summary>
    [HarmonyPatch(typeof(Mission), "SpawnAgent")]
    internal static class AgentSpawnPatches
    {
        [HarmonyPrefix]
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
