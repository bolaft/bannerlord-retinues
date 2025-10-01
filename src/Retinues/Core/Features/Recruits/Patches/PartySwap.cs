using HarmonyLib;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;

namespace Retinues.Core.Features.Recruits.Patches
{
    [HarmonyPatch(typeof(CaravanPartyComponent), "CreateCaravanParty")]
    static class CaravanSwap_Initialize_Postfix
    {
        static void Postfix(CaravanPartyComponent __instance, MobileParty __result)
        {
            if (__result == null)
            {
                Log.Debug("CaravanSwap: Party is null, skipping.");
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
                Log.Debug("MilitiaSwap: Party is null, skipping.");
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
                Log.Debug("GarrisonSwap: Party is null, skipping.");
                return;
            }
            TroopSwapHelper.SwapParty(__result);
        }
    }
}
