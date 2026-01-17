using System;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Domain.Characters.Wrappers;
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
        /// <summary>
        /// One captain per N occurrences of a given base troop.
        /// </summary>
        private const int CaptainFrequency = 15;

        private static Mission _lastMission;
        private static readonly Dictionary<string, int> _spawnCounts = [];

        static void Postfix(UniqueTroopDescriptor troopDescriptor, ref CharacterObject __result)
        {
            try
            {
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
                var baseWc = wc.IsCaptain ? (wc.CaptainBase ?? wc) : wc;
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

                if (CaptainFrequency <= 0 || (count % CaptainFrequency) != 0)
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
