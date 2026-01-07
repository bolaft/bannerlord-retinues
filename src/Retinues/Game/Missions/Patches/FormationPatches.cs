using System;
using HarmonyLib;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Utilities;
using SandBox.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Game.Missions.Patches
{
    [HarmonyPatch(typeof(Mission), nameof(Mission.GetAgentTroopClass))]
    internal static class Mission_GetAgentTroopClass_Patch
    {
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
                if (agentCharacter is not CharacterObject co)
                    return true;

                var w = WCharacter.Get(co);
                if (w == null)
                    return true;

                if (w.IsEdited == false)
                    return true; // skip vanilla

                var fc = w.FormationClassOverride;
                if (fc == FormationClass.Unset)
                    return true;

                // IMPORTANT: Mission.GetAgentTroopClass normally dismounts in sieges/sally-out.
                // If we override here, we must keep the same rule.
                if (
                    __instance.IsSiegeBattle
                    || (__instance.IsSallyOutBattle && battleSide == BattleSideEnum.Attacker)
                )
                    fc = fc.DismountedClass();

                __result = fc;
                return false; // skip vanilla
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
            for (int i = 0; i < __result.Count; i++)
            {
                var origin = __result[i].origin;
                var troop = origin?.Troop as CharacterObject;
                if (troop == null || troop.IsHero)
                    continue;

                var w = WCharacter.Get(troop);
                if (w == null || w.FormationClassOverride == FormationClass.Unset)
                    continue;

                // Use the effective class you already computed in UpdateFormationClass().
                var fc = w.FormationClass;

                // Keep vanilla siege/sally-out dismount semantics.
                if (
                    mission != null
                    && (
                        mission.IsSiegeBattle
                        || (mission.IsSallyOutBattle && battleSide == BattleSideEnum.Attacker)
                    )
                )
                    fc = fc.DismountedClass();

                // Initial spawn assignments only support formations 0..7.
                if ((int)fc >= 8)
                    fc = fc.DefaultClass(); // 0..3

                __result[i] = (origin, (int)fc);
            }
        }
    }

    [HarmonyPatch(typeof(BasicCharacterObject), nameof(BasicCharacterObject.HasMount))]
    internal static class BasicCharacterObject_HasMount_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix(BasicCharacterObject __instance, ref bool __result)
        {
            // Only enforce when your formation override is active.
            if (__instance is not CharacterObject co)
                return true;

            var w = WCharacter.Get(co);
            if (w == null || w.FormationClassOverride == FormationClass.Unset)
                return true;

            __result = w.IsMounted; // "virtual mounted"
            return false; // skip vanilla Equipment[10] check
        }
    }

    [HarmonyPatch(typeof(Agent), "get_IsRangedCached")]
    internal static class Agent_IsRangedCached_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix(Agent __instance, ref bool __result)
        {
            if (__instance?.Character is not CharacterObject co)
                return true;

            var w = WCharacter.Get(co);
            if (w == null || w.FormationClassOverride == FormationClass.Unset)
                return true;

            __result = w.IsRanged; // "virtual ranged"
            return false;
        }
    }

    internal static class VirtualClassHelper
    {
        public static bool TryGetVirtual(Agent a, out bool mounted, out bool ranged)
        {
            mounted = false;
            ranged = false;

            if (a?.Character is not CharacterObject co)
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
