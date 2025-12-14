using System;
using HarmonyLib;
using Retinues.Safety.Sanitizer;
using Retinues.Utils;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;

namespace OldRetinues.Safety.Patches
{
    /// <summary>
    /// Patch for DefaultPartyTrainingModel.GetEffectiveDailyExperience to sanitize
    /// the party on exception. Prevents bad roster data from killing the daily tick.
    /// </summary>
    [HarmonyPatch(typeof(DefaultPartyTrainingModel), "GetEffectiveDailyExperience")]
    public static class DefaultPartyTrainingModel_GetEffectiveDailyExperience_Diag
    {
        // Harmony finalizer: runs after the original method.
        // If __exception is non-null, the original crashed.
        static Exception Finalizer(
            MobileParty mobileParty,
            TroopRosterElement troop,
            Exception __exception
        )
        {
            if (__exception == null)
                return null;

            try
            {
                Log.Error(
                    $"GetEffectiveDailyExperience crashed for party "
                        + $"{mobileParty?.Name} ({mobileParty?.StringId}), "
                        + $"troop={troop.Character?.StringId}: {__exception}"
                );

                if (mobileParty != null)
                    PartySanitizer.SanitizeParty(mobileParty);
            }
            catch (Exception exDump)
            {
                Log.Error($"Training dump failed: {exDump}");
            }

            // Swallow the original exception so campaign tick can continue.
            // XP for this troop/day will effectively be 0, which is safe.
            return null;
        }
    }
}
