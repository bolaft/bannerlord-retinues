using System.Collections.Generic;
using Retinues.Behaviors.Missions;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Events.Models;
using Retinues.Framework.Behaviors;

namespace Retinues.Behaviors.History
{
    /// <summary>
    /// Updates WCharacter battle history attributes for faction troops.
    /// - Kills/casualties are recorded from CombatBehavior kills.
    /// - Battle result (type + won/lost) is recorded for all troops present in the player party roster at battle start.
    /// </summary>
    public sealed class HistoryRecordBehavior : BaseCampaignBehavior
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Pending                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Troops present in Player.Party.MemberRoster at battle start (faction troops only).
        readonly List<WCharacter> _rosterAtStart = [];

        // Simple guard to avoid double-applying per mission end if hooks fire twice.
        bool _killsApplied;

        // Indicates we are waiting for a map event end to apply RecordBattleResult(...).
        bool _awaitingMapEventEnd;

        // True once a mission ran for the current map event (manual battle); stays false for
        // auto-resolved (simulated) battles, where no mission fires.
        bool _missionHappened;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Mission                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Called when a map event starts. Fires for both manual and auto-resolved battles, so it
        /// is the snapshot/reset point that covers simulated battles (which never run a mission).
        /// </summary>
        protected override void OnMapEventStarted(MMapEvent mapEvent)
        {
            _rosterAtStart.Clear();
            _killsApplied = false;
            _awaitingMapEventEnd = false;
            _missionHappened = false;

            if (mapEvent == null || !mapEvent.IsPlayerInvolved)
                return;

            // Only battles carry a result worth recording.
            if (!mapEvent.IsFieldBattle && !mapEvent.IsSiegeBattle && !mapEvent.IsNavalBattle)
                return;

            SnapshotPlayerRoster();
        }

        /// <summary>
        /// Called when a mission starts.
        /// </summary>
        protected override void OnMissionStarted(MMission mission)
        {
            _killsApplied = false;
            _awaitingMapEventEnd = false;
            _missionHappened = true;

            _rosterAtStart.Clear();

            // Only track roster for combat missions.
            if (mission == null || !mission.IsBattle)
                return;

            // Re-snapshot here as well: OnMapEventStarted normally fires first, but this is robust
            // to event ordering and to missions that lack a preceding player map event.
            SnapshotPlayerRoster();
        }

        /// <summary>
        /// Snapshots the player party's faction troops present at battle start.
        /// </summary>
        void SnapshotPlayerRoster()
        {
            var roster = Player.Party?.MemberRoster;
            if (roster == null)
                return;

            foreach (var e in roster.Elements)
            {
                if (e.Number <= 0)
                    continue; // Skip empty.

                var troop = e.Troop; // this is a WCharacter instance

                if (troop == null)
                    continue;

                if (!troop.IsFactionTroop)
                    continue; // Ignore everything else.

                // Record once per troop type, not per stack count.
                _rosterAtStart.Add(troop);
            }
        }

        /// <summary>
        /// Called when a mission ends.
        /// </summary>
        protected override void OnMissionEnded(MMission mission)
        {
            // Always apply kills/casualties once when the mission ends.
            ApplyKillsAndCasualtiesOnce();

            // If a player-involved map event exists, defer battle result to OnMapEventEnded.
            var mapEvent = CombatBehavior.MapEvent;
            if (mapEvent != null && mapEvent.IsPlayerInvolved)
            {
                _awaitingMapEventEnd = true;
                return;
            }

            // No map event: we cannot call RecordBattleResult(MMapEvent).
            _awaitingMapEventEnd = false;
            _rosterAtStart.Clear();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Map Event                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Called when a map event ends.
        /// </summary>
        protected override void OnMapEventEnded(MMapEvent end)
        {
            // Record the result for manual battles (we were awaiting this) and for auto-resolved
            // battles (no mission ran). Manual kills are applied separately in OnMissionEnded;
            // auto-resolve cannot attribute individual kills, so it records the result only.
            bool shouldRecord = _awaitingMapEventEnd || !_missionHappened;
            _awaitingMapEventEnd = false;

            if (!shouldRecord || end == null || !end.IsPlayerInvolved)
            {
                _rosterAtStart.Clear();
                return;
            }

            // Record battle outcome/type for every troop present at start.
            // (Only faction troops were stored.)
            for (int i = 0; i < _rosterAtStart.Count; i++)
            {
                var troop = _rosterAtStart[i];
                if (troop == null)
                    continue;

                if (!troop.IsFactionTroop)
                    continue;

                troop.RecordBattleResult(end);
            }

            _rosterAtStart.Clear();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Applies kills and casualties to faction troops once.
        /// </summary>
        void ApplyKillsAndCasualtiesOnce()
        {
            if (_killsApplied)
                return;

            _killsApplied = true;

            var kills = CombatBehavior.GetKills();
            if (kills == null || kills.Count == 0)
                return;

            for (int i = 0; i < kills.Count; i++)
            {
                var k = kills[i];

                // These are WCharacter instances in your pipeline.
                var killer = k.KillerCharacter;
                var victim = k.VictimCharacter;

                // Killer: record kill against victim.
                if (killer != null && killer.IsFactionTroop)
                {
                    // victim may be null in edge cases; your wrapper handles dictionary keys,
                    // so skip if victim is null.
                    if (victim != null)
                        killer.RecordKill(victim, count: 1);
                }

                // Victim: record casualty caused by killer.
                if (victim != null && victim.IsFactionTroop)
                {
                    if (killer != null)
                        victim.RecordCasualty(killer, count: 1);
                }
            }
        }
    }
}
