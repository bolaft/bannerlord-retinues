using System;
using HarmonyLib;
using Retinues.Core.Safety.Sanitizer;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Core.Safety.Patches
{
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
