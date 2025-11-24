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

namespace Retinues.Features.Equipments.Patches
{
    [HarmonyPatch(typeof(Mission), "SpawnAgent")]
    internal static class Mission_SpawnAgent_Postfix
    {
        static void Postfix(
            AgentBuildData agentBuildData,
            bool spawnFromAgentVisuals,
            Agent __result
        )
        {
            try
            {
                var agent = __result;
                if (agent == null || agent.IsMount || agent.Character == null)
                    return;

                var troop = AgentHelper.TroopFromAgentBuildData(agentBuildData);
                if (troop == null)
                    return;

                if (troop.IsHero)
                    return; // still don't touch heroes

                if (!troop.IsCustom && !Config.EnableGlobalEditor)
                    return;

                var mission = Mission.Current;
                if (mission == null)
                    return;

                // Only affect allowed modes (same whitelist you already have)
                var modeWhiteList = new[]
                {
                    MissionMode.Battle,
                    MissionMode.Duel,
                    MissionMode.Deployment,
                    MissionMode.Stealth,
                };

                if (!modeWhiteList.Contains(mission.Mode))
                    return;

                // Avoid tournaments / arena (same checks you had in the Prefix)
                foreach (var behavior in mission.MissionBehaviors)
                {
                    if (behavior is TournamentBehavior)
                        return;

                    var name = behavior.GetType().FullName?.ToLowerInvariant() ?? string.Empty;
                    if (name.Contains("tournament") || name.Contains("arena"))
                        return;
                }

                // ── Pick the equipment, same logic as before ────────────────────── //

                Equipment eq = null;

                if (agentBuildData.AgentCivilianEquipment)
                {
                    var civs = troop.Loadout.CivilianSets.ToList();
                    if (civs.Count == 0)
                        eq = troop.Loadout.Civilian.Base;
                    else
                        eq = civs[MBRandom.RandomInt(civs.Count)].Base;
                }
                else if (Config.ForceMainBattleSetInCombat)
                {
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
                        eligible.Add(troop.Loadout.Battle); // safety

                    eq = eligible[MBRandom.RandomInt(eligible.Count)].Base;
                }

                if (eq == null)
                    return;

                // ── Finally, override whatever Shokuho (or others) did ─────────── //

                agent.UpdateSpawnEquipmentAndRefreshVisuals(eq);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
