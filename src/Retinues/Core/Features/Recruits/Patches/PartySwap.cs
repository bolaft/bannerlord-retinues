using HarmonyLib;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using Retinues.Core.Utils;

namespace Retinues.Core.Features.Recruits.Patches
{
    [HarmonyPatch(typeof(CaravanPartyComponent), "CreateCaravanParty")]
    static class CaravanSwap_Initialize_Postfix
    {
        static void Postfix(CaravanPartyComponent __instance, MobileParty __result)
        {
            if (__result == null)
            {
                Log.Info("CaravanSwap: Party is null, skipping.");
                return;
            }
            TroopSwapHelper.SwapParty(__result);
        }
    }

    [HarmonyPatch(typeof(MilitiaPartyComponent), "CreateMilitiaParty")]
    static class MilitiaSwap_Initialize_Postfix
    {
        static void Postfix(MilitiaPartyComponent __instance, MobileParty __result)
        {
            if (__result == null)
            {
                Log.Info("MilitiaSwap: Party is null, skipping.");
                return;
            }
            TroopSwapHelper.SwapParty(__result);
        }
    }

    [HarmonyPatch(typeof(GarrisonPartyComponent), "CreateGarrisonParty")]
    static class GarrisonSwap_Initialize_Postfix
    {
        static void Postfix(GarrisonPartyComponent __instance, MobileParty __result)
        {
            if (__result == null)
            {
                Log.Info("GarrisonSwap: Party is null, skipping.");
                return;
            }
            TroopSwapHelper.SwapParty(__result);
        }
    }
}
