using System;
using System.Collections.Generic;
using System.Linq;
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
                if (troop.IsHero)
                    return; // Don't affect heroes

                if (!troop.IsCustom)
                    if (Config.GlobalEditorEnabled == false || ModuleChecker.IsLoaded("Shokuho"))
                        return; // Feature disabled for vanilla troops

                if (Mission.Current?.Mode == MissionMode.Tournament)
                    return; // Don't affect tournaments

                // Try to ensure we are not in a tournament or arena battle
                foreach (var behavior in Mission.Current?.MissionBehaviors)
                {
                    if (behavior is TournamentBehavior)
                        return;

                    if (behavior.GetType().FullName.ToLowerInvariant().Contains("tournament"))
                        return;

                    if (behavior.GetType().FullName.ToLowerInvariant().Contains("arena"))
                        return;
                }

                Equipment eq = null;

                if (agentBuildData.AgentCivilianEquipment)
                {
                    // Pick a civilian set if available
                    var civs = troop.Loadout.GetCivilianSets().ToList();
                    if (civs.Count == 0)
                        eq = troop.Loadout.Civilian.Base; // EnsureMinimumSets guarantees one exists
                    else
                        eq = civs[MBRandom.RandomInt(civs.Count)].Base;
                }
                else if (Config.ForceMainBattleSetInCombat)
                {
                    eq = troop.Loadout.Battle.Base; // index 0 is normalized to a battle set
                }
                else if (ModuleChecker.IsLoaded("Shokuho"))
                {
                    eq = troop.Loadout.Battle.Base; // force main battle set for Shokuho to avoid issues
                }
                else
                {
                    var battleType = BattleType.FieldBattle;
                    var battle = new Battle();
                    if (battle.IsSiege)
                        battleType = battle.PlayerIsDefender
                            ? BattleType.SiegeDefense
                            : BattleType.SiegeAssault;

                    var all = troop.Loadout.Equipments;
                    var eligible = new List<WEquipment>();

                    for (int i = 0; i < all.Count; i++)
                    {
                        var we = all[i];
                        if (we.IsCivilian)
                            continue;

                        if (CombatEquipmentBehavior.IsEnabled(troop, i, battleType))
                            eligible.Add(we);
                    }

                    if (eligible.Count == 0)
                        eligible.Add(troop.Loadout.Battle); // safety fallback

                    eq = eligible[MBRandom.RandomInt(eligible.Count)].Base;
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
