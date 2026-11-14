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

                var wc = WCharacter.Get(__result.Character.StringId);
                int bonus = GetHealthBonus(wc, __result.BaseHealthLimit,
                    __result.Team, __instance?.PlayerTeam);

                if (bonus <= 0)
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

        internal static int GetHealthBonus(WCharacter troop, float baseHealth, Team team, Team playerTeam)
        {
            if (troop?.IsRetinue != true)
                return 0;

            int bonus = Configuration.RetinueHealthBonus;
            // The configured bonus applies to every retinue; the player's doctrine
            // applies only to the player's team. Troop identity alone is insufficient
            // because enemies can field captured or shared retinue troop types.
            // Mission.PlayerTeam also works before the main agent has spawned.
            if (DoctrineCatalog.Indomitable?.IsAcquired == true
                && playerTeam != null && team == playerTeam)
                bonus += (int)Math.Round(baseHealth * 0.1, MidpointRounding.AwayFromZero);
            return bonus;
        }
    }
}
