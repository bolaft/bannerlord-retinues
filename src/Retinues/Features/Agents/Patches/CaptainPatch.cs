using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.TroopSuppliers;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Features.Agents.Patches
{
    /// <summary>
    /// Make 1 out of every N supplied troops of a given base type spawn as its captain.
    /// Works at the PartyGroupTroopSupplier level, so rosters are effectively "split"
    /// without touching the actual Party/PartyBase MemberRosters.
    /// </summary>
    [HarmonyPatch(typeof(PartyGroupTroopSupplier), "SupplyTroops")]
    internal static class PartyGroupTroopSupplier_SupplyTroops_CaptainsPatch
    {
        // One captain per N spawns of a given base troop
        private const int CaptainFrequency = 2;

        private static Mission _lastMission;
        private static readonly Dictionary<string, int> _spawnCounts = new();

        static void Postfix(ref IEnumerable<IAgentOriginBase> __result)
        {
            try
            {
                if (__result == null)
                    return;

                var mission = Mission.Current;
                if (mission == null)
                    return;

                // Only apply in "real" combat modes (same whitelist as your agent patch)
                List<MissionMode> modeWhiteList =
                [
                    MissionMode.Battle,
                    MissionMode.Duel,
                    MissionMode.Deployment,
                    MissionMode.Stealth,
                ];

                if (!modeWhiteList.Contains(mission.Mode))
                    return;

                // Skip tournaments / arenas
                foreach (var behavior in mission.MissionBehaviors)
                {
                    if (behavior is SandBox.Tournaments.MissionLogics.TournamentBehavior)
                        return;

                    var name = behavior.GetType().FullName?.ToLowerInvariant() ?? string.Empty;
                    if (name.Contains("tournament") || name.Contains("arena"))
                        return;
                }

                // Reset per-mission counters
                if (!ReferenceEquals(mission, _lastMission))
                {
                    _lastMission = mission;
                    _spawnCounts.Clear();
                }

                // We need a mutable collection
                var origins = __result.ToArray();
                bool anyChange = false;

                for (int i = 0; i < origins.Length; i++)
                {
                    var origin = origins[i];
                    if (origin == null)
                        continue;

                    var co = origin.Troop as CharacterObject;
                    if (co == null)
                        continue;

                    var troop = new WCharacter(co);

                    // No heroes
                    if (troop.IsHero)
                        continue;

                    // Only care about your custom troops
                    if (!troop.IsCustom)
                        continue;

                    // Always count per base troop
                    var baseTroop =
                        troop.IsCaptain && troop.BaseTroop != null ? troop.BaseTroop : troop;

                    var captain = baseTroop.Captain;
                    if (captain == null)
                        continue;

                    var key = baseTroop.StringId;
                    if (!_spawnCounts.TryGetValue(key, out var count))
                        count = 0;

                    count++;
                    _spawnCounts[key] = count;

                    if (CaptainFrequency <= 0 || count % CaptainFrequency != 0)
                        continue;

                    // This one becomes a captain
                    Log.Info(
                        $"[Captains] SupplyTroops: swapping {baseTroop.StringId} -> captain {captain.StringId} (#{count}) ({captain.Name}) ({captain.IsFemale})"
                    );

                    origins[i] = new CaptainAgentOrigin(origin, captain.Base);
                    anyChange = true;
                }

                if (anyChange)
                {
                    __result = origins;
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
