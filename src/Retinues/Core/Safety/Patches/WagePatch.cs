using System;
using HarmonyLib;
using Retinues.Core.Safety.Sanitizer;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Core.Safety.Patches
{
    /// <summary>
    /// Patch for MobileParty.TotalWage to sanitize party rosters on exception.
    /// Cleans up invalid party data and logs errors before allowing the crash.
    /// </summary>
    [HarmonyPatch(typeof(MobileParty), "get_TotalWage")]
    public static class MobileParty_TotalWage_Diag
    {
        static Exception Finalizer(MobileParty __instance, Exception __exception)
        {
            if (__exception == null)
                return null;

            try
            {
                RosterSanitizer.CleanParty(__instance);
            }
            catch (Exception exDump)
            {
                Log.Error($"Dump failed: {exDump}");
            }

            // keep original behavior (still crash) but now with one precise dump
            return null;
        }
    }
}
