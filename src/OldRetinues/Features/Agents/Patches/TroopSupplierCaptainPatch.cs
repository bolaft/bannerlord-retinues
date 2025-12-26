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
using TaleWorlds.ObjectSystem;

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
                var mission = Mission.Current;

                if (!MissionHelper.IsCombatMission(mission))
                    return;

                if (__result == null)
                    return;

                var id = __result.StringId;

                // Fast reject: only custom troops or known captain troops matter.
                if (!WCharacter.IsCustomId(id) && !WCharacter.IsCaptainId(id))
                    return;

                // If we got a captain id, normalize to base id for counting/enabled flag.
                if (WCharacter.TryGetBaseIdFromCaptainId(id, out var baseId))
                    id = baseId;

                // If this base troop doesn't have captains enabled, bail without wrapper allocation.
                if (!WCharacter.IsCaptainEnabledId(id))
                    return;

                // Reset per mission
                if (!ReferenceEquals(mission, _lastMission))
                {
                    _lastMission = mission;
                    _spawnCounts.Clear();
                }

                _spawnCounts.TryGetValue(id, out var count);
                count++;
                _spawnCounts[id] = count;

                if (CaptainFrequency <= 0 || count % CaptainFrequency != 0)
                    return;

                // Prefer cached captain CharacterObject
                if (WCharacter.TryGetCaptainObject(id, out var captainCo))
                {
                    __result = captainCo;
                    return;
                }

                // Captain not created yet: create lazily (rare path)
                var baseCo = MBObjectManager.Instance.GetObject<CharacterObject>(id);
                if (baseCo == null)
                    return;

                var baseTroop = new WCharacter(baseCo);

                if (!baseTroop.CanHaveCaptain)
                    return;

                var captain = baseTroop.Captain;
                if (captain?.Base == null)
                    return;

                __result = captain.Base;
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
