using System.Collections.Generic;
using Retinues.Core.Features.Doctrines;
using Retinues.Core.Features.Doctrines.Catalog;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Features.Xp
{
    public static class TroopXpService
    {
        internal static bool SharedPool => Config.GetOption<bool>("SharedXpPool");

        internal static Dictionary<string, int> _pool = [];

        public static string PoolKey(WCharacter troop)
        {
            return SharedPool ? "shared" : troop?.StringId ?? "unknown";
        }

        public static int GetPool(WCharacter troop) =>
            troop == null ? 0 : (_pool.TryGetValue(PoolKey(troop), out var v) ? v : 0);

        public static void SetPool(WCharacter troop, int amount)
        {
            if (troop == null || amount < 0)
                return;
            _pool[PoolKey(troop)] = amount;
        }

        public static void AddToPool(WCharacter troop, int amount)
        {
            if (troop == null || amount <= 0)
                return;
            _pool[PoolKey(troop)] = GetPool(troop) + amount;
        }

        public static bool TrySpend(WCharacter troop, int amount)
        {
            if (amount <= 0)
                return true;
            if (troop == null)
                return false;
            var have = GetPool(troop);
            if (have < amount)
                return false;
            _pool[PoolKey(troop)] = have - amount;
            return true;
        }

        public static void Refund(WCharacter troop, int amount)
        {
            var refund = DoctrineAPI.IsDoctrineUnlocked<AdaptiveTraining>();

            if (!refund)
                return;

            if (troop == null || amount <= 0)
                return;
            _pool[PoolKey(troop)] = GetPool(troop) + amount;
        }

        public static void AccumulateFromMission(Dictionary<WCharacter, int> xpByTroop)
        {
            if (xpByTroop == null || xpByTroop.Count == 0)
                return;

            foreach (var kv in xpByTroop)
            {
                if (kv.Key == null || kv.Value <= 0)
                    continue;
                _pool[PoolKey(kv.Key)] = GetPool(kv.Key) + kv.Value;
            }
        }
    }
}
