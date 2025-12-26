#if BL12
using System;
using System.Linq;
using HarmonyLib;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Features.Swaps.Patches
{
    /// <summary>
    /// BL12 fix: vanilla caravan creation uses CharacterObject.All.First(...) with IsInfantry,
    /// which crashes if the culture's caravan guard was edited to be mounted (IsInfantry=false).
    /// We only intervene when the vanilla predicate has no match, then fall back safely.
    /// </summary>
    [HarmonyPatch(typeof(CaravanPartyComponent), "InitializeCaravanOnCreation")]
    internal static class CaravanPartyComponent_InitializeCaravanOnCreation_SpawnFix
    {
        static bool Prefix(
            CaravanPartyComponent __instance,
            MobileParty mobileParty,
            Hero caravanLeader,
            ItemRoster caravanItems,
            int troopToBeGiven,
            bool isElite
        )
        {
            // If vanilla will add a hero leader, it won't hit the crashing First(...).
            if (caravanLeader != null)
                return true;

            var culture = mobileParty?.Party?.Owner?.Culture;
            if (culture == null)
                return true;

            // If the vanilla predicate still matches something, do nothing.
            if (
                CharacterObject.All.Any(co =>
                    co.Occupation == Occupation.CaravanGuard
                    && co.IsInfantry
                    && co.Level == 26
                    && co.Culture == culture
                )
            )
            {
                return true;
            }

            try
            {
                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
                //                Re-implement vanilla init               //
                //    (only for the crash case, with safe guard pick)      //
                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

                // Matches vanilla InitializeCaravanProperties()
                mobileParty.Aggressiveness = 0f;

                int troopCount = troopToBeGiven;
                if (troopCount == 0)
                {
                    float num = 1f;
                    num =
                        (MBRandom.RandomFloat < 0.67f)
                            ? ((1f - MBRandom.RandomFloat * MBRandom.RandomFloat) * 0.5f + 0.5f)
                            : 1f;

                    int num2 = (int)(mobileParty.Party.PartySizeLimit * num);
                    if (num2 >= 10)
                        num2--;

                    troopCount = num2;
                }

                var settlement = __instance.Settlement;
                if (settlement == null)
                {
                    Log.Warn(
                        "Caravan spawn fix: __instance.Settlement is null; letting vanilla run."
                    );
                    return true;
                }

                PartyTemplateObject pt = isElite
                    ? settlement.Culture.EliteCaravanPartyTemplate
                    : settlement.Culture.CaravanPartyTemplate;

                mobileParty.InitializeMobilePartyAtPosition(
                    pt,
                    settlement.GatePosition,
                    troopCount
                );

                // Hero leader case was handled above; here we pick a safe caravan guard.
                CharacterObject guard =
                    // Prefer the culture's configured caravan guard (BL12 has it).
                    culture.CaravanGuard
                    // Otherwise, prefer any culture caravan guard matching vanilla except infantry.
                    ?? CharacterObject.All.FirstOrDefault(co =>
                        co.Occupation == Occupation.CaravanGuard
                        && co.Level == 26
                        && co.Culture == culture
                    )
                    // Last resort: any caravan guard of that culture.
                    ?? CharacterObject.All.FirstOrDefault(co =>
                        co.Occupation == Occupation.CaravanGuard && co.Culture == culture
                    );

                if (guard == null)
                {
                    Log.Error(
                        $"Caravan spawn fix: no caravan guard found for culture '{culture.StringId}'. Caravan will spawn without the extra guard."
                    );
                }
                else
                {
                    mobileParty.MemberRoster.AddToCounts(guard, 1, insertAtFront: true);

                    Log.Warn(
                        $"Caravan spawn fix: culture '{culture.StringId}' has no infantry caravan guard Level=26. "
                            + $"Using '{guard.StringId}' instead (IsInfantry={guard.IsInfantry}, IsMounted={guard.IsMounted})."
                    );
                }

                mobileParty.ActualClan = __instance.Owner?.Clan;
                mobileParty.Party.SetVisualAsDirty();
                mobileParty.InitializePartyTrade(
                    10000 + ((__instance.Owner?.Clan == Clan.PlayerClan) ? 5000 : 0)
                );

                if (caravanItems != null)
                {
                    mobileParty.ItemRoster.Add(caravanItems);
                    return false; // skip vanilla
                }

                // Vanilla pack animal seeding
                float cheapest = 10000f;
                ItemObject packAnimal = null;
                var mgr = MBObjectManager.Instance;
                if (mgr != null)
                {
                    foreach (var item in mgr.GetObjectTypeList<ItemObject>())
                    {
                        if (
                            item.ItemCategory == DefaultItemCategories.PackAnimal
                            && !item.NotMerchandise
                            && item.Value < cheapest
                        )
                        {
                            packAnimal = item;
                            cheapest = item.Value;
                        }
                    }
                }

                if (packAnimal != null)
                {
                    mobileParty.ItemRoster.Add(
                        new ItemRosterElement(
                            packAnimal,
                            (int)(mobileParty.MemberRoster.TotalManCount * 0.5f)
                        )
                    );
                }

                return false; // we handled it; skip vanilla
            }
            catch (Exception e)
            {
                Log.Exception(e, "Caravan spawn fix failed; letting vanilla run.");
                return true;
            }
        }
    }
}
#endif
