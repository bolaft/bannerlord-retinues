using System;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Game.Helpers;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.TroopSuppliers;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace OldRetinues.Features.Agents.Patches
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

        static void Postfix(UniqueTroopDescriptor troopDescriptor, ref CharacterObject __result)
        {
            try
            {
                // No mission => probably auto-resolve / simulation / non-mission use, skip.
                var mission = Mission.Current;

                if (!MissionHelper.IsCombatMission(mission))
                    return; // Combat only

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
