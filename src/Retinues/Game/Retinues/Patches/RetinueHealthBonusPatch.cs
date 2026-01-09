using System;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Utilities;
using TaleWorlds.MountAndBlade;

namespace Retinues.Game.Retinues.Patches
{
    /// <summary>
    /// Applies the retinue health buff setting to spawned agents.
    /// </summary>
    [HarmonyPatch(typeof(Mission), "SpawnAgent")]
    internal static class RetinueHealthBonusPatches
    {
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

                var bonus = Settings.RetinueHealthBonus;
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
