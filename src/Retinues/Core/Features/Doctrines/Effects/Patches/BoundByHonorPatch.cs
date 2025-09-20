using HarmonyLib;
using Retinues.Core.Features.Doctrines.Catalog;
using Retinues.Core.Game;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Core.Features.Doctrines.Effects.Patches
{
    [HarmonyPatch(
        typeof(DefaultPartyMoraleModel),
        nameof(DefaultPartyMoraleModel.GetEffectivePartyMorale)
    )]
    internal static class RetinuePartyMoralePatch
    {
        static void Postfix(MobileParty mobileParty, ref ExplainedNumber __result)
        {
            if (!DoctrineAPI.IsDoctrineUnlocked<BoundByHonor>())
                return;

            var bonus =
                __result.ResultNumber * (Player.Party.MemberRoster.RetinueRatio * 0.2f * 100);
            __result.Add(
                bonus,
                L.T("retinue_morale_bonus_bound_by_honor", "Retinue morale bonus (Bound by Honor)")
            );
        }
    }
}
