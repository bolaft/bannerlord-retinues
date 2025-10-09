using HarmonyLib;
using Retinues.Doctrines.Catalog;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Doctrines.Effects.Patches
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

            var party = new WParty(mobileParty);

            if (party != Player.Party)
                return; // player party only

            var bonus = __result.ResultNumber * (party.MemberRoster.RetinueRatio * 0.2f);
            __result.Add(
                bonus,
                L.T("retinue_morale_bonus_bound_by_honor", "Retinue (Bound by Honor)")
            );
        }
    }
}
