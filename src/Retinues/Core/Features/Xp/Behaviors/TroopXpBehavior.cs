using System;
using System.Collections.Generic;
using Retinues.Core.Features.Doctrines;
using Retinues.Core.Features.Doctrines.Catalog;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Features.Xp.Behaviors
{
    [SafeClass]
    public sealed class TroopXpBehavior : CampaignBehaviorBase
    {
        public static TroopXpBehavior Instance { get; private set; }

        public TroopXpBehavior()
        {
            Instance = this;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private Dictionary<string, int> _xpPools = [];

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("Retinues_Xp_Pools", ref _xpPools);

            Log.Debug($"{_xpPools.Count} troop XP pools.");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents() { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static int Get(WCharacter troop)
        {
            if (troop == null || Instance == null)
                return 0;
            return Instance.GetPool(PoolKey(troop));
        }

        public static void Add(WCharacter troop, int delta)
        {
            if (troop == null || Instance == null || delta == 0)
                return;
            var cur = Instance.GetPool(PoolKey(troop));
            Instance._xpPools[PoolKey(troop)] = Math.Max(0, cur + delta);
        }

        public static bool TrySpend(WCharacter troop, int amount)
        {
            if (amount <= 0)
                return true;
            if (troop == null)
                return false;
            var have = Get(troop);
            if (have < amount)
                return false;
            Add(troop, -amount);
            return true;
        }

        public static void Refund(WCharacter troop, int amount)
        {
            if (troop == null || amount <= 0)
                return;

            if (!DoctrineAPI.IsDoctrineUnlocked<AdaptiveTraining>())
                return;

            Add(troop, amount);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool SharedPool => Config.GetOption<bool>("SharedXpPool");

        private static string PoolKey(WCharacter troop) => SharedPool ? "_shared" : troop?.StringId;

        private int GetPool(string key) =>
            (key != null && _xpPools.TryGetValue(key, out var v)) ? v : 0;
    }
}
