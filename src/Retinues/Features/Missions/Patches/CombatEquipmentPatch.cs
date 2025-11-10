using System;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Features.Missions.Behaviors;
using Retinues.Game.Events;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Features.Missions.Patches
{
    [HarmonyPatch(typeof(Mission), "SpawnAgent")]
    internal static class Mission_SpawnAgent_Prefix
    {
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
                // if (!troop.IsCustom)
                //     return; // Only affect custom troops

                if (Mission.Current?.GetMissionBehavior<TournamentBehavior>() != null)
                    return; // Don't modify equipment in tournaments

                Equipment eq = null;

                if (agentBuildData.AgentCivilianEquipment)
                {
                    // Use civilian set if spawning as civilian.
                    eq = troop.Loadout.Civilian.Base;
                }
                else if (Config.ForceMainBattleSetInCombat || ModuleChecker.IsLoaded("Shokuho"))
                {
                    // Use main battle set if configured to do so.
                    eq = troop.Loadout.Battle.Base;
                }
                else
                {
                    var battleType = BattleType.FieldBattle;

                    var battle = new Battle();
                    if (battle.IsSiege)
                        battleType = battle.PlayerIsDefender
                            ? BattleType.SiegeDefense
                            : BattleType.SiegeAssault;

                    // Build eligible sets according to mask; battle set is always eligible.
                    var eligible = new List<WEquipment> { troop.Loadout.Battle };

                    foreach (var alt in troop.Loadout.Alternates)
                        if (!troop.IsCustom || CombatEquipmentBehavior.IsEnabled(troop, alt.Index, battleType))
                            eligible.Add(alt);

                    // Fallback safety: if somehow none, force battle set.
                    var pick = eligible.Count > 0 ? eligible : [troop.Loadout.Battle];

                    var idx = MBRandom.RandomInt(pick.Count);
                    eq = pick[idx].Base;
                }

                // Force the main battle set and prevent randomization/variants.
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
