using System;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Features.Missions.Patches
{
    [HarmonyPatch(typeof(Mission), "SpawnAgent")]
    internal static class Mission_SpawnAgent_Prefix
    {
        // Runs before the engine constructs the Agent, so no randomization slips through.
        static void Prefix(AgentBuildData agentBuildData, bool spawnFromAgentVisuals)
        {
            try
            {
                if (agentBuildData == null)
                    return;

                var character = agentBuildData.AgentCharacter;
                if (character == null)
                    return;

                var troop = new WCharacter(character.StringId);
                if (!troop.IsCustom)
                    return; // Only affect your custom troops

                Equipment eq = null;

                if (Config.ForceMainBattleSetInCombat)
                {
                    // Use main battle set if configured to do so.
                    eq = troop.Loadout.Battle.Base;
                }
                else if (agentBuildData.AgentCivilianEquipment)
                {
                    // Use civilian set if spawning as civilian.
                    eq = troop.Loadout.Civilian.Base;
                }
                else
                {
                    // Randomly pick one of the sets (battle or alternates).
                    var allSets = new List<WEquipment> { troop.Loadout.Battle };
                    allSets.AddRange(troop.Loadout.Alternates);
                    if (allSets.Count > 0)
                    {
                        var rand = new Random();
                        var idx = rand.Next(allSets.Count);
                        eq = allSets[idx].Base;
                    }
                }

                // Force the main battle set and prevent randomization/variants.
                agentBuildData
                    .Equipment(eq)
                    .MissionEquipment(null) // ensure nothing overrides our equipment later
                    .FixedEquipment(true)
                    .CivilianEquipment(false)
                    .NoWeapons(false)
                    .NoArmor(false);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
