using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Events.Models;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Game.Unlocks
{
    /// <summary>
    /// Applies equipment unlock progress from mission outcomes (kills).
    /// </summary>
    public sealed class UnlocksByKillsBehavior : BaseCampaignBehavior
    {
        private static Mission _lastAppliedMission;

        /// <summary>
        /// Called when a mission ends.
        /// </summary>
        protected override void OnMissionEnded(MMission mission)
        {
            if (!Settings.EquipmentNeedsUnlocking || !Settings.UnlockItemsThroughKills)
                return;

            try
            {
                if (!IsPlayerVictory())
                    return;

                // Keep the legacy behavior as a fallback path (for cases where
                // the post-battle scoreboard patch did not run).
                _ = ApplyProgressFromMissionKills(mission, notify: true);
            }
            catch (Exception e)
            {
                Log.Exception(e, "Item unlock progress failed on mission end.");
            }
        }

        /// <summary>
        /// Applies unlock progress based on kills recorded in the given mission.
        /// Returns the list of newly-unlocked items (if any).
        /// </summary>
        internal static IReadOnlyList<WItem> ApplyProgressFromMissionKills(
            MMission mission,
            bool notify
        )
        {
            var mm = MMission.Current;
            if (mm == null)
                return Array.Empty<WItem>();

            // If we got a mission wrapper, ensure it matches Current (when available).
            if (mission?.Base != null && !ReferenceEquals(mm.Base, mission.Base))
                return Array.Empty<WItem>();

            var mbMission = mission?.Base;
            if (mbMission != null && ReferenceEquals(_lastAppliedMission, mbMission))
                return Array.Empty<WItem>();

            var kills = mm.Kills;
            if (kills == null || kills.Count == 0)
                return Array.Empty<WItem>();

            var required = (int)Settings.RequiredKillsToUnlock;
            if (required <= 0)
                return Array.Empty<WItem>();

            var perKill = Math.Max(1, WItem.UnlockThreshold / required);

            var playerSide = mbMission?.PlayerTeam?.Side ?? BattleSideEnum.None;
            if (playerSide == BattleSideEnum.None)
                playerSide = InferPlayerSideFromKills(kills);

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var party = Player.Party;

            for (var i = 0; i < kills.Count; i++)
            {
                var k = kills[i];

                if (!IsQualifyingKill(party, k, playerSide))
                    continue;

                var code = k.VictimEquipmentCode;
                if (string.IsNullOrEmpty(code))
                    continue;

                var eq = Equipment.CreateFromEquipmentCode(code);
                if (eq == null)
                    continue;

                AccumulateFromEquipment(eq, counts);
            }

            if (counts.Count == 0)
                return Array.Empty<WItem>();

            var itemsTouched = 0;
            var unlocked = new List<WItem>();
            long totalAdded = 0;

            foreach (var kvp in counts)
            {
                var wItem = WItem.Get(kvp.Key);
                if (wItem == null || !wItem.IsValidEquipment)
                    continue;

                var add = perKill * kvp.Value;
                if (add <= 0)
                    continue;

                var wasUnlocked = wItem.IsUnlocked;
                var isUnlocked = wItem.IncreaseUnlockProgress(add);

                itemsTouched++;
                totalAdded += add;

                if (!wasUnlocked && isUnlocked)
                    unlocked.Add(wItem);
            }

            if (unlocked.Count > 0 && notify)
                UnlockNotifier.ItemsUnlocked(UnlockNotifier.UnlockMethod.Kills, unlocked);

            if (mbMission != null)
                _lastAppliedMission = mbMission;

            if (Settings.DebugMode && itemsTouched > 0)
            {
                Log.Debug(
                    $"[Unlocks] Mission kill progress applied: items={itemsTouched}, newlyUnlocked={unlocked.Count}, totalAdded={totalAdded}."
                );
            }

            return unlocked;
        }

        private static bool IsPlayerVictory()
        {
            // Prefer our wrapper if available.
            var mm = MMapEvent.Current;
            if (mm != null)
                return mm.IsWon;

            // Fallback for cases where Current was cleared early.
            var me = TaleWorlds.CampaignSystem.Party.MobileParty.MainParty?.MapEvent;
            if (me == null)
                return false;

            var wrapped = new MMapEvent(me);
            return wrapped.IsWon;
        }

        private static BattleSideEnum InferPlayerSideFromKills(IReadOnlyList<MMission.Kill> kills)
        {
            var party = Player.Party;
            if (party?.MemberRoster == null)
                return BattleSideEnum.None;

            for (var i = 0; i < kills.Count; i++)
            {
                var killerId = kills[i].KillerCharacterId;
                if (IsInParty(party, killerId))
                    return kills[i].KillerSide;
            }

            return BattleSideEnum.None;
        }

        private static bool IsQualifyingKill(
            WParty party,
            MMission.Kill k,
            BattleSideEnum playerSide
        )
        {
            var killerInPlayerParty = IsInParty(party, k.KillerCharacterId);

            if (!killerInPlayerParty)
            {
                if (!Settings.CountAllyKills)
                    return false;

                if (playerSide == BattleSideEnum.None)
                    return false;

                if (k.KillerSide != playerSide)
                    return false;
            }

            if (!Settings.CountAllyCasualties && playerSide != BattleSideEnum.None)
            {
                if (k.VictimSide == playerSide)
                    return false;
            }

            return true;
        }

        private static bool IsInParty(WParty party, string characterId)
        {
            if (party == null || string.IsNullOrEmpty(characterId))
                return false;

            var w = WCharacter.Get(characterId);
            if (w?.Base == null)
                return false;

            return party.MemberRoster.CountOf(w) > 0;
        }

        private static void AccumulateFromEquipment(Equipment eq, Dictionary<string, int> counts)
        {
            var slotCount = (int)EquipmentIndex.NumEquipmentSetSlots;

            for (var i = 0; i < slotCount; i++)
            {
                var item = eq[(EquipmentIndex)i].Item;
                if (item == null)
                    continue;

                var id = item.StringId;
                if (string.IsNullOrEmpty(id))
                    continue;

                if (counts.TryGetValue(id, out var c))
                    counts[id] = c + 1;
                else
                    counts[id] = 1;
            }
        }
    }
}
