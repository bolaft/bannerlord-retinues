#if BL13
using System.Collections.Generic;
using HarmonyLib;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.MountAndBlade;

namespace OldRetinues.Safety.Patches
{
    [HarmonyPatch(typeof(DisguiseMissionLogic), "CanAgentSeeAgent")]
    internal static class DisguiseMissionLogicCanAgentSeeAgentPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(
            Agent agent1,
            Agent agent2,
            ref bool hasVisualOnCorpse,
            ref bool __result,
            Dictionary<Agent, AlarmedBehaviorGroup> ____agentAlarmedBehaviorCache
        )
        {
            if (agent1 == null)
            {
                hasVisualOnCorpse = false;
                __result = false;
                return false; // skip original
            }

            AlarmedBehaviorGroup behaviorGroup;
            if (
                !____agentAlarmedBehaviorCache.TryGetValue(agent1, out behaviorGroup)
                || behaviorGroup == null
            )
            {
                // Vanilla bug: this can happen if guards were never passed through AddBehaviorGroups (e.g. female guards).
                // We treat it as "no visual" instead of crashing.
                // Log.Debug($"Disguise guard without AlarmedBehaviorGroup: {agent1.Name} ({agent1.Character?.Name}).");

                hasVisualOnCorpse = false;
                __result = false;
                return false; // skip original so the indexer is never hit
            }

            // Dictionary contains the agent; let the original method run.
            return true;
        }
    }
}
#endif
