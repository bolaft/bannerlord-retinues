using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;

namespace Retinues.Game.Recruitement.Patches
{
    internal static class VolunteerSwapForPlayerPatches
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Enter Recruit Screen                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [HarmonyPatch]
        private static class RecruitVolunteers_OnConsequence_Patch
        {
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
