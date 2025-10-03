using HarmonyLib;
using Retinues.Core.Game.Wrappers;
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
                return; // should not happen

            var party = new WParty(__result);
            party.SwapTroops();
        }
    }

    [HarmonyPatch(typeof(MilitiaPartyComponent), "CreateMilitiaParty")]
    static class MilitiaSwap_Initialize_Postfix
    {
        static void Postfix(MilitiaPartyComponent __instance, MobileParty __result)
        {
            if (__result == null)
                return; // should not happen

            var party = new WParty(__result);
            party.SwapTroops();
        }
    }

    [HarmonyPatch(typeof(GarrisonPartyComponent), "CreateGarrisonParty")]
    static class GarrisonSwap_Initialize_Postfix
    {
        static void Postfix(GarrisonPartyComponent __instance, MobileParty __result)
        {
            if (__result == null)
                return; // should not happen

            var party = new WParty(__result);
            party.SwapTroops();
        }
    }
}
