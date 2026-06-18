using System;
using System.Collections.Generic;
using Retinues.Features.Stocks;
using Retinues.Features.Unlocks;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Tests
{
    /// <summary>
    /// Disposable test scope that isolates tests which mutate global mod state. On construction
    /// it snapshots every static store the troop systems touch; on dispose it restores them, so a
    /// test run leaves the loaded campaign exactly as it found it.
    ///
    /// Usage:
    ///   using var sandbox = new TestSandbox();
    ///   var faction = sandbox.NewFaction();   // throwaway faction for tree-building
    ///   // ... create troops, run assertions ...
    /// </summary>
    public sealed class TestSandbox : IDisposable
    {
        private readonly List<string> _activeStubs;
        private readonly Dictionary<string, BaseFaction> _factionMap;
        private readonly Dictionary<string, string> _vanillaIdMap;
        private readonly Dictionary<string, WCharacter> _upgradeMap;
        private readonly HashSet<string> _editedVanilla;
        private readonly List<string> _unlocked;
        private readonly Dictionary<string, int> _unlockProgress;
        private readonly Dictionary<string, int> _stocks;

        public TestSandbox()
        {
            _activeStubs = new List<string>(WCharacter.ActiveStubIds);
            _factionMap = new Dictionary<string, BaseFaction>(BaseFaction.TroopFactionMap);
            _vanillaIdMap = new Dictionary<string, string>(WCharacter.VanillaStringIdMap);
            _upgradeMap = new Dictionary<string, WCharacter>(WCharacter.UpgradeMap);
            _editedVanilla = new HashSet<string>(
                WCharacter.EditedVanillaRootIds,
                StringComparer.Ordinal
            );

            var unlocks = UnlocksBehavior.Instance;
            _unlocked = unlocks != null ? new List<string>(unlocks.UnlockedItemIds) : null;
            _unlockProgress =
                unlocks != null ? new Dictionary<string, int>(unlocks.ProgressByItemId) : null;

            var stocks = StocksBehavior.Instance;
            _stocks = stocks != null ? new Dictionary<string, int>(stocks.StocksByItemId) : null;
        }

        /// <summary>
        /// Allocates a fresh custom stub, marks it active, and returns it as a wrapper.
        /// Released automatically when the sandbox is disposed.
        /// </summary>
        public WCharacter NewStub()
        {
            var stub = WCharacter.AllocateStub();
            if (!WCharacter.ActiveStubIds.Contains(stub.StringId))
                WCharacter.ActiveStubIds.Add(stub.StringId);
            return new WCharacter(stub);
        }

        /// <summary>
        /// Returns a throwaway faction (a non-player clan whose culture has troop roots) for
        /// building custom troop trees without touching the player's real factions. Pass an
        /// <paramref name="exclude"/> faction to get a second, distinct one. Returns null if no
        /// suitable clan exists in the current campaign.
        /// </summary>
        public WFaction NewFaction(WFaction exclude = null)
        {
            var playerClan = Hero.MainHero?.Clan;
            var excludeBase = exclude?.Base;

            foreach (var clan in Clan.All)
            {
                if (clan == null || clan == playerClan || clan == excludeBase || clan.Culture == null)
                    continue;

                var faction = new WFaction(clan);
                if (faction.Culture?.RootElite != null && faction.Culture?.RootBasic != null)
                    return faction;
            }

            return null;
        }

        public void Dispose()
        {
            try
            {
                RestoreList(WCharacter.ActiveStubIds, _activeStubs);
                RestoreDict(BaseFaction.TroopFactionMap, _factionMap);
                RestoreDict(WCharacter.VanillaStringIdMap, _vanillaIdMap);
                RestoreDict(WCharacter.UpgradeMap, _upgradeMap);

                WCharacter.EditedVanillaRootIds.Clear();
                WCharacter.EditedVanillaRootIds.UnionWith(_editedVanilla);

                var unlocks = UnlocksBehavior.Instance;
                if (unlocks != null && _unlocked != null)
                    RestoreList(unlocks.UnlockedItemIds, _unlocked);
                if (unlocks != null && _unlockProgress != null)
                    RestoreDict(unlocks.ProgressByItemId, _unlockProgress);

                var stocks = StocksBehavior.Instance;
                if (stocks != null && _stocks != null)
                    RestoreDict(stocks.StocksByItemId, _stocks);

                // Invalidate cached faction lookups and captain caches so nothing keeps a
                // reference to a sandbox troop after restore.
                BaseFaction.TroopFactionMapVersion++;
                WCharacter.ClearCaptainCaches();
            }
            catch (Exception e)
            {
                Log.Exception(e, "TestSandbox: failed to restore global state.");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void RestoreList(List<string> target, List<string> snapshot)
        {
            target.Clear();
            target.AddRange(snapshot);
        }

        private static void RestoreDict<TKey, TValue>(
            Dictionary<TKey, TValue> target,
            Dictionary<TKey, TValue> snapshot
        )
        {
            target.Clear();
            foreach (var kv in snapshot)
                target[kv.Key] = kv.Value;
        }
    }
}
