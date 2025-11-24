using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Game.Events;
using Retinues.Game.Helpers;
using Retinues.Game.Wrappers;
using Retinues.Mods;
using Retinues.Utils;
using SandBox.Tournaments.MissionLogics;
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
                    if (Config.EnableGlobalEditor == false)
                        return; // Feature disabled for vanilla troops

                List<MissionMode> modeWhiteList =
                [
                    MissionMode.Battle,
                    MissionMode.Duel,
                    MissionMode.Deployment,
                    MissionMode.Stealth,
                    MissionMode.Duel,
                ];

                var m = Mission.Current;
                if (m == null)
                    return; // No mission, nothing to do

                if (!modeWhiteList.Contains(m.Mode))
                    return; // Only affect allowed missions

                // Try to ensure we are not in a tournament or arena battle
                foreach (var behavior in Mission.Current?.MissionBehaviors)
                {
                    if (behavior is TournamentBehavior)
                        return;

                    var name = behavior.GetType().FullName.ToLowerInvariant();
                    if (name.Contains("tournament"))
                        return;

                    if (name.Contains("arena"))
                        return;
                }

                // Pick the equipment set
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
                else if (
                    Config.ForceMainBattleSetInCombat || ModCompatibility.NoAlternateEquipmentSets
                )
                {
                    // Force main battle set
                    chosenSet = troop.Loadout.Battle; // index 0 normalized to battle set
                }
                else
                {
                    var battleType = PolicyToggleType.FieldBattle;
                    var battle = new Battle();
                    if (battle.IsSiege)
                        battleType = battle.PlayerIsDefender
                            ? PolicyToggleType.SiegeDefense
                            : PolicyToggleType.SiegeAssault;

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
                    {
                        // Safety fallback: main battle set
                        chosenSet = troop.Loadout.Battle;
                    }
                    else
                    {
                        chosenSet = eligible[MBRandom.RandomInt(eligible.Count)];
                    }
                }

                var eq = chosenSet.Base;

                // Apply gender override if this set has it
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
                // else: we don't touch gender, so default CharacterObject gender is used.

                // Lock in equipment and turn off randomization/variants
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
