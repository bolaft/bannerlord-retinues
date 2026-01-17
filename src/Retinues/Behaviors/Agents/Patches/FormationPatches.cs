using System;
using HarmonyLib;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Utilities;
using SandBox.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Behaviors.Agents.Patches
{
    /// <summary>
    /// Helper to determine when formation override behavior should be applied.
    /// </summary>
    internal static class FormationOverrideContext
    {
        /// <summary>
        /// Returns true when formation overrides are active for the given mission.
        /// </summary>
        public static bool IsActive(Mission mission = null)
        {
            var m = mission ?? Mission.Current;
            if (m == null)
                return false;

            // Only apply during actual combat and the deployment screen.
            // This prevents UI / character editor / inventory preview missions from being affected.
            return m.Mode == MissionMode.Battle || m.Mode == MissionMode.Deployment;
        }
    }

    [HarmonyPatch(typeof(Mission), nameof(Mission.GetAgentTroopClass))]
    internal static class Mission_GetAgentTroopClass_Patch
    {
        /// <summary>
        /// Prefix patch to override an agent's formation class based on edited character data.
        /// </summary>
        [HarmonyPrefix]
        private static bool Prefix(
            Mission __instance,
            BattleSideEnum battleSide,
            BasicCharacterObject agentCharacter,
            ref FormationClass __result
        )
        {
            try
            {
                if (!FormationOverrideContext.IsActive(__instance))
                    return true;

                if (agentCharacter is not CharacterObject co)
                    return true;

                var w = WCharacter.Get(co);
                if (w == null)
                    return true;

                if (w.IsEdited == false)
                    return true;

                var fc = w.FormationClassOverride;
                if (fc == FormationClass.Unset)
                    return true;

                // Keep vanilla siege/sally-out dismount semantics.
                if (
                    __instance.IsSiegeBattle
                    || (__instance.IsSallyOutBattle && battleSide == BattleSideEnum.Attacker)
                )
                    fc = fc.DismountedClass();

                __result = fc;
                return false;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Mission.GetAgentTroopClass override failed.");
                return true;
            }
        }
    }

    [HarmonyPatch(
        typeof(SandboxBattleSpawnModel),
        nameof(SandboxBattleSpawnModel.GetInitialSpawnAssignments)
    )]
    public static class Patch_SandboxBattleSpawnModel_GetInitialSpawnAssignments
    {
        /// <summary>
        /// Postfix that adjusts initial spawn assignments to honor edited formation classes.
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(
            BattleSideEnum battleSide,
            System.Collections.Generic.List<IAgentOriginBase> troopOrigins,
            ref System.Collections.Generic.List<(
                IAgentOriginBase origin,
                int formationIndex
            )> __result
        )
        {
            if (__result == null || __result.Count == 0)
                return;

            var mission = Mission.Current;
            if (!FormationOverrideContext.IsActive(mission))
                return;

            for (int i = 0; i < __result.Count; i++)
            {
                var origin = __result[i].origin;
                var troop = origin?.Troop as CharacterObject;
                if (troop == null || troop.IsHero)
                    continue;

                var w = WCharacter.Get(troop);
                if (w == null || w.FormationClassOverride == FormationClass.Unset)
                    continue;

                var fc = w.FormationClass;

                if (
                    mission != null
                    && (
                        mission.IsSiegeBattle
                        || (mission.IsSallyOutBattle && battleSide == BattleSideEnum.Attacker)
                    )
                )
                    fc = fc.DismountedClass();

                if ((int)fc >= 8)
                    fc = fc.DefaultClass();

                __result[i] = (origin, (int)fc);
            }
        }
    }

    [HarmonyPatch(typeof(BasicCharacterObject), nameof(BasicCharacterObject.HasMount))]
    internal static class BasicCharacterObject_HasMount_Patch
    {
        /// <summary>
        /// Prefix to report mount availability based on edited character settings.
        /// </summary>
        [HarmonyPrefix]
        private static bool Prefix(BasicCharacterObject __instance, ref bool __result)
        {
            // Avoid UI / preview contexts.
            if (!FormationOverrideContext.IsActive())
                return true;

            if (__instance is not CharacterObject co)
                return true;

            var w = WCharacter.Get(co);
            if (w == null || w.FormationClassOverride == FormationClass.Unset)
                return true;

            __result = w.IsMounted;
            return false;
        }
    }

    /// <summary>
    /// Utility for deriving virtual class attributes (mounted/ranged) for an agent.
    /// </summary>
    internal static class VirtualClassHelper
    {
        /// <summary>
        /// Attempts to determine virtual mounted/ranged flags for the given agent.
        /// </summary>
        public static bool TryGetVirtual(Agent a, out bool mounted, out bool ranged)
        {
            mounted = false;
            ranged = false;

            if (a == null)
                return false;

            // Avoid UI / preview contexts.
            if (!FormationOverrideContext.IsActive(a.Mission))
                return false;

            if (a.Character is not CharacterObject co)
                return false;

            var w = WCharacter.Get(co);
            if (w == null || w.FormationClassOverride == FormationClass.Unset)
                return false;

            mounted = w.IsMounted;
            ranged = w.IsRanged;
            return true;
        }
    }

    [HarmonyPatch(typeof(QueryLibrary), nameof(QueryLibrary.IsInfantry))]
    internal static class QueryLibrary_IsInfantry_Patch
    {
        /// <summary>
        /// Prefix that treats an agent as infantry based on virtual class flags.
        /// </summary>
        [HarmonyPrefix]
        private static bool Prefix(Agent a, ref bool __result)
        {
            if (!VirtualClassHelper.TryGetVirtual(a, out var m, out var r))
                return true;

            __result = !m && !r;
            return false;
        }
    }

    [HarmonyPatch(typeof(QueryLibrary), nameof(QueryLibrary.IsRanged))]
    internal static class QueryLibrary_IsRanged_Patch
    {
        /// <summary>
        /// Prefix that treats an agent as ranged based on virtual class flags.
        /// </summary>
        [HarmonyPrefix]
        private static bool Prefix(Agent a, ref bool __result)
        {
            if (!VirtualClassHelper.TryGetVirtual(a, out var m, out var r))
                return true;

            __result = !m && r;
            return false;
        }
    }

    [HarmonyPatch(typeof(QueryLibrary), nameof(QueryLibrary.IsCavalry))]
    internal static class QueryLibrary_IsCavalry_Patch
    {
        /// <summary>
        /// Prefix that treats an agent as cavalry based on virtual class flags.
        /// </summary>
        [HarmonyPrefix]
        private static bool Prefix(Agent a, ref bool __result)
        {
            if (!VirtualClassHelper.TryGetVirtual(a, out var m, out var r))
                return true;

            __result = m && !r;
            return false;
        }
    }

    [HarmonyPatch(typeof(QueryLibrary), nameof(QueryLibrary.IsRangedCavalry))]
    internal static class QueryLibrary_IsRangedCavalry_Patch
    {
        /// <summary>
        /// Prefix that treats an agent as ranged cavalry based on virtual class flags.
        /// </summary>
        [HarmonyPrefix]
        private static bool Prefix(Agent a, ref bool __result)
        {
            if (!VirtualClassHelper.TryGetVirtual(a, out var m, out var r))
                return true;

            __result = m && r;
            return false;
        }
    }
}
