using System;
using System.Collections.Generic;
using Retinues.Domain;
using Retinues.Domain.Characters.Services.Matching;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Recruitement
{
    /// <summary>
    /// Temporarily swaps notable volunteers while the player is in the recruit menu.
    /// This is a player-only view override and must always restore afterwards.
    /// </summary>
    internal static class PlayerVolunteerSwapState
    {
        private static WSettlement _settlement;
        private static Dictionary<string, CharacterObject[]> _snapshot;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void TryBeginSwapForPlayerRecruitMenu()
        {
            try
            {
                // Only makes sense if player is actually in a settlement.
                var baseSettlement =
                    TaleWorlds.CampaignSystem.Settlements.Settlement.CurrentSettlement ?? Player
                        .CurrentSettlement
                        ?.Base;

                var settlement = baseSettlement != null ? WSettlement.Get(baseSettlement) : null;
                if (settlement == null)
                    return;

                // If we were already active for a different settlement, restore first.
                if (_settlement != null && _settlement != settlement)
                    RestoreIfActive();

                if (_snapshot == null)
                    SnapshotVolunteers(settlement);

                if (_snapshot == null)
                    return;

                ApplySwapForPlayer(settlement);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Recruitement: player volunteer swap begin failed.");
            }
        }

        public static void RestoreIfActive()
        {
            try
            {
                RestoreSnapshot();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Recruitement: player volunteer swap restore failed.");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Snapshot / Restore                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void SnapshotVolunteers(WSettlement settlement)
        {
            var notables = settlement?.Notables;
            if (notables == null || notables.Count == 0)
                return;

            var snapshot = new Dictionary<string, CharacterObject[]>(StringComparer.Ordinal);

            foreach (var notable in notables)
            {
                if (string.IsNullOrEmpty(notable?.StringId))
                    continue;

                var volunteers = notable.Base.VolunteerTypes;
                if (volunteers == null || volunteers.Length == 0)
                    continue;

                var copy = new CharacterObject[volunteers.Length];
                for (int i = 0; i < volunteers.Length; i++)
                    copy[i] = volunteers[i];

                snapshot[notable.StringId] = copy;
            }

            if (snapshot.Count == 0)
                return;

            _settlement = settlement;
            _snapshot = snapshot;
        }

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

                var volunteers = notable.Base.VolunteerTypes;
                if (volunteers == null)
                    continue;

                int count = Math.Min(volunteers.Length, notableSnapshot.Length);
                for (int i = 0; i < count; i++)
                {
                    // Preserve empty slots currently in-game.
                    if (volunteers[i] == null)
                        continue;

                    volunteers[i] = notableSnapshot[i];
                }
            }

            ClearSnapshot();
        }

        private static void ClearSnapshot()
        {
            _snapshot = null;
            _settlement = null;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Swap Logic                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void ApplySwapForPlayer(WSettlement settlement)
        {
            var notables = settlement?.Notables;
            if (notables == null || notables.Count == 0)
                return;

            foreach (var notable in notables)
            {
                if (string.IsNullOrEmpty(notable?.StringId))
                    continue;

                var volunteers = notable.Base.VolunteerTypes;
                if (volunteers == null || volunteers.Length == 0)
                    continue;

                for (int i = 0; i < volunteers.Length; i++)
                {
                    var current = volunteers[i];
                    if (current == null || current.IsHero)
                        continue;

                    var wc = WCharacter.Get(current);
                    if (wc == null)
                        continue;

                    bool elite = wc.IsElite;

                    // WSettlement defines exactly what roots the player can recruit from here.
                    var roots = elite ? settlement.GetEliteRoots() : settlement.GetBasicRoots();
                    if (roots == null || roots.Count == 0)
                        continue;

                    var chosenRoot = ChooseRootDeterministic(
                        settlement,
                        notable.StringId,
                        i,
                        roots
                    );
                    if (chosenRoot == null)
                        continue;

                    var replacement = CharacterMatcher.PickBestFromTree(wc, chosenRoot);
                    var replacementBase = replacement?.Base;
                    if (replacementBase == null)
                        continue;

                    if (replacementBase == current)
                        continue;

                    volunteers[i] = replacementBase;
                }
            }
        }

        private static WCharacter ChooseRootDeterministic(
            WSettlement settlement,
            string notableId,
            int slotIndex,
            List<WCharacter> roots
        )
        {
            if (
                settlement == null
                || string.IsNullOrEmpty(notableId)
                || roots == null
                || roots.Count == 0
            )
                return null;

            // Stable per day, per settlement, per notable, per slot.
            int day = (int)CampaignTime.Now.ToDays;
            int seed = StableHash($"{settlement.Base.StringId}|{notableId}|{slotIndex}|{day}");

            int idx = seed % roots.Count;
            if (idx < 0)
                idx = -idx;

            return roots[idx];
        }

        private static int StableHash(string s)
        {
            unchecked
            {
                int hash = 23;
                for (int i = 0; i < s.Length; i++)
                    hash = (hash * 31) + s[i];
                return hash;
            }
        }
    }
}
