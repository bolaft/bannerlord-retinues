using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Party;
using Retinues.Core.Utils;

namespace Retinues.Core.Safety
{
    [HarmonyPatch(typeof(MobileParty), "get_TotalWage")]
    public static class MobileParty_TotalWage_Diag
    {
        static Exception Finalizer(MobileParty __instance, Exception __exception)
        {
            if (__exception == null) return null;

            try
            {
                RosterSanitizer.CleanParty(__instance);
            }
            catch (Exception exDump)
            {
                Log.Error($"[WageDiag] Dump failed: {exDump}");
            }

            // keep original behavior (still crash) but now with one precise dump
            return null;
        }
    }
}
