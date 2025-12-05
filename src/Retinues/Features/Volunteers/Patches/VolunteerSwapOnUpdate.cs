using HarmonyLib;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Features.Volunteers.Patches
{
    /// <summary>
    /// Harmony patch for UpdateVolunteersOfNotablesInSettlement.
    /// Ensures that player-sphere settlements always have fully swapped
    /// (custom) volunteers for notables. This is the canonical state used
    /// by AI, auto-recruit, and mods like Improved Garrisons.
    /// </summary>
    [HarmonyPatch(
        typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.RecruitmentCampaignBehavior),
        "UpdateVolunteersOfNotablesInSettlement"
    )]
    public static class VolunteerSwapOnUpdate
    {
        [SafeMethod]
        static void Postfix(Settlement settlement)
        {
            if (settlement == null)
                return;

            var s = new WSettlement(settlement);

            // Only touch settlements in the player's "sphere"
            // (player clan / kingdom fiefs).
            if (s.PlayerFaction == null)
                return;

            // Always fully swap volunteers to the current player-sphere tree.
            s.SwapVolunteers();
        }
    }
}
