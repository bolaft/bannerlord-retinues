using System;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.TroopSuppliers;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Features.Agents.Patches
{
    /// <summary>
    /// Whenever the mission asks PartyGroupTroopSupplier for a troop
    /// from the battle roster, 1 out of every N of that base type is
    /// swapped to its captain CharacterObject.
    /// </summary>
    [HarmonyPatch(typeof(PartyGroupTroopSupplier), "GetTroop")]
    internal static class PartyGroupTroopSupplier_GetTroop_CaptainsPatch
    {
        // One captain per N occurrences of a given base troop
        private const int CaptainFrequency = 15;

        private static Mission _lastMission;
        private static readonly Dictionary<string, int> _spawnCounts = [];

        // Signature matches: internal CharacterObject GetTroop(UniqueTroopDescriptor troopDescriptor)
        static void Postfix(UniqueTroopDescriptor troopDescriptor, ref CharacterObject __result)
        {
            try
            {
                // No mission => probably auto-resolve / simulation / non-mission use, skip.
                var mission = Mission.Current;
                if (mission == null)
                    return;

                // Restrict to battle-like missions
                var mode = mission.Mode;
                if (
                    mode != MissionMode.Battle
                    && mode != MissionMode.Duel
                    && mode != MissionMode.Deployment
                    && mode != MissionMode.Stealth
                )
                    return;

                // Skip tournaments / arena missions
                foreach (var behavior in mission.MissionBehaviors)
                {
                    if (behavior is TournamentBehavior)
                        return;

                    var name = behavior.GetType().FullName?.ToLowerInvariant() ?? string.Empty;
                    if (name.Contains("tournament") || name.Contains("arena"))
                        return;
                }

                // If the supplier didn't find anything, nothing to do
                if (__result == null)
                    return;

                var troop = new WCharacter(__result);

                // Captains are only for custom non-heroes
                if (!troop.CanHaveCaptain)
                    return;

                // Always key off the base troop
                var baseTroop =
                    troop.IsCaptain && troop.BaseTroop != null ? troop.BaseTroop : troop;

                if (!baseTroop.CaptainEnabled)
                    return;

                var captain = baseTroop.Captain;
                if (captain == null)
                    return;

                // Reset per mission
                if (!ReferenceEquals(mission, _lastMission))
                {
                    _lastMission = mission;
                    _spawnCounts.Clear();
                }

                var key = baseTroop.StringId;
                _spawnCounts.TryGetValue(key, out var count);
                count++;
                _spawnCounts[key] = count;

                if (CaptainFrequency <= 0 || count % CaptainFrequency != 0)
                    return;

                __result = captain.Base; // CharacterObject
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
