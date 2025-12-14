using System;
using HarmonyLib;
using Retinues.Doctrines.Catalog;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace OldRetinues.Doctrines.Effects.Patches
{
    /// <summary>
    /// Harmony patch for DefaultPartyMoraleModel.GetEffectivePartyMorale.
    /// Adds a morale bonus for retinue ratio if Bound by Honor doctrine is unlocked.
    /// </summary>
    [HarmonyPatch(
        typeof(DefaultPartyMoraleModel),
        nameof(DefaultPartyMoraleModel.GetEffectivePartyMorale)
    )]
    internal static class RetinuePartyMoralePatch
    {
        [SafeMethod]
        static void Postfix(MobileParty mobileParty, ref ExplainedNumber __result)
        {
            if (!DoctrineAPI.IsDoctrineUnlocked<BoundByHonor>())
                return;

            if (!mobileParty.IsMainParty)
                return; // player party only

            var party = new WParty(mobileParty);

            var roster = party.MemberRoster;
            if (roster == null)
                return;

            // Use a non-negative base morale for percentage-based bonus so negative
            // morale values don't invert the intended positive bonus.
            var baseMorale = Math.Max(0f, __result.ResultNumber);
            var bonus = baseMorale * (roster.RetinueRatio * 0.2f);

            if (bonus <= 0f)
                return;

            __result.Add(
                bonus,
                L.T("retinue_morale_bonus_bound_by_honor", "Retinue (Bound by Honor)")
            );
        }
    }
}
