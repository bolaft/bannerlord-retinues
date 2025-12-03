using HarmonyLib;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Features.Volunteers.Patches
{
    /// <summary>
    /// Swaps volunteers in settlement notables to match player faction logic.
    /// </summary>
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
            var f = s.PlayerFaction;
            if (f != null)
                s.SwapVolunteers(f);
        }
    }
}
