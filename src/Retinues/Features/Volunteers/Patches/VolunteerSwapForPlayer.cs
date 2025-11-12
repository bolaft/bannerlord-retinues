using System;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Features.Volunteers.Patches
{
    /// <summary>
    /// Harmony patch for recruit volunteers menu consequence.
    /// Swaps volunteers for the player and restores the native roster.
    /// </summary>
    [HarmonyPatch]
    internal static class VolunteerSwapForPlayer
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         State                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static Settlement _settlement;
        private static Dictionary<string, CharacterObject[]> _snapshot;

        static VolunteerSwapForPlayer()
        {
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(
                typeof(VolunteerSwapForPlayer),
                OnSettlementLeft
            );
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(
                typeof(VolunteerSwapForPlayer),
                OnBeforeSave
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Snapshot / Restore Helpers             //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Store the current volunteers for each notable in the settlement.
        /// </summary>
        private static void SnapshotVolunteers(Settlement settlement)
        {
            var notables = settlement?.Notables;
            if (notables == null || notables.Count == 0)
                return;

            var snapshot = new Dictionary<string, CharacterObject[]>(StringComparer.Ordinal);

            foreach (var notable in notables)
            {
                if (notable?.StringId == null)
                    continue;

                var volunteers = notable.VolunteerTypes;
                if (volunteers == null || volunteers.Length == 0)
                    continue;

                var notableSnapshot = new CharacterObject[volunteers.Length];
                Array.Copy(volunteers, notableSnapshot, volunteers.Length);
                snapshot[notable.StringId] = notableSnapshot;
            }

            if (snapshot.Count == 0)
                return;

            _settlement = settlement;
            _snapshot = snapshot;
        }

        /// <summary>
        /// Restore the volunteers for the settlement if a snapshot exists.
        /// </summary>
        private static void RestoreSnapshot()
        {
            if (_snapshot == null || _settlement == null)
                return;

            var notables = _settlement?.Notables;
            if (notables == null || notables.Count == 0)
            {
                ClearSnapshot();
                return;
            }

            foreach (var notable in notables)
            {
                if (notable?.StringId == null)
                    continue;

                if (!_snapshot.TryGetValue(notable.StringId, out var notableSnapshot))
                    continue;

                var volunteers = notable.VolunteerTypes;
                if (volunteers == null)
                    continue;

                var count = Math.Min(volunteers.Length, notableSnapshot.Length);
                for (var i = 0; i < count; i++)
                {
                    if (volunteers[i] == null)
                        continue;

                    volunteers[i] = notableSnapshot[i];
                }
            }

            ClearSnapshot();
            Log.Debug(
                "[VolunteerSwapForPlayer] Restored volunteers for settlement "
                    + _settlement?.StringId
            );
        }

        /// <summary>
        /// Clear any stored snapshot.
        /// </summary>
        private static void ClearSnapshot()
        {
            _snapshot = null;
            _settlement = null;
        }

        /// <summary>
        /// Restore native volunteers once the player starts waiting in the settlement.
        /// </summary>
        [HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "game_menu_settlement_wait_on_init")]
        private static class VolunteerSwapForPlayer_WaitStart
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                RestoreSnapshot();
            }
        }

        /// <summary>
        /// Restore native volunteers once the main party leaves the settlement.
        /// </summary>
        private static void OnSettlementLeft(MobileParty party, Settlement settlement)
        {
            if (party != MobileParty.MainParty)
                return;

            RestoreSnapshot();
        }

        /// <summary>
        /// Ensure volunteers are restored before the game is saved.
        /// </summary>
        private static void OnBeforeSave()
        {
            RestoreSnapshot();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Volunteer Swap                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [HarmonyPatch(
            typeof(PlayerTownVisitCampaignBehavior),
            "game_menu_recruit_volunteers_on_consequence"
        )]
        internal static class VolunteerSwapForPlayer_Begin
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                var settlement = Settlement.CurrentSettlement;
                if (settlement == null || string.IsNullOrEmpty(settlement.StringId))
                    return;

                var wrapper = new WSettlement(settlement);
                bool playerOwnsFief = wrapper.PlayerFaction != null;

                if (!playerOwnsFief && !Config.RecruitAnywhere)
                    return;

                var faction = playerOwnsFief ? wrapper.PlayerFaction : Player.Clan;
                if (faction == null)
                    return;

                SnapshotVolunteers(settlement);
                Log.Debug(
                    "[VolunteerSwapForPlayer] Swapping volunteers for player faction in settlement "
                        + settlement.StringId
                );

                wrapper.SwapVolunteers(faction);
            }
        }
    }
}
