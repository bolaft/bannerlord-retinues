using System;
using HarmonyLib;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Features.Formations.Patches
{
    /// <summary>
    /// Locks FormationQuerySystem.MainClass (PhysicalClass) to the intended class
    /// for specific formations (e.g., those composed of Retinues-tagged troops).
    /// This keeps UI text ("Infantry: charge!") and AI behaviors consistent.
    /// </summary>
    [HarmonyPatch(typeof(FormationQuerySystem), "get_MainClass")]
    internal static class FormationQuerySystemPatch
    {
        // Policy: when should a formation be "forced"?
        private static bool TryGetForcedClass(FormationQuerySystem fqs, out FormationClass forced)
        {
            forced = default;

            try
            {
                var formation = fqs.Formation;
                if (formation == null)
                    return false;

                // If all (or a large majority) agents in the formation are custom troops.
                int total = formation.CountOfUnits;
                if (total <= 0)
                    return false;

                int customCount = 0;
                foreach (var a in formation.UnitsWithoutLooseDetachedOnes)
                {
                    var agent = a as Agent;
                    var isCustom = IsRetinuesCustom(agent?.Character);
                    if (isCustom)
                        customCount++;
                }

                var percent = customCount * 100 / total;
                if (percent < 70)
                    return false;

                forced = formation.ArrangementOrder.OrderEnum switch
                {
                    _ => DominantDefaultClass(formation),
                };

                // Apply vanilla siege/sally dismount mapping so VO stays sensible.
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
                return false;
            }
        }

        private static FormationClass DominantDefaultClass(Formation formation)
        {
            try
            {
                // Vote by troop DefaultFormationClass.
                int inf = 0,
                    rng = 0,
                    cav = 0,
                    har = 0;
                foreach (var a in formation.UnitsWithoutLooseDetachedOnes)
                {
                    var agent = a as Agent;
                    var c = agent?.Character;
                    if (c is null)
                        continue;

                    switch (c.DefaultFormationClass)
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

                // Tie-breaker: Infantry > Ranged > Cavalry > HorseArcher
                if (inf >= rng && inf >= cav && inf >= har)
                {
                    return FormationClass.Infantry;
                }
                if (rng >= cav && rng >= har)
                {
                    return FormationClass.Ranged;
                }
                if (cav >= har)
                {
                    return FormationClass.Cavalry;
                }
                return FormationClass.HorseArcher;
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return FormationClass.Infantry; // fallback on error
            }
        }

        private static bool IsRetinuesCustom(BasicCharacterObject c)
        {
            try
            {
                var id = c?.StringId;
                var res =
                    id != null
                    && id.StartsWith(WCharacter.CustomIdPrefix, StringComparison.Ordinal);
                return res;
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return false;
            }
        }

        // Prefix: if a value is supplied, skip original.
        static bool Prefix(FormationQuerySystem __instance, ref FormationClass __result)
        {
            try
            {
                if (TryGetForcedClass(__instance, out var forced))
                {
                    __result = forced;
                    return false; // skip vanilla ratio-based MainClass
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
            return true; // let vanilla compute
        }
    }
}
