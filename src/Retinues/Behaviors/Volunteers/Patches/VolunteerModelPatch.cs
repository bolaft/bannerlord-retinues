using System;
using System.Linq;
using System.Reflection;
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
        private static void Postfix(CampaignGameStarter __instance, GameModel __0)
        {
            try
            {
                if (__instance == null || __0 == null)
                    return;

                // Only react to VolunteerModel registrations.
                if (__0 is not VolunteerModel vm)
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

    /// <summary>
    /// Also re-wraps VolunteerModels registered via the GENERIC AddModel&lt;T&gt; overload.
    /// That overload adds straight to the model list and never routes through AddModel(GameModel),
    /// so the patch above cannot see it. Mods such as Adonnay's Troop Changer (used by De Re
    /// Militari) register their VolunteerModel this way — without this, their model becomes the
    /// last-registered VolunteerModel and wins, replacing our custom-troop injection with their
    /// own recruits (only their/vanilla troops appear in village recruit pools).
    /// </summary>
    [HarmonyPatch]
    internal static class Recruitement_CampaignGameStarter_AddModelGeneric_Patch
    {
        /// <summary>
        /// Targets the closed generic CampaignGameStarter.AddModel&lt;VolunteerModel&gt;.
        /// </summary>
        private static MethodBase TargetMethod()
        {
            var generic = typeof(CampaignGameStarter)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                    m.Name == nameof(CampaignGameStarter.AddModel) && m.IsGenericMethodDefinition
                );

            return generic?.MakeGenericMethod(typeof(VolunteerModel));
        }

        /// <summary>
        /// Postfix that re-wraps a generically-registered VolunteerModel with CustomVolunteerModel.
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(CampaignGameStarter __instance, object __0)
        {
            try
            {
                if (__instance == null)
                    return;

                // __0 is the MBGameModel<VolunteerModel> argument; a VolunteerModel IS one.
                if (__0 is not VolunteerModel vm)
                    return;

                // Prevent recursion / double-wrapping.
                if (vm is CustomVolunteerModel)
                    return;

                __instance.AddModel(new CustomVolunteerModel(vm));

                Log.Debug(
                    $"Recruitement: VolunteerModel (generic) re-wrapped (inner={vm.GetType().Name})."
                );
            }
            catch (Exception ex)
            {
                Log.Exception(
                    ex,
                    "Recruitement: CampaignGameStarter.AddModel<VolunteerModel> patch failed."
                );
            }
        }
    }
}
