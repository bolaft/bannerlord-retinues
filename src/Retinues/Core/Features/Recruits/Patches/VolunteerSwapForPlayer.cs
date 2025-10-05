using HarmonyLib;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Core.Features.Recruits.Patches
{
    /// <summary>
    /// Harmony patch for recruit volunteers menu consequence.
    /// Swaps volunteers for player clan if RecruitAnywhere is enabled.
    /// </summary>
    [HarmonyPatch]
    internal static class VolunteerSwapForPlayer
    {
        /// <summary>
        /// Postfix: swaps volunteers in current settlement for player clan after recruit menu.
        /// </summary>
        [HarmonyPatch(
            typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.PlayerTownVisitCampaignBehavior),
            "game_menu_recruit_volunteers_on_consequence"
        )]
        internal static class VolunteerSwapForPlayer_Begin
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                if (!Config.GetOption<bool>("RecruitAnywhere"))
                    return;

                var settlement = Settlement.CurrentSettlement;
                if (settlement == null)
                    return;

                var s = new WSettlement(settlement);
                s.SwapVolunteers(Player.Clan);
            }
        }
    }
}
