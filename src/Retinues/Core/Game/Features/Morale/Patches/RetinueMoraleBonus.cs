using HarmonyLib;
using Retinues.Core.Game.Features.Doctrines;
using Retinues.Core.Game.Features.Doctrines.Catalog;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Core.Game.Features.Morale.Patches
{
    [HarmonyPatch(typeof(DefaultPartyMoraleModel), nameof(DefaultPartyMoraleModel.GetEffectivePartyMorale))]
    internal static class RetinuePartyMoralePatch
    {
        static void Postfix(MobileParty mobileParty, ref ExplainedNumber __result)
        {
            if (!DoctrineAPI.IsDoctrineUnlocked<BoundByHonor>()) return;

            var bonus = __result.ResultNumber * (Player.Party.MemberRoster.RetinueRatio * 0.2f * 100);
            __result.Add(bonus, new TextObject("Retinue morale bonus (Bound by Honor)"));
        }
    }
}
