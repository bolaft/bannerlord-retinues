using System;
using HarmonyLib;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Behaviors.Troops.Patches
{
    /// <summary>
    /// Patches for quest-related parties where troops need to be swapped to match player faction logic.
    /// </summary>
    [SafeClass]
    internal static class QuestPatches
    {
        /// <summary>
        /// Swaps troops in the given mobile party according to its settlement's base troop faction.
        /// </summary>
        private static bool DoSwap(MobileParty mp, Func<WCharacter, bool> filter)
        {
            try
            {
                if (mp == null)
                    return false;

                var settlement = mp.HomeSettlement ?? mp.CurrentSettlement;
                if (settlement == null)
                    return false;

                var faction = WSettlement.Get(settlement)?.GetBaseTroopsFaction();
                if (faction == null)
                    return false;

                var party = WParty.Get(mp);
                party?.SwapTroops(faction, filter);

                return true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Guards party swap failed");
                return false;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //              GangLeaderNeedsWeapons Quest              //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Patch GangLeaderNeedsWeaponsIssueQuestBehavior.CreateGuardsParty to swap troops after creation.
        /// </summary>
        [HarmonyPatch(
            typeof(GangLeaderNeedsWeaponsIssueQuestBehavior.GangLeaderNeedsWeaponsIssueQuest),
            "CreateGuardsParty"
        )]
        private static class GangWeapons_GuardsParty_Postfix
        {
            private static readonly AccessTools.FieldRef<
                GangLeaderNeedsWeaponsIssueQuestBehavior.GangLeaderNeedsWeaponsIssueQuest,
                MobileParty
            > GuardsPartyRef = AccessTools.FieldRefAccess<
                GangLeaderNeedsWeaponsIssueQuestBehavior.GangLeaderNeedsWeaponsIssueQuest,
                MobileParty
            >("_guardsParty");

            private static void Postfix(
                GangLeaderNeedsWeaponsIssueQuestBehavior.GangLeaderNeedsWeaponsIssueQuest __instance
            )
            {
                bool swapped = DoSwap(GuardsPartyRef(__instance), t => t.IsMilitia);

                if (swapped)
                    Log.Debug(
                        $"GangWeapons_GuardsParty_Postfix: swapped guards troops for gang quest."
                    );
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   CaravanAmbush Quest                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Patch CaravanAmbushIssueQuestBehavior.OnQuestAccepted to swap troops after creation.
        /// </summary>
        [HarmonyPatch(
            typeof(CaravanAmbushIssueBehavior.CaravanAmbushIssueQuest),
            "OnQuestAccepted"
        )]
        private static class CaravanAmbush_CaravanMaster_Postfix
        {
            private static readonly AccessTools.FieldRef<
                CaravanAmbushIssueBehavior.CaravanAmbushIssueQuest,
                MobileParty
            > CaravanPartyRef = AccessTools.FieldRefAccess<
                CaravanAmbushIssueBehavior.CaravanAmbushIssueQuest,
                MobileParty
            >("_caravanParty");

            private static void Postfix(
                CaravanAmbushIssueBehavior.CaravanAmbushIssueQuest __instance
            )
            {
                bool swapped = DoSwap(CaravanPartyRef(__instance), t => t.IsCaravan);

                if (swapped)
                    Log.Debug(
                        $"CaravanAmbush_CaravanMaster_Postfix: swapped caravan troops for ambush quest."
                    );
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //        LandlordNeedsAccessToVillageCommons Quest       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Patch LandlordNeedsAccessToVillageCommonsIssueQuestBehavior.SpawnHerdersParty to swap troops after creation.
        /// </summary>
        [HarmonyPatch(
            typeof(LandlordNeedsAccessToVillageCommonsIssueBehavior.LandlordNeedsAccessToVillageCommonsIssueQuest),
            "SpawnHerdersParty"
        )]
        private static class LandlordCommons_Herders_Postfix
        {
            private static readonly AccessTools.FieldRef<
                LandlordNeedsAccessToVillageCommonsIssueBehavior.LandlordNeedsAccessToVillageCommonsIssueQuest,
                MobileParty
            > HerdersPartyRef = AccessTools.FieldRefAccess<
                LandlordNeedsAccessToVillageCommonsIssueBehavior.LandlordNeedsAccessToVillageCommonsIssueQuest,
                MobileParty
            >("_herdersMobileParty");

            private static void Postfix(
                LandlordNeedsAccessToVillageCommonsIssueBehavior.LandlordNeedsAccessToVillageCommonsIssueQuest __instance
            )
            {
                bool swapped = DoSwap(HerdersPartyRef(__instance), t => t.IsVillager);

                if (swapped)
                    Log.Debug(
                        $"LandlordCommons_Herders_Postfix: swapped herders troops for quest."
                    );
            }
        }

        /// <summary>
        /// Patch LandlordNeedsAccessToVillageCommonsIssueQuestBehavior.SpawnRivalParty to swap troops after creation.
        /// </summary>
        [HarmonyPatch(
            typeof(LandlordNeedsAccessToVillageCommonsIssueBehavior.LandlordNeedsAccessToVillageCommonsIssueQuest),
            "SpawnRivalParty"
        )]
        private static class LandlordCommons_Rival_Postfix
        {
            private static readonly AccessTools.FieldRef<
                LandlordNeedsAccessToVillageCommonsIssueBehavior.LandlordNeedsAccessToVillageCommonsIssueQuest,
                MobileParty
            > RivalPartyRef = AccessTools.FieldRefAccess<
                LandlordNeedsAccessToVillageCommonsIssueBehavior.LandlordNeedsAccessToVillageCommonsIssueQuest,
                MobileParty
            >("_rivalMobileParty");

            private static void Postfix(
                LandlordNeedsAccessToVillageCommonsIssueBehavior.LandlordNeedsAccessToVillageCommonsIssueQuest __instance
            )
            {
                bool swapped = DoSwap(RivalPartyRef(__instance), t => t.IsVillager);

                if (swapped)
                    Log.Debug($"LandlordCommons_Rival_Postfix: swapped rival troops for quest.");
            }
        }
    }
}
