using HarmonyLib;
using Retinues.Behaviors.Doctrines.Catalogs;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;

namespace Retinues.Behaviors.Troops.Patches
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
            if (DoctrineCatalog.RoadWardens.IsAcquired == false)
                return; // No need to swap if doctrine not acquired

            if (__result == null)
                return; // should not happen

            var party = WParty.Get(__result);
            if (party == null)
                return;

            Log.Debug(
                $"CaravanSwap_Initialize_Postfix: swapping caravan troops for party {party.Name}."
            );

            // Hero-safe swap: caravans always have a hero leader
            party.SwapTroops(filter: t => t.IsCaravan);
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
            if (DoctrineCatalog.StalwartMilitia.IsAcquired == false)
                return; // No need to swap if doctrine not acquired

            if (__result == null)
                return; // should not happen

            var party = WParty.Get(__result);
            if (party == null)
                return;

            Log.Debug(
                $"MilitiaSwap_Initialize_Postfix: swapping militia troops for party {party.Name}."
            );

            // Hero-safe swap: caravans always have a hero leader
            party.SwapTroops(filter: t => t.IsMilitia);
        }
    }

    /// <summary>
    /// Harmony postfix patch for villager party creation.
    /// Swaps troops to match player faction logic after a villager party is created.
    /// </summary>
    [HarmonyPatch(typeof(VillagerPartyComponent), "CreateVillagerParty")]
    static class VillagerSwap_Initialize_Postfix
    {
        static void Postfix(VillagerPartyComponent __instance, MobileParty __result)
        {
            if (DoctrineCatalog.ArmedPeasantry.IsAcquired == false)
                return; // No need to swap if doctrine not acquired

            if (__result == null)
                return; // should not happen

            var party = WParty.Get(__result);
            if (party == null)
                return;

            Log.Debug(
                $"VillagerSwap_Initialize_Postfix: swapping villager troops for party {party.Name}."
            );

            // Hero-safe swap: caravans always have a hero leader
            party.SwapTroops(filter: t => t.IsVillager);
        }
    }
}
