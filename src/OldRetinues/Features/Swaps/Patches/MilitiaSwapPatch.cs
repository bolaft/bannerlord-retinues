using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Features.Swaps.Patches
{
    /// <summary>
    /// Harmony patch for Settlement.AddMilitiasToParty.
    /// Spawns custom militia for player faction settlements if available, otherwise falls back to vanilla logic.
    /// </summary>
    [HarmonyPatch(typeof(Settlement), "AddMilitiasToParty")]
    internal static class PlayerMilitiaSpawnPatch
    {
        private static readonly MethodInfo Helper_RefInt = typeof(Settlement)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(m =>
            {
                if (m.Name != "AddTroopToMilitiaParty")
                    return false;
                var p = m.GetParameters();
                return p.Length == 5
                    && p[0].ParameterType == typeof(MobileParty)
                    && p[1].ParameterType == typeof(CharacterObject)
                    && p[2].ParameterType == typeof(CharacterObject)
                    && p[3].ParameterType == typeof(float)
                    && p[4].ParameterType.IsByRef
                    && p[4].ParameterType.GetElementType() == typeof(int);
            });

        private static bool IsValidChar(CharacterObject co) =>
            co != null && !co.IsHero && co.IsReady && co.IsInitialized;

        private static bool IsValid(WCharacter w) =>
            w != null && w.IsActive && !w.IsHero && w.Base != null && IsValidChar(w.Base);

        /// <summary>
        /// Adds a militia lane safely using reflection, updating remaining count.
        /// </summary>
        private static void AddLaneSafe(
            Settlement s,
            MobileParty party,
            CharacterObject basic,
            CharacterObject elite,
            float ratio,
            ref int remaining
        )
        {
            if (Helper_RefInt == null)
                return;
            if (remaining <= 0)
                return;

            // If both basic & elite are invalid, skip
            if (!IsValidChar(basic) && !IsValidChar(elite))
                return;

            try
            {
                object[] args = [party, basic, elite, ratio, remaining];
                Helper_RefInt.Invoke(s, args);
                remaining = (int)args[4];
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "AddLane failed (continuing with remaining lanes)");
            }
        }

        /// <summary>
        /// Prefix: replaces vanilla militia spawn with custom militia if available and valid.
        /// </summary>
        static bool Prefix(
            Settlement __instance,
            [HarmonyArgument(0)] MobileParty militaParty,
            [HarmonyArgument(1)] int militiaToAdd
        )
        {
            if (
                __instance == null
                || militaParty == null
                || militiaToAdd <= 0
                || Helper_RefInt == null
            )
                return true;

            try
            {
                var ws = new WSettlement(__instance);

                if (ws.PlayerFaction == null)
                    return true; // vanilla

                // Pull custom militia set
                var melee = ws.MilitiaMelee;
                var meleeElite = ws.MilitiaMeleeElite;
                var ranged = ws.MilitiaRanged;
                var rangedElite = ws.MilitiaRangedElite;

                // If any lane is missing/invalid, fall back to vanilla
                if (
                    !(
                        IsValid(melee)
                        && IsValid(meleeElite)
                        && IsValid(ranged)
                        && IsValid(rangedElite)
                    )
                )
                    return true;

                Campaign.Current.Models.SettlementMilitiaModel.CalculateMilitiaSpawnRate(
                    __instance,
                    out float meleeRatio,
                    out _
                );

                int remaining = militiaToAdd;

                AddLaneSafe(
                    __instance,
                    militaParty,
                    melee.Base,
                    meleeElite.Base,
                    meleeRatio,
                    ref remaining
                );
                AddLaneSafe(
                    __instance,
                    militaParty,
                    ranged.Base,
                    rangedElite.Base,
                    1f,
                    ref remaining
                );

                Log.Debug($"{ws.Name}: custom militia used (add={militiaToAdd}, rem={remaining}).");

                // skip original
                return false;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Fall back to vanilla");
                return true; // let vanilla proceed on any failure
            }
        }
    }
}
