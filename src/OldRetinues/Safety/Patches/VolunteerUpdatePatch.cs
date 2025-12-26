using System;
using HarmonyLib;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;

namespace OldRetinues.Safety.Patches
{
    /// <summary>
    /// Safety net for RecruitmentCampaignBehavior.UpdateVolunteersOfNotablesInSettlement.
    /// Prevents rare corrupted/null states (often mod-induced) from crashing campaign ticks.
    /// </summary>
    [HarmonyPatch(typeof(RecruitmentCampaignBehavior), "UpdateVolunteersOfNotablesInSettlement")]
    public static class VolunteerUpdateSafetyPatch
    {
        // Keep this extremely cheap; no heavy allocations in the normal case.
        [HarmonyPriority(Priority.First)]
        static bool Prefix(Settlement settlement)
        {
            try
            {
                if (settlement == null)
                    return false;

                // Guard settlement component graph (modded settlements can be weird).
                if (settlement.IsTown && settlement.Town == null)
                {
                    Log.Error(
                        $"VolunteerUpdateSafety: settlement '{settlement.StringId}' is Town but Town is null. Skipping update."
                    );
                    return false;
                }

                if (settlement.IsVillage)
                {
                    var v = settlement.Village;
                    if (v == null || v.Bound == null || v.Bound.Town == null)
                    {
                        Log.Error(
                            $"VolunteerUpdateSafety: settlement '{settlement.StringId}' is Village but Bound/Town is null. Skipping update."
                        );
                        return false;
                    }
                }

                // Validate notables cache.
                var notables = settlement.Notables;
                if (notables == null)
                {
                    Log.Error(
                        $"VolunteerUpdateSafety: settlement '{settlement.StringId}' Notables is null. Skipping update."
                    );
                    return false;
                }

                bool hasNullNotable = false;
                for (int i = 0; i < notables.Count; i++)
                {
                    if (notables[i] == null)
                    {
                        hasNullNotable = true;
                        break;
                    }
                }

                // If cache is dirty, try to rebuild it (private method).
                if (hasNullNotable)
                {
                    try
                    {
                        Reflector.InvokeMethod(
                            settlement,
                            "CollectNotablesToCache",
                            parameterTypes: null
                        );
                        notables = settlement.Notables;
                    }
                    catch (Exception e)
                    {
                        Log.Exception(
                            e,
                            $"VolunteerUpdateSafety: failed to rebuild notables cache for '{settlement.StringId}'. Skipping update."
                        );
                        return false;
                    }

                    for (int i = 0; i < notables.Count; i++)
                    {
                        if (notables[i] == null)
                        {
                            Log.Error(
                                $"VolunteerUpdateSafety: settlement '{settlement.StringId}' still has null notables after rebuild. Skipping update."
                            );
                            return false;
                        }
                    }
                }

                // Repair VolunteerTypes arrays (null elements are OK; null array / wrong length is not).
                for (int i = 0; i < notables.Count; i++)
                {
                    var hero = notables[i];
                    if (hero == null)
                        continue;

                    // Dead heroes can legitimately have VolunteerTypes == null (Hero.OnDeath()).
                    if (!hero.IsAlive)
                        continue;

                    // Only care about notables that can participate in recruit logic.
                    if (!hero.CanHaveRecruits)
                        continue;

                    var vt = hero.VolunteerTypes;
                    if (vt == null || vt.Length != 6)
                    {
                        var fixedArr = new CharacterObject[6];
                        if (vt != null)
                        {
                            int copy = Math.Min(6, vt.Length);
                            for (int k = 0; k < copy; k++)
                                fixedArr[k] = vt[k]; // elements may be null; that's fine
                        }

                        hero.VolunteerTypes = fixedArr;

                        Log.Warn(
                            $"VolunteerUpdateSafety: fixed VolunteerTypes array for notable '{hero.StringId}' in settlement '{settlement.StringId}'."
                        );
                    }
                }

                return true; // run vanilla
            }
            catch (Exception ex)
            {
                Log.Exception(
                    ex,
                    $"VolunteerUpdateSafety: suppressed exception in UpdateVolunteersOfNotablesInSettlement for settlement '{settlement?.StringId ?? "null"}'."
                );
                return true; // run vanilla despite error
            }
        }

        // Absolute safety net: suppress any exception thrown by vanilla or other patches.
        [HarmonyPriority(Priority.Last)]
        static Exception Finalizer(Exception __exception, Settlement settlement)
        {
            if (__exception == null)
                return null;

            try
            {
                Log.Exception(
                    __exception,
                    $"VolunteerUpdateSafety: suppressed exception in UpdateVolunteersOfNotablesInSettlement for settlement '{settlement?.StringId ?? "null"}'."
                );
            }
            catch { }

            return null; // suppress
        }
    }
}
