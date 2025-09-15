using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        public static int GetPool(WCharacter troop) =>
            troop == null ? 0 : (_pool.TryGetValue(troop.StringId, out var v) ? v : 0);

        public static bool TrySpend(WCharacter troop, int amount)
        {
            if (troop == null || amount <= 0)
                return false;
            var key = troop.StringId;
            var have = GetPool(troop);
            if (have < amount)
                return false;
            _pool[key] = have - amount;
            return true;
        }

        public static void Refund(WCharacter troop, int amount)
        {
            var refund = DoctrineAPI.IsDoctrineUnlocked<AdaptiveTraining>();

            if (!refund)
                return;

            if (troop == null || amount <= 0)
                return;
            var key = troop.StringId;
            _pool[key] = GetPool(troop) + amount;
        }

        /// Called at game loaded/session launched to align snapshot with current roster XP.
        public static void InitializeSnapshotFromRoster()
        {
            Log.Info("TroopXpService: Initializing XP snapshot from player party...");
            _lastSnapshotXp.Clear();
            var roster = Player.Party?.MemberRoster;
            if (roster == null)
                return;

            foreach (var e in roster.Elements)
            {
                if (e.Troop == null || !e.Troop.IsCustom)
                    continue;
                _lastSnapshotXp[e.Troop.StringId] = e.Xp;
            }
        }

        public static void AccumulateFromMission(Dictionary<WCharacter, int> xpByTroop)
        {
            if (xpByTroop == null || xpByTroop.Count == 0)
                return;

            foreach (var kv in xpByTroop)
            {
                if (kv.Key == null || kv.Value <= 0)
                    continue;
                _pool[kv.Key.StringId] = GetPool(kv.Key) + kv.Value;
            }
        }
    }
}
