using HarmonyLib;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Core.Features.Recruits.Patches
{
    [HarmonyPatch(
        typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.RecruitmentCampaignBehavior),
        "UpdateVolunteersOfNotablesInSettlement"
    )]
    public static class VolunteerSwap
    {
        [SafeMethod]
        static void Postfix(Settlement settlement)
        {
            var s = new WSettlement(settlement);
            s.SwapVolunteers();
        }
    }
}
