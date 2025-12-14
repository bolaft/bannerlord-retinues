using System;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace OldRetinues.Features.Formations.Patches
{
    /// <summary>
    /// Locks FormationQuerySystem.MainClass to a stable class for mostly-custom formations.
    /// </summary>
    [HarmonyPatch(typeof(FormationQuerySystem), "get_MainClass")]
    internal static class FormationQuerySystemPatch
    {
        private const float RecomputeDelaySeconds = 0.2f;

        private sealed class CacheEntry
        {
            public int LastUnitCount;
            public float LastUpdateTime;
            public bool HasForced;
            public FormationClass Forced;
        }

        private static readonly Dictionary<Formation, CacheEntry> _cache = [];
        private static Mission _cachedMission;

        static bool Prefix(FormationQuerySystem __instance, ref FormationClass __result)
        {
            try
            {
                if (Config.AllowFormationOverrides == false)
                    return true;

                var mission = Mission.Current;
                if (!ReferenceEquals(mission, _cachedMission))
                {
                    // New mission (or null) -> reset cache.
                    _cachedMission = mission;
                    _cache.Clear();
                }

                var formation = __instance?.Formation;
                if (formation == null)
                    return true;

                var entry = GetOrUpdateEntry(formation);

                if (entry.HasForced)
                {
                    __result = entry.Forced;
                    return false;
                }

                return true; // fall back to vanilla MainClass
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                return true;
            }
        }

        private static CacheEntry GetOrUpdateEntry(Formation formation)
        {
            var mission = Mission.Current;
            var currentTime = mission?.CurrentTime ?? 0f;
            var total = formation.CountOfUnits;

            if (!_cache.TryGetValue(formation, out var entry))
            {
                entry = new CacheEntry { LastUnitCount = total, LastUpdateTime = currentTime };

                TryRefreshForcedClass(formation, total, currentTime, entry);
                _cache[formation] = entry;
                return entry;
            }

            if (entry.LastUnitCount != total)
            {
                var dt = currentTime - entry.LastUpdateTime;
                if (dt >= RecomputeDelaySeconds || entry.LastUnitCount == 0)
                {
                    entry.LastUnitCount = total;
                    entry.LastUpdateTime = currentTime;
                    TryRefreshForcedClass(formation, total, currentTime, entry);
                }
            }

            return entry;
        }

        private static void TryRefreshForcedClass(
            Formation formation,
            int total,
            float currentTime,
            CacheEntry entry
        )
        {
            if (TryComputeForcedClass(formation, total, out var forced))
            {
                entry.HasForced = true;
                entry.Forced = forced;
            }
            else
            {
                entry.HasForced = false;
            }
        }

        private static bool TryComputeForcedClass(
            Formation formation,
            int total,
            out FormationClass forced
        )
        {
            forced = default;

            try
            {
                if (total <= 0)
                    return false;

                int customCount = 0;
                int inf = 0,
                    rng = 0,
                    cav = 0,
                    har = 0;

                foreach (var unit in formation.UnitsWithoutLooseDetachedOnes)
                {
                    var agent = unit as Agent;
                    var character = agent?.Character;
                    if (character == null)
                        continue;

                    if (IsRetinuesCustom(character))
                        customCount++;

                    switch (character.DefaultFormationClass)
                    {
                        case FormationClass.Ranged:
                            rng++;
                            break;
                        case FormationClass.Cavalry:
                            cav++;
                            break;
                        case FormationClass.HorseArcher:
                            har++;
                            break;
                        default:
                            inf++;
                            break;
                    }
                }

                var percent = customCount * 100 / total;
                if (percent < 70)
                    return false;

                forced = DominantClass(inf, rng, cav, har);

                var mission = Mission.Current;
                if (
                    mission != null
                    && (
                        mission.IsSiegeBattle
                        || (
                            mission.IsSallyOutBattle
                            && formation.Team?.Side == BattleSideEnum.Attacker
                        )
                    )
                )
                {
                    forced = forced.DismountedClass();
                }

                return true;
            }
            catch (Exception e)
            {
                Log.Exception(e);
                forced = FormationClass.Infantry;
                return false;
            }
        }

        private static FormationClass DominantClass(int inf, int rng, int cav, int har)
        {
            // Infantry > Ranged > Cavalry > HorseArcher tie-breaker.
            if (inf >= rng && inf >= cav && inf >= har)
                return FormationClass.Infantry;
            if (rng >= cav && rng >= har)
                return FormationClass.Ranged;
            if (cav >= har)
                return FormationClass.Cavalry;
            return FormationClass.HorseArcher;
        }

        private static bool IsRetinuesCustom(BasicCharacterObject c)
        {
            try
            {
                var id = c?.StringId;
                return id != null
                    && id.StartsWith(
                        Game.Wrappers.WCharacter.CustomIdPrefix,
                        StringComparison.Ordinal
                    );
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return false;
            }
        }
    }
}
