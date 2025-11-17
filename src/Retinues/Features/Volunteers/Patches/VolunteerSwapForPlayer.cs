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

        private static WSettlement _settlement;
        private static Dictionary<string, WCharacter[]> _snapshot;
        private static bool _listenersRegistered;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Wiring                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Ensure campaign events are hooked exactly once.
        /// </summary>
        private static void EnsureListeners()
        {
            if (_listenersRegistered)
                return;

            _listenersRegistered = true;

            Log.Debug("[VolunteerSwapForPlayer] Registering campaign event listeners.");

            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(
                typeof(VolunteerSwapForPlayer),
                OnSettlementLeft
            );
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(
                typeof(VolunteerSwapForPlayer),
                OnBeforeSave
            );
            CampaignEvents.SettlementEntered.AddNonSerializedListener(
                typeof(VolunteerSwapForPlayer),
                OnSettlementEntered
            );
        }

        /// <summary>
        /// Bootstrap: called once when PlayerTownVisitCampaignBehavior registers its events.
        /// </summary>
        [HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "RegisterEvents")]
        private static class VolunteerSwapForPlayer_Bootstrap
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                EnsureListeners();
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                Snapshot / Restore Helpers              //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Store the current volunteers for each notable in the settlement (wrapper-based).
        /// </summary>
        private static void SnapshotVolunteers(WSettlement settlement)
        {
            var notables = settlement?.Notables;
            if (notables == null || notables.Count == 0)
                return;

            var snapshot = new Dictionary<string, WCharacter[]>(StringComparer.Ordinal);

            foreach (var notable in notables)
            {
                if (string.IsNullOrEmpty(notable?.StringId))
                    continue;

                var volunteers = notable.Hero?.VolunteerTypes;
                if (volunteers == null || volunteers.Length == 0)
                    continue;

                var notableSnapshot = new WCharacter[volunteers.Length];
                for (int i = 0; i < volunteers.Length; i++)
                    notableSnapshot[i] =
                        volunteers[i] != null ? new WCharacter(volunteers[i]) : null;

                snapshot[notable.StringId] = notableSnapshot;
            }

            if (snapshot.Count == 0)
                return;

            _settlement = settlement;
            _snapshot = snapshot;
        }

        /// <summary>
        /// Restore the volunteers for the settlement if a snapshot exists (wrapper-based).
        /// </summary>
        private static void RestoreSnapshot()
        {
            Log.Info(
                "[VolunteerSwapForPlayer] Attempting to restore volunteer snapshot for player..."
            );
            Log.Info(
                $"[VolunteerSwapForPlayer] Snapshot is {(_snapshot == null ? "null" : "not null")}."
            );
            Log.Info(
                $"[VolunteerSwapForPlayer] Settlement is {(_settlement == null ? "null" : _settlement.StringId)}."
            );

            if (_snapshot == null || _settlement == null)
                return;

            var notables = _settlement.Notables;
            if (notables == null || notables.Count == 0)
            {
                ClearSnapshot();
                return;
            }

            foreach (var notable in notables)
            {
                if (string.IsNullOrEmpty(notable?.StringId))
                    continue;

                if (!_snapshot.TryGetValue(notable.StringId, out var notableSnapshot))
                    continue;

                var volunteers = notable.Hero?.VolunteerTypes;
                if (volunteers == null)
                    continue;

                var count = Math.Min(volunteers.Length, notableSnapshot.Length);
                for (int i = 0; i < count; i++)
                {
                    // Preserve empty slots currently in-game (mirror original behavior)
                    if (volunteers[i] == null)
                        continue;

                    // Write back from snapshot (null-safe)
                    var snap = notableSnapshot[i];
                    volunteers[i] = snap?.Base;
                }
            }
            Log.Debug(
                $"[VolunteerSwapForPlayer] Restored volunteers for settlement {_settlement.StringId}"
            );

            ClearSnapshot();
        }

        /// <summary>
        /// Clear any stored snapshot.
        /// </summary>
        private static void ClearSnapshot()
        {
            _snapshot = null;
            _settlement = null;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //             Hooks That Restore Snapshot                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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
        private static void OnSettlementLeft(MobileParty party, Settlement _)
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
        //                   Hooks That Swap                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Attempt to swap volunteers when the main party enters a settlement.
        /// </summary>
        private static void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            Log.Debug(
                $"[VolunteerSwapForPlayer] OnSettlementEntered triggered for party {party?.Name} "
                    + $"and settlement {settlement?.Name}."
            );

            if (party != MobileParty.MainParty)
                return;

            // Use Current to stay in wrapper-land consistently.
            TryBeginSwap();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Volunteer Swap                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void TryBeginSwap()
        {
            Log.Info("[VolunteerSwapForPlayer] Trying to begin volunteer swap for player...");
            // Already active for this visit? Don't resnapshot or reswap.
            if (_snapshot != null)
                return;

            var settlement = WSettlement.Current;
            if (settlement == null || string.IsNullOrEmpty(settlement.StringId))
                return;

            bool inPlayerOwnedFief = settlement.PlayerFaction != null;

            if (!inPlayerOwnedFief && !Config.RecruitAnywhere)
                return;

            var faction = inPlayerOwnedFief ? settlement.PlayerFaction : Player.Clan;
            if (faction == null)
                return;

            SnapshotVolunteers(settlement);
            settlement.SwapVolunteers(faction);

            Log.Debug(
                $"[VolunteerSwapForPlayer] Swapped volunteers for settlement {settlement.StringId} "
                    + $"(faction {faction.Name})."
            );
        }
    }
}
