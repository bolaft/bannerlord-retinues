using System;
using HarmonyLib;
using Retinues.Behaviors.Doctrines.Catalogs;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Settings;
using Retinues.Utilities;
using TaleWorlds.MountAndBlade;

namespace Retinues.Behaviors.Retinues.Patches
{
    /// <summary>
    /// Applies configured retinue health bonus to spawned retinue agents.
    /// </summary>
    [HarmonyPatch(typeof(Mission), "SpawnAgent")]
    internal static class RetinueHealthBonusPatches
    {
        /// <summary>
        /// Postfix that increases health for retinue agents based on settings.
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(
            Mission __instance,
            AgentBuildData agentBuildData,
            bool spawnFromAgentVisuals,
            Agent __result
        )
        {
            try
            {
                if (__result == null || __result.Character == null)
                    return;

                if (!__result.IsHuman)
                    return;

                int bonus = Configuration.RetinueHealthBonus;

                // +10% retinue health bonus from Indomitable doctrine
                if (DoctrineCatalog.Indomitable?.IsAcquired == true)
                    bonus += (int)
                        Math.Round(__result.BaseHealthLimit * 0.1, MidpointRounding.AwayFromZero);

                if (bonus <= 0)
                    return;

                var wc = WCharacter.Get(__result.Character.StringId);
                if (wc == null || !wc.IsRetinue)
                    return;

                __result.BaseHealthLimit += bonus;
                __result.HealthLimit = __result.BaseHealthLimit;
                __result.Health = __result.HealthLimit;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Retinue health buff failed.");
            }
        }
    }
}
