using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Game.Events;
using Retinues.Game.Helpers;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Features.Agents.Patches
{
    [HarmonyPatch(typeof(Mission), "SpawnAgent")]
    internal static class Mission_SpawnAgent_Prefix
    {
        static void Prefix(AgentBuildData agentBuildData, bool spawnFromAgentVisuals)
        {
            try
            {
                var troop = AgentHelper.TroopFromAgentBuildData(agentBuildData);
                if (troop == null)
                    return; // Safety check

                if (troop.IsHero)
                    return; // Don't affect heroes

                if (!troop.IsCustom)
                    return; // Feature disabled for vanilla troops

                if (!MissionHelper.IsCombatMission())
                    return; // Combat only

                // Choose the equipment set
                WEquipment chosenSet = null;

                if (agentBuildData.AgentCivilianEquipment)
                {
                    // Pick a civilian set if available
                    var civs = troop.Loadout.CivilianSets.ToList();
                    chosenSet =
                        civs.Count == 0
                            ? troop.Loadout.Civilian
                            : civs[MBRandom.RandomInt(civs.Count)];
                }
                else if (Config.ForceMainBattleSetInCombat)
                {
                    // Force main battle set
                    chosenSet = troop.Loadout.Battle;
                }
                else
                {
                    var battleType = MissionHelper.GetBattleType();

                    var all = troop.Loadout.Equipments;
                    var eligible = new List<WEquipment>();

                    for (int i = 0; i < all.Count; i++)
                    {
                        var we = all[i];
                        if (we.IsCivilian)
                            continue;

                        if (CombatAgentBehavior.IsEnabled(troop, i, battleType))
                            eligible.Add(we);
                    }

                    if (eligible.Count == 0)
                        eligible.Add(troop.Loadout.Battle); // safety fallback

                    chosenSet = eligible[MBRandom.RandomInt(eligible.Count)];
                }

                if (chosenSet == null)
                    return;

                var eq = chosenSet.Base;

                // Gender override: flip if enabled for this set
                if (
                    CombatAgentBehavior.IsEnabled(
                        troop,
                        chosenSet.Index,
                        PolicyToggleType.GenderOverride
                    )
                )
                {
                    // Flip relative to the troop's base gender
                    agentBuildData.IsFemale(!troop.IsFemale);
                }

                // Force the chosen set and prevent randomization
                agentBuildData
                    .Equipment(eq)
                    .MissionEquipment(null) // ensure nothing overrides equipment later
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
