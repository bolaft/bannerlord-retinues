using HarmonyLib;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
#if BL13 || BL14
using Helpers;
#endif

namespace Retinues.Behaviors.Retinues.Patches
{
    /// <summary>
    /// Tracks context flags for special party-screen flows (create clan party screen).
    /// </summary>
    internal static class PartyScreenContext
    {
        // True while the "create clan party for hero" party screen is open.
        public static bool IsCreateClanPartyScreenActive;
    }

#if BL13 || BL14
    /// <summary>
    /// Sets/clears the create-clan-party context flag for BL1.3 party screens.
    /// </summary>
    [HarmonyPatch(typeof(PartyScreenHelper))]
    internal static class PartyScreenHelper_CreateClanParty_ContextPatch
    {
        /// <summary>
        /// Marks the create-clan-party screen active before opening it.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("OpenScreenAsCreateClanPartyForHero")]
        private static void Prefix_OpenScreenAsCreateClanPartyForHero()
        {
            PartyScreenContext.IsCreateClanPartyScreenActive = true;
        }

        /// <summary>
        /// Clears the create-clan-party context after the party screen closes.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("ClosePartyPresentation")]
        private static void Postfix_ClosePartyPresentation()
        {
            PartyScreenContext.IsCreateClanPartyScreenActive = false;
        }
    }
#else
    /// <summary>
    /// Sets/clears the create-clan-party context flag for older party screen manager types.
    /// </summary>
    [HarmonyPatch(typeof(PartyScreenManager))]
    internal static class PartyScreenManager_CreateClanParty_ContextPatch
    {
        /// <summary>
        /// Marks the create-clan-party screen active before opening it.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("OpenScreenAsCreateClanPartyForHero")]
        private static void Prefix_OpenScreenAsCreateClanPartyForHero()
        {
            PartyScreenContext.IsCreateClanPartyScreenActive = true;
        }

        /// <summary>
        /// Clears the create-clan-party context after the party screen closes.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("ClosePartyPresentation")]
        private static void Postfix_ClosePartyPresentation()
        {
            PartyScreenContext.IsCreateClanPartyScreenActive = false;
        }
    }
#endif

    /// <summary>
    /// Prevents retinue troops being transferred between two real parties and enforces safe transfer rules.
    /// </summary>
    [HarmonyPatch(typeof(PartyScreenLogic))]
    internal static class PartyScreenLogic_IsTroopTransferable_RetinueGuardPatch
    {
        /// <summary>
        /// Postfix that restricts transfers of retinues depending on screen context and owner parties.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("IsTroopTransferable")]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(
            PartyScreenLogic __instance,
            PartyScreenLogic.TroopType troopType,
            CharacterObject character,
            int side,
            ref bool __result
        )
        {
            try
            {
                // If vanilla (or another mod) already said "no", don't fight it.
                if (!__result)
                    return;

                if (character == null)
                    return;

                // Only care about normal party members; prisoners can stay vanilla.
                if (troopType != PartyScreenLogic.TroopType.Member)
                    return;

                var w = WCharacter.Get(character);
                if (!w.IsRetinue)
                    return;

                // Figure out which party owns THIS row, and what is on the other side.
                var rosterSide = (PartyScreenLogic.PartyRosterSide)side;

                PartyBase currentOwner =
                    rosterSide == PartyScreenLogic.PartyRosterSide.Left
                        ? __instance.LeftOwnerParty
                        : __instance.RightOwnerParty;

                PartyBase otherOwner =
                    rosterSide == PartyScreenLogic.PartyRosterSide.Left
                        ? __instance.RightOwnerParty
                        : __instance.LeftOwnerParty;

                bool currentIsReal = currentOwner != null;
                bool otherIsReal = otherOwner != null;
                bool currentIsMainParty =
                    currentOwner != null && currentOwner.MobileParty == MobileParty.MainParty;

                // Special case: creating a clan party for a hero.
                if (
                    PartyScreenContext.IsCreateClanPartyScreenActive
                    && __instance.LeftOwnerParty == null
                    && __instance.RightOwnerParty != null
                    && __instance.RightOwnerParty.MobileParty == MobileParty.MainParty
                )
                {
                    __result = false;
                    return;
                }

                // Case A: both sides are real parties (garrison, donate, create party, clan party, etc.)
                // → never allow retinues to transfer in such screens.
                if (currentIsReal && otherIsReal)
                {
                    if (currentIsMainParty || !currentIsMainParty)
                    {
                        __result = false;
                        return;
                    }
                }

                // Case B: retinue belongs to a non-main party (should not normally happen).
                // As a safety net, prevent further transfers so they don't spread.
                if (currentIsReal && !currentIsMainParty)
                {
                    __result = false;
                    return;
                }

                // Case C: only one real party + one dummy roster.
                // - Default main-party manage/dismiss screen: RightOwnerParty = MainParty, LeftOwnerParty = null
                // - Other "single-party" manage screens where the other side is dummy
                // In these, we leave __result as-is so retinues can be dismissed or reshuffled.
            }
            catch (System.Exception ex)
            {
                Log.Warning(
                    $"[Retinues] PartyScreenLogic.IsTroopTransferable retinue guard failed: {ex}"
                );
            }
        }
    }
}
