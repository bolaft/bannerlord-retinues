using HarmonyLib;
using Retinues.Game.Wrappers;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;

namespace Retinues.Features.Swaps.Patches
{
    /// <summary>
    /// Harmony postfix patch for caravan party creation.
    /// Swaps troops to match player faction logic after caravan is created.
    /// </summary>
    [HarmonyPatch(typeof(CaravanPartyComponent), "CreateCaravanParty")]
    static class CaravanSwap_Initialize_Postfix
    {
        static void Postfix(CaravanPartyComponent __instance, MobileParty __result)
        {
            if (__result == null)
                return; // should not happen

            var party = new WParty(__result);
            if (party.PlayerFaction == null)
                return; // no player faction

            // Hero-safe swap: caravans always have a hero leader
            party.MemberRoster?.SwapTroopsPreservingHeroes();
        }
    }

    /// <summary>
    /// Harmony postfix patch for militia party creation.
    /// Swaps troops to match player faction logic after militia is created.
    /// </summary>
    [HarmonyPatch(typeof(MilitiaPartyComponent), "CreateMilitiaParty")]
    static class MilitiaSwap_Initialize_Postfix
    {
        static void Postfix(MilitiaPartyComponent __instance, MobileParty __result)
        {
            if (__result == null)
                return; // should not happen

            var party = new WParty(__result);
            if (party.PlayerFaction == null)
                return; // no player faction
            party.MemberRoster?.SwapTroops();
        }
    }

    /// <summary>
    /// Harmony postfix patch for garrison party creation.
    /// Swaps troops to match player faction logic after garrison is created.
    /// </summary>
    [HarmonyPatch(typeof(GarrisonPartyComponent), "CreateGarrisonParty")]
    static class GarrisonSwap_Initialize_Postfix
    {
        static void Postfix(GarrisonPartyComponent __instance, MobileParty __result)
        {
            if (__result == null)
                return; // should not happen

            var party = new WParty(__result);
            if (party.PlayerFaction == null)
                return; // no player faction
            party.MemberRoster?.SwapTroops();
        }
    }
}
