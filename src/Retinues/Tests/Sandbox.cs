using System;
using System.Collections.Generic;
using Retinues.Doctrines;
using Retinues.Features.Experience;
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
        private readonly Dictionary<string, int> _skillBaseline;
        private readonly HashSet<string> _editedVanilla;
        private readonly List<string> _unlocked;
        private readonly Dictionary<string, int> _unlockProgress;
        private readonly Dictionary<string, int> _stocks;

        // Doctrine state (private fields on DoctrineServiceBehavior, captured by reference).
        private readonly HashSet<string> _doctrineUnlockedRef;
        private readonly List<string> _doctrineUnlockedSnapshot;
        private readonly Dictionary<string, int> _featProgressRef;
        private readonly Dictionary<string, int> _featProgressSnapshot;

        // XP pools (private field on TroopXpBehavior, captured by reference).
        private readonly Dictionary<string, int> _xpPoolsRef;
        private readonly Dictionary<string, int> _xpPoolsSnapshot;

        public TestSandbox()
        {
            _activeStubs = new List<string>(WCharacter.ActiveStubIds);
            _factionMap = new Dictionary<string, BaseFaction>(BaseFaction.TroopFactionMap);
            _vanillaIdMap = new Dictionary<string, string>(WCharacter.VanillaStringIdMap);
            _upgradeMap = new Dictionary<string, WCharacter>(WCharacter.UpgradeMap);
            _skillBaseline = new Dictionary<string, int>(WCharacter.SkillBaselineMap);
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

            // Doctrine state lives in private fields; capture the live collections by reference so
            // we can restore their contents on dispose (covers reflective unlocks and feat edits).
            var svc = Campaign.Current?.GetCampaignBehavior<DoctrineServiceBehavior>();
            if (svc != null)
            {
                _doctrineUnlockedRef = Reflector.GetFieldValue<HashSet<string>>(svc, "_unlocked");
                _doctrineUnlockedSnapshot =
                    _doctrineUnlockedRef != null ? new List<string>(_doctrineUnlockedRef) : null;

                _featProgressRef = Reflector.GetFieldValue<Dictionary<string, int>>(
                    svc,
                    "_featProgress"
                );
                _featProgressSnapshot =
                    _featProgressRef != null
                        ? new Dictionary<string, int>(_featProgressRef)
                        : null;
            }

            var xp = TroopXpBehavior.Instance;
            if (xp != null)
            {
                _xpPoolsRef = Reflector.GetFieldValue<Dictionary<string, int>>(xp, "_xpPools");
                _xpPoolsSnapshot =
                    _xpPoolsRef != null ? new Dictionary<string, int>(_xpPoolsRef) : null;
            }
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
                RestoreDict(WCharacter.SkillBaselineMap, _skillBaseline);

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

                if (_doctrineUnlockedRef != null && _doctrineUnlockedSnapshot != null)
                {
                    _doctrineUnlockedRef.Clear();
                    _doctrineUnlockedRef.UnionWith(_doctrineUnlockedSnapshot);
                }
                if (_featProgressRef != null && _featProgressSnapshot != null)
                    RestoreDict(_featProgressRef, _featProgressSnapshot);

                if (_xpPoolsRef != null && _xpPoolsSnapshot != null)
                    RestoreDict(_xpPoolsRef, _xpPoolsSnapshot);

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
