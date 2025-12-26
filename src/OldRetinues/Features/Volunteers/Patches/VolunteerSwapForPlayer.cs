using System;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace OldRetinues.Features.Volunteers.Patches
{
    /// <summary>
    /// Swaps volunteers for the player and handles snapshots.
    /// </summary>
    [HarmonyPatch]
    internal static class VolunteerSwapForPlayer
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         State                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static WSettlement _settlement;
        private static Dictionary<string, WCharacter[]> _snapshot;
        private static readonly Random _rng = new();

        /// <summary>
        /// Re-wire events for the current campaign and clear any stale snapshot.
        /// </summary>
        public static void Initialize()
        {
            // Make sure we do not carry stale state across saves / campaigns.
            ClearSnapshot();

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

        // Wire campaign events once via static ctor.
        static VolunteerSwapForPlayer()
        {
            // First game in the process still gets wired automatically.
            // Later games (load from main menu / in-session) will be re-wired via Initialize().
            Initialize();
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
        /// This helps support hotkey mods that jump straight to the volunteer screen.
        /// </summary>
        private static void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (party != MobileParty.MainParty)
                return;

            TryBeginSwap();
        }

        [HarmonyPatch(
            typeof(PlayerTownVisitCampaignBehavior),
            "game_menu_recruit_volunteers_on_consequence"
        )]
        internal static class VolunteerSwapForPlayer_Begin
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                TryBeginSwap();
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Volunteer Swap                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Ensure the player sees custom volunteers where allowed.
        /// - In player-faction fiefs, volunteers are already swapped by the
        ///   daily UpdateVolunteers patch; we just enforce it if needed.
        /// - In foreign settlements (Recruit Anywhere), we do a temporary
        ///   swap with snapshot + restore.
        /// </summary>
        private static void TryBeginSwap()
        {
            // Do not stack multiple temporary swaps.
            if (_snapshot != null)
                return;

            var settlement = WSettlement.Current;
            if (settlement == null || string.IsNullOrEmpty(settlement.StringId))
                return;

            bool inPlayerOwnedFief = settlement.PlayerFaction != null;

            // If Recruit Anywhere is disabled, we do not touch foreign settlements.
            if (!inPlayerOwnedFief && Config.RestrictToOwnedSettlements)
                return;

            // ── Player-owned fief ─────────────────────────────────
            if (inPlayerOwnedFief)
            {
                // Ensure volunteers are fully swapped to the current player-sphere tree
                // (this keeps newly unlocked troops in sync even before the next daily tick).
                settlement.SwapVolunteers();

                // Snapshot the canonical, fully-custom volunteers so that any temporary
                // mix we apply for the player view can be reverted on exit.
                SnapshotVolunteers(settlement);

                // Apply player-only proportion (mix some vanilla back in visually).
                ApplyPlayerVolunteerProportion(settlement);
                return;
            }

            // ── Remote settlement (Recruit Anywhere) ──────────────
            // For foreign fiefs we only want a temporary swap for the player:
            // 1) Snapshot native volunteers.
            // 2) Swap everything to Player.Clan / mix rules.
            // 3) Apply the player-only custom/vanilla proportion.
            var faction = Player.Clan;
            if (faction == null)
                return;

            SnapshotVolunteers(settlement);
            settlement.SwapVolunteers(faction);
            ApplyPlayerVolunteerProportion(settlement);
        }

        /// <summary>
        /// For the current settlement, adjust volunteers so that roughly
        /// Config.CustomVolunteerProportion of them remain custom and the rest
        /// are mapped back to native/culture equivalents. This only affects
        /// what the player sees during the recruit screen; the canonical
        /// volunteers are restored from the snapshot afterwards.
        /// The selection is deterministic per settlement per campaign day.
        /// </summary>
        private static void ApplyPlayerVolunteerProportion(WSettlement settlement)
        {
            if (settlement == null)
                return;

            float customP = Config.CustomVolunteersProportion;
            if (customP <= 0f)
                customP = 0f;
            else if (customP >= 1f)
                return; // 100% custom, nothing to do

            var culture = settlement.Culture;
            if (culture == null)
                return;

            var nativeBasic = culture.RootBasic;
            var nativeElite = culture.RootElite;

            if (
                (nativeBasic == null || !nativeBasic.IsValid)
                && (nativeElite == null || !nativeElite.IsValid)
            )
                return;

            // Deterministic RNG: same settlement + same day => same pattern.
            var now = CampaignTime.Now;
            int day = (int)now.ToDays;
            int seed = unchecked(settlement.StringId.GetHashCode() * 397 ^ day);
            var rng = new Random(seed);

            foreach (var notable in settlement.Notables)
            {
                var volunteers = notable.Hero?.VolunteerTypes;
                if (volunteers == null || volunteers.Length == 0)
                    continue;

                for (int i = 0; i < volunteers.Length; i++)
                {
                    var co = volunteers[i];
                    if (co == null)
                        continue;

                    var troop = new WCharacter(co);
                    if (!troop.IsValid)
                    {
                        volunteers[i] = null;
                        continue;
                    }

                    // Only consider demoting custom troops; native ones (if any)
                    // should stay as-is.
                    if (!troop.IsCustom)
                        continue;

                    // Keep as custom according to the configured proportion.
                    if (rng.NextDouble() <= customP)
                        continue;

                    var root = troop.IsElite ? nativeElite : nativeBasic;
                    if (root == null || !root.IsValid)
                        continue;

                    var native = TroopMatcher.PickBestFromTree(root, troop, sameTierOnly: false);
                    if (native == null || !native.IsValid)
                        continue;

                    volunteers[i] = native.Base;
                }
            }
        }
    }
}
