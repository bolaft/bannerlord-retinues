using System;
using HarmonyLib;
using Retinues.Behaviors.Volunteers.Models;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;

namespace Retinues.Behaviors.Volunteers.Patches
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                   VolunteerModel Hook                  //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //
    // BL1.3 has two AddModel overloads, so we must target the
    // non-generic one explicitly to avoid Harmony ambiguity.
    //
    // This keeps RetinuesVolunteerModel as the last VolunteerModel even
    // if another mod adds/replaces VolunteerModel after our OnGameStart.

    /// <summary>
    /// Ensures VolunteerModel registrations are re-wrapped so custom roots are preferred.
    /// </summary>
    [HarmonyPatch(
        typeof(CampaignGameStarter),
        nameof(CampaignGameStarter.AddModel),
        [typeof(GameModel)]
    )]
    internal static class Recruitement_CampaignGameStarter_AddModel_Patch
    {
        /// <summary>
        /// Postfix that re-wraps VolunteerModel instances with CustomVolunteerModel.
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(CampaignGameStarter __instance, GameModel gameModel)
        {
            try
            {
                if (__instance == null || gameModel == null)
                    return;

                // Only react to VolunteerModel registrations.
                if (gameModel is not VolunteerModel vm)
                    return;

                // Prevent recursion / double-wrapping.
                if (vm is CustomVolunteerModel)
                    return;

                __instance.AddModel(new CustomVolunteerModel(vm));

                Log.Debug($"Recruitement: VolunteerModel re-wrapped (inner={vm.GetType().Name}).");
            }
            catch (Exception ex)
            {
                Log.Exception(
                    ex,
                    "Recruitement: CampaignGameStarter.AddModel(GameModel) patch failed."
                );
            }
        }
    }
}
