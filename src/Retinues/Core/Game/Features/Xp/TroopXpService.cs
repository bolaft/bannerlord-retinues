using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game.Features.Doctrines;
using Retinues.Core.Game.Features.Doctrines.Catalog;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Game.Features.Xp
{
    // Global XP bank per troop type (StringId). Tracks deltas from the player's party roster XP.
    public static class TroopXpService
    {
        // Persisted
        internal static Dictionary<string, int> _pool = [];

        // Not persisted (rebuilt on load to avoid "free" XP)
        private static readonly Dictionary<string, int> _lastSnapshotXp = [];

        public static int GetPool(WCharacter troop)
            => troop == null ? 0 : (_pool.TryGetValue(troop.StringId, out var v) ? v : 0);

        public static bool TrySpend(WCharacter troop, int amount)
        {
            if (troop == null || amount <= 0) return false;
            var key = troop.StringId;
            var have = GetPool(troop);
            if (have < amount) return false;
            _pool[key] = have - amount;
            return true;
        }

        public static void Refund(WCharacter troop, int amount)
        {
            var refund = DoctrineAPI.IsDoctrineUnlocked<AdaptiveTraining>();

            if (!refund)
                return;

            if (troop == null || amount <= 0) return;
            var key = troop.StringId;
            _pool[key] = GetPool(troop) + amount;
        }

        // Call at game loaded/session launched to align snapshot with current roster XP.
        public static void InitializeSnapshotFromRoster()
        {
            _lastSnapshotXp.Clear();
            var roster = Player.Party?.MemberRoster;
            if (roster == null) return;

            foreach (var e in roster.Elements)
            {
                if (e.Troop == null || !e.Troop.IsCustom) continue;
                _lastSnapshotXp[e.Troop.StringId] = e.Xp;
            }
        }

        // Accumulate positive XP deltas from the player's party roster into the bank.
        public static void AccumulateFromPlayerParty()
        {
            try
            {
                var roster = Player.Party?.MemberRoster;
                if (roster == null) return;

                // Build a set of all troop StringIds present now
                var present = roster.Elements
                                    .Where(e => e.Troop != null && e.Troop.IsCustom)
                                    .Select(e => e.Troop.StringId)
                                    .ToHashSet();

                foreach (var e in roster.Elements)
                {
                    if (e.Troop == null /* || !e.Troop.IsCustom */) continue; // keep/customize your filter as needed

                    var id  = e.Troop.StringId;
                    var now = e.Xp;

                    // Baseline new troop types to 0 so their first battle counts
                    var last = _lastSnapshotXp.TryGetValue(id, out var l) ? l : 0;

                    var delta = now - last;

                    // Credit 5% of the delta (at least 1 if there was any positive delta)
                    int toAdd = delta > 0 ? System.Math.Max(1, delta / 20) : 0;
                    if (toAdd > 0)
                        _pool[id] = (_pool.TryGetValue(id, out var v) ? v : 0) + toAdd;

                    // Update snapshot AFTER using it
                    _lastSnapshotXp[id] = now;
                }

                // Optionally prune snapshots for types no longer present
                foreach (var key in _lastSnapshotXp.Keys.Where(k => !present.Contains(k)).ToList())
                {
                    _lastSnapshotXp.Remove(key);
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }
    }
}
