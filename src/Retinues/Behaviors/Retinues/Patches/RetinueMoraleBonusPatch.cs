using System;
using HarmonyLib;
using Retinues.Behaviors.Doctrines.Catalogs;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Interface.Services;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Behaviors.Retinues.Patches
{
    /// <summary>
    /// Harmony patch for DefaultPartyMoraleModel.GetEffectivePartyMorale.
    /// Adds a morale bonus for retinue ratio if Bound by Honor doctrine is unlocked.
    /// </summary>
    [HarmonyPatch(
        typeof(DefaultPartyMoraleModel),
        nameof(DefaultPartyMoraleModel.GetEffectivePartyMorale)
    )]
    internal static class RetinueMoraleBonusPatch
    {
        /// <summary>
        /// Postfix that adds retinue morale bonus.
        /// </summary>
        static void Postfix(MobileParty mobileParty, ref ExplainedNumber __result)
        {
            if (!DoctrineCatalog.BoundByHonor.IsAcquired)
                return; // Feature disabled.

            if (!mobileParty.IsMainParty)
                return; // player party only

            var party = WParty.Get(mobileParty);

            // Use a non-negative base morale for percentage-based bonus so negative
            // morale values don't invert the intended positive bonus.
            var baseMorale = Math.Max(0f, __result.ResultNumber);
            var bonus = baseMorale * (party.RetinueRatio * 0.2f);

            if (bonus <= 0f)
                return;

            __result.Add(bonus, L.T("retinue_morale_bonus_bound_by_honor", "Bound by Honor"));
        }
    }
}
