using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;

namespace Retinues.Behaviors.Recruitement.Patches
{
    /// <summary>
    /// Manages player volunteer swap lifecycle when entering/exiting recruit screens.
    /// </summary>
    internal static class VolunteerSwapForPlayerPatches
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Enter Recruit Screen                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Targets various recruit-volunteers callbacks across versions to start swap for player.
        /// </summary>
        [HarmonyPatch]
        private static class RecruitVolunteers_OnConsequence_Patch
        {
            /// <summary>
            /// Returns candidate methods for hooking the recruit-volunteers consequence.
            /// </summary>
            private static IEnumerable<MethodBase> TargetMethods()
            {
                // 1) Older/alternate path: behavior methods
                var a = AccessTools.Method(
                    typeof(PlayerTownVisitCampaignBehavior),
                    "game_menu_recruit_volunteers_on_consequence",
                    new[] { typeof(MenuCallbackArgs) }
                );
                if (a != null)
                    yield return a;

                var b = AccessTools.Method(
                    typeof(PlayerTownVisitCampaignBehavior),
                    "game_menu_ui_recruit_volunteers_on_consequence",
                    new[] { typeof(MenuCallbackArgs) }
                );
                if (b != null)
                    yield return b;

                // 2) Common upstream path: GameMenuInitializationHandlers.PlayerTownVisit
                var handlerType = AccessTools.TypeByName(
                    "TaleWorlds.CampaignSystem.GameMenus.GameMenuInitializationHandlers.PlayerTownVisit"
                );

                if (handlerType != null)
                {
                    var c = AccessTools.Method(
                        handlerType,
                        "game_menu_recruit_volunteers_on_consequence",
                        new[] { typeof(MenuCallbackArgs) }
                    );
                    if (c != null)
                        yield return c;

                    var d = AccessTools.Method(
                        handlerType,
                        "game_menu_ui_recruit_volunteers_on_consequence",
                        new[] { typeof(MenuCallbackArgs) }
                    );
                    if (d != null)
                        yield return d;
                }
            }

            /// <summary>
            /// Prefix that begins the player recruit-menu swap state.
            /// </summary>
            [HarmonyPrefix]
            private static void Prefix(MenuCallbackArgs args)
            {
                try
                {
                    Log.Info("Recruitement: recruit_volunteers_on_consequence prefix HIT.");
                    PlayerVolunteerSwapState.TryBeginSwapForPlayerRecruitMenu();
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, "Recruitement: player recruit-volunteers patch failed.");
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Restore Snapshot                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Keep your restores (these already work for you; your probe hit town_on_init).
        /// <summary>
        /// Restores the player's volunteer snapshot on town menu init if active.
        /// </summary>
        [HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "game_menu_town_on_init")]
        [HarmonyPostfix]
        private static void Postfix_TownOnInit(MenuCallbackArgs args)
        {
            try
            {
                PlayerVolunteerSwapState.RestoreIfActive();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Recruitement: town_on_init restore patch failed.");
            }
        }

        /// <summary>
        /// Restores the player's volunteer snapshot on castle menu init if active.
        /// </summary>
        [HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "game_menu_castle_on_init")]
        [HarmonyPostfix]
        private static void Postfix_CastleOnInit(MenuCallbackArgs args)
        {
            try
            {
                PlayerVolunteerSwapState.RestoreIfActive();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Recruitement: castle_on_init restore patch failed.");
            }
        }

        /// <summary>
        /// Restores the player's volunteer snapshot on village menu init if active.
        /// </summary>
        [HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "game_menu_village_on_init")]
        [HarmonyPostfix]
        private static void Postfix_VillageOnInit(MenuCallbackArgs args)
        {
            try
            {
                PlayerVolunteerSwapState.RestoreIfActive();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Recruitement: village_on_init restore patch failed.");
            }
        }

        /// <summary>
        /// Restores the player's volunteer snapshot on settlement wait menu init if active.
        /// </summary>
        [HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "game_menu_settlement_wait_on_init")]
        [HarmonyPostfix]
        private static void Postfix_WaitOnInit(MenuCallbackArgs args)
        {
            try
            {
                PlayerVolunteerSwapState.RestoreIfActive();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Recruitement: settlement_wait_on_init restore patch failed.");
            }
        }
    }
}
