using System;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Behaviors.Doctrines.Catalogs;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Settings;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.TroopSuppliers;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Behaviors.Agents.Patches
{
    /// <summary>
    /// Whenever the mission asks PartyGroupTroopSupplier for a troop from the battle roster,
    /// 1 out of every N occurrences of a given base type is swapped to its captain variant (if enabled).
    /// </summary>
    [HarmonyPatch(typeof(PartyGroupTroopSupplier), "GetTroop")]
    internal static class PartyGroupTroopSupplier_GetTroop_CaptainsPatch
    {
        private static Mission _lastMission;
        private static readonly Dictionary<string, int> _spawnCounts = [];

        [HarmonyPostfix]
        private static void Postfix(
            UniqueTroopDescriptor troopDescriptor,
            ref CharacterObject __result
        )
        {
            try
            {
                if (DoctrineCatalog.Captains?.IsAcquired != true)
                    return; // Feature disabled.

                var mission = Mission.Current;

                // Only affect combat missions.
                if (!AgentSpawnResolver.IsCombatMission)
                    return;

                if (__result == null || __result.IsHero)
                    return;

                var wc = WCharacter.Get(__result);
                if (wc == null)
                    return;

                // Normalize: count by BASE troop id (captain spawns should still count towards the base pool).
                var baseWc = wc.NonVariantBase();
                if (baseWc == null || baseWc.Base == null || baseWc.IsHero)
                    return;

                // No auto-creation: only swap if a captain variant already exists AND is enabled.
                var captain = baseWc.Captain;
                if (captain == null || captain.Base == null)
                    return;

                if (!captain.IsCaptainEnabled)
                    return;

                // Reset per mission.
                if (!ReferenceEquals(mission, _lastMission))
                {
                    _lastMission = mission;
                    _spawnCounts.Clear();
                }

                var id = baseWc.StringId;

                _spawnCounts.TryGetValue(id, out var count);
                count++;
                _spawnCounts[id] = count;

                int spawnRate = Configuration.CaptainSpawnRate;

                if (spawnRate <= 0 || (count % spawnRate) != 0)
                    return;

                __result = captain.Base;
            }
            catch (Exception e)
            {
                Log.Exception(e, "Captain troop swap failed.");
            }
        }
    }
}
