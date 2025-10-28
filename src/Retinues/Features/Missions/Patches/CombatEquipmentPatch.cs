using System;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Features.Missions.Behaviors;
using Retinues.Game.Events;
using Retinues.Game.Wrappers;
using Retinues.Mods;
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

                if (Config.ForceMainBattleSetInCombat || ModuleChecker.GetModule("Shokuho") != null)
                {
                    // Use main battle set if configured to do so.
                    eq = troop.Loadout.Battle.Base;
                }
                else if (agentBuildData.AgentCivilianEquipment)
                {
                    // Use civilian set if spawning as civilian.
                    eq = troop.Loadout.Civilian.Base;
                }
                else if (Battle.MapEvent == null)
                {
                    return; // No battle context, use default equipment (tournaments etc).
                }
                else
                {
                    var battleType = BattleType.FieldBattle;

                    var battle = new Battle(Battle.MapEvent);
                    if (battle.IsSiege)
                        battleType = battle.PlayerIsDefender
                            ? BattleType.SiegeDefense
                            : BattleType.SiegeAssault;

                    // Build eligible sets according to mask; battle set is always eligible.
                    var eligible = new List<WEquipment> { troop.Loadout.Battle };

                    foreach (var alt in troop.Loadout.Alternates)
                        if (CombatEquipmentBehavior.IsEnabled(troop, alt.Index, battleType))
                            eligible.Add(alt);

                    // Fallback safety: if somehow none, force battle set.
                    var pick = eligible.Count > 0 ? eligible : [troop.Loadout.Battle];

                    var rand = new Random();
                    var idx = rand.Next(pick.Count);
                    eq = pick[idx].Base;
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
