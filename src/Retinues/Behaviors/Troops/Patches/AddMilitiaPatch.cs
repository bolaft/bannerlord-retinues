using System;
using HarmonyLib;
using Retinues.Behaviors.Doctrines.Catalogs;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Behaviors.Troops.Patches
{
    /// <summary>
    /// Patch Settlement.AddMilitiasToParty to use custom militia lanes instead of culture lanes
    /// when the doctrine is acquired. Falls back to vanilla on any failure.
    /// </summary>
    [SafeClass]
    [HarmonyPatch(typeof(Settlement), "AddMilitiasToParty")]
    internal static class PlayerMilitiaSpawnPatch
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Reverse-patched helper                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

#if BL12
        /// <summary>
        /// Reverse-patched helper for BL12 signature.
        /// </summary>
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Settlement), "AddTroopToMilitiaParty")]
        private static void AddTroopToMilitiaParty(
            Settlement __instance,
            MobileParty militaParty,
            CharacterObject militiaTroop,
            CharacterObject eliteMilitiaTroop,
            float troopRatio,
            ref int numberToAddRemaining
        )
        {
            throw new NotImplementedException();
        }
#endif

#if BL13 || BL14
        /// <summary>
        /// Reverse-patched helper for BL13 signature.
        /// </summary>
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Settlement), "AddTroopToMilitiaParty")]
        private static void AddTroopToMilitiaParty(
            Settlement __instance,
            MobileParty militiaParty,
            CharacterObject militiaTroop,
            CharacterObject veteranMilitiaTroop,
            float troopRatio,
            int militiaNumberToAdd
        )
        {
            throw new NotImplementedException();
        }
#endif

        /// <summary>
        /// Determines if the given CharacterObject is valid for militia use.
        /// </summary>
        private static bool IsValid(CharacterObject co) =>
            co != null && !co.IsHero && co.IsReady && co.IsInitialized;

        /// <summary>
        /// Determines if the given WCharacter is valid for militia use.
        /// </summary>
        private static bool IsValid(WCharacter wc) => IsValid(wc?.Base) && !wc.IsHero;

        /// <summary>
        /// Attempts to get a faction with militia roster from the settlement's clan or kingdom.
        /// </summary>
        private static bool TryGetFaction(WSettlement ws, out IBaseFaction faction)
        {
            faction = null;
            if (ws == null)
                return false;

            static bool HasMilitia(IBaseFaction f) =>
                f != null && f.RosterMilitia != null && f.RosterMilitia.Count > 0;

            if (HasMilitia(ws.Clan))
            {
                faction = ws.Clan;
                return true;
            }

            if (HasMilitia(ws.Kingdom))
            {
                faction = ws.Kingdom;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Prefix patch for Settlement.AddMilitiasToParty to use custom militia troops.
        /// </summary>
        private static bool Prefix(
            Settlement __instance,
            [HarmonyArgument(0)] MobileParty militaParty,
            [HarmonyArgument(1)] int militiaToAdd
        )
        {
            if (DoctrineCatalog.StalwartMilitia?.IsAcquired == false)
                return true; // Use vanilla if doctrine not acquired.

            if (__instance == null || militaParty == null || militiaToAdd <= 0)
                return true; // Use vanilla on invalid input.

            try
            {
                var ws = WSettlement.Get(__instance);
                if (!TryGetFaction(ws, out var faction))
                    return true;

                var melee = faction.MeleeMilitiaTroop;
                var meleeElite = faction.MeleeEliteMilitiaTroop;
                var ranged = faction.RangedMilitiaTroop;
                var rangedElite = faction.RangedEliteMilitiaTroop;

                // Fail open to vanilla if overrides are incomplete/invalid.
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
                    out float rangedRatio
                );

#if BL12
                int remaining = militiaToAdd;

                // BL12 vanilla: ranged lane uses 1f, and "remaining" is decremented by ref.
                AddTroopToMilitiaParty(
                    __instance,
                    militaParty,
                    melee.Base,
                    meleeElite.Base,
                    meleeRatio,
                    ref remaining
                );
                AddTroopToMilitiaParty(
                    __instance,
                    militaParty,
                    ranged.Base,
                    rangedElite.Base,
                    1f,
                    ref remaining
                );
#else
                // BL13 vanilla: both lanes take the full count with their own ratios.
                AddTroopToMilitiaParty(
                    __instance,
                    militaParty,
                    melee.Base,
                    meleeElite.Base,
                    meleeRatio,
                    militiaToAdd
                );
                AddTroopToMilitiaParty(
                    __instance,
                    militaParty,
                    ranged.Base,
                    rangedElite.Base,
                    rangedRatio,
                    militiaToAdd
                );
#endif

                Log.Debug($"AddMilitiaPatch: custom militia used for {ws?.Name}.");
                return false;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Militia spawn patch failed; falling back to vanilla.");
                return true;
            }
        }
    }
}
