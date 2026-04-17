using System;
using System.Collections.Generic;
using Retinues.Behaviors.Doctrines;
using Retinues.Behaviors.Doctrines.Definitions;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Migration.Legacy;
using Retinues.Migration.Shims;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;

namespace Retinues.Migration
{
    /// <summary>
    /// Applies v1 → v2 save migration on the first load of a legacy save.
    /// <para/>
    /// Registered before <c>BehaviorManager</c> so every shim gets its
    /// <c>SyncData</c> called before this coordinator's
    /// <c>OnGameLoadFinished</c> runs.
    /// </summary>
    internal sealed class LegacyMigrationCoordinator : CampaignBehaviorBase
    {
        /// <summary>
        /// Set to the v1 save version string when a legacy save is detected.
        /// Read by <see cref="Retinues.Framework.Modules.Versions.VersionBehavior"/>
        /// to show the migration-specific popup instead of the generic upgrade popup.
        /// </summary>
        internal static string DetectedLegacySaveVersion;
        private readonly FactionBehavior _faction;
        private readonly TroopXpBehavior _xp;
        private readonly TroopStatisticsBehavior _stats;
        private readonly VersionBehavior _versionShim;
        private readonly StocksBehavior _stocks;
        private readonly UnlocksBehavior _unlocks;
        private readonly DoctrineServiceBehavior _doctrines;
        private readonly AutoJoinBehavior _autoJoin;

        internal LegacyMigrationCoordinator(
            FactionBehavior faction,
            TroopXpBehavior xp,
            TroopStatisticsBehavior stats,
            StocksBehavior stocks,
            UnlocksBehavior unlocks,
            DoctrineServiceBehavior doctrines,
            AutoJoinBehavior autoJoin,
            VersionBehavior versionShim
        )
        {
            _faction = faction;
            _xp = xp;
            _stats = stats;
            _versionShim = versionShim;
            _stocks = stocks;
            _unlocks = unlocks;
            _doctrines = doctrines;
            _autoJoin = autoJoin;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(
                this,
                OnGameLoadFinished
            );
        }

        public override void SyncData(
            IDataStore dataStore
        ) { /* coordinator carries no saved state */
        }

        // ─────────────────────────────────────────────────────────────────────

        private void OnGameLoadFinished()
        {
            if (!HasLegacyData())
                return;

            DetectedLegacySaveVersion = _versionShim.SavedVersion ?? "unknown";
            Log.Info("[Migration] Legacy save detected – applying v1 → v2 migration.");

            try
            {
                MigrateCharacterData();
                MigrateItemData();
                MigrateDoctrines();
                MigrateFeatProgress();
            }
            catch (Exception ex)
            {
                Log.Error($"[Migration] Error during legacy migration: {ex}");
            }
        }

        // ─── Detect legacy save ────────────────────────────────────────────

        private bool HasLegacyData() =>
            _faction.ClanTroops != null
            || _faction.KingdomTroops != null
            || (_faction.CultureTroops?.Count > 0)
            || (_stocks.StocksByItemId?.Count > 0)
            || (_unlocks.UnlockedItemIds?.Count > 0)
            || (_unlocks.ProgressByItemId?.Count > 0)
            || (_doctrines.UnlockedDoctrines?.Count > 0)
            || (_autoJoin.HireCaps?.Count > 0);

        // ─── Troop / character data ────────────────────────────────────────

        private void MigrateCharacterData()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var troop in FlattenAllTroops())
            {
                if (string.IsNullOrEmpty(troop.StringId))
                    continue;

                if (!seen.Add(troop.StringId))
                    continue;

                var wc = WCharacter.Get(troop.StringId);
                if (wc == null)
                    continue; // troop does not exist in v2 – skip

                wc.FormationClassOverride = troop.FormationClassOverride;
                wc.IsMariner = troop.IsMariner;
                wc.IsCaptain = troop.IsCaptain;
                wc.IsCaptainEnabled = troop.CaptainEnabled;

                if (troop.Captain?.StringId != null)
                {
                    var captainWc = WCharacter.Get(troop.Captain.StringId);
                    if (captainWc != null)
                        wc.Captain = captainWc;
                }

                // Skill points
                if (
                    _xp.XpPools != null
                    && _xp.XpPools.TryGetValue(troop.StringId, out var rawXp)
                    && rawXp > 0
                )
                {
                    // v1 XP is unspent raw experience. Convert to skill points
                    // using the v1 default cost formula: cost(n) = 100 + 1 * n.
                    wc.SkillPoints = XpToSkillPoints(rawXp);
                }

                // Combat history
                if (_stats.Stats != null && _stats.Stats.TryGetValue(troop.StringId, out var s))
                {
                    wc.ImportLegacyHistory(
                        s.BattlesWon,
                        s.BattlesLost,
                        s.FieldBattles,
                        s.SiegeBattles,
                        s.KillsByTroopId,
                        s.DeathsByTroopId
                    );
                }
            }
        }

        // ─── Item data ─────────────────────────────────────────────────────

        private void MigrateItemData()
        {
            // Migrate fully-unlocked items first (sets progress to threshold)
            if (_unlocks.UnlockedItemIds != null)
            {
                foreach (var itemId in _unlocks.UnlockedItemIds)
                {
                    if (string.IsNullOrEmpty(itemId))
                        continue;
                    var wi = WItem.Get(itemId);
                    if (wi == null)
                        continue;
                    wi.UnlockProgress = WItem.UnlockThreshold;
                }
            }

            // Migrate partial progress (do not overwrite already-unlocked)
            if (_unlocks.ProgressByItemId != null)
            {
                foreach (var kvp in _unlocks.ProgressByItemId)
                {
                    if (string.IsNullOrEmpty(kvp.Key))
                        continue;
                    var wi = WItem.Get(kvp.Key);
                    if (wi == null)
                        continue;
                    if (wi.IsUnlocked)
                        continue; // already handled above
                    wi.UnlockProgress = kvp.Value;
                }
            }

            // Migrate stocks
            if (_stocks.StocksByItemId != null)
            {
                foreach (var kvp in _stocks.StocksByItemId)
                {
                    if (string.IsNullOrEmpty(kvp.Key) || kvp.Value <= 0)
                        continue;
                    var wi = WItem.Get(kvp.Key);
                    if (wi == null)
                        continue;
                    wi.Stock = kvp.Value;
                }
            }
        }

        // ─── Doctrines ─────────────────────────────────────────────────────

        private void MigrateDoctrines()
        {
            if (_doctrines.UnlockedDoctrines == null)
                return;

            DoctrinesRegistry.EnsureRegistered();

            foreach (var v1Key in _doctrines.UnlockedDoctrines)
            {
                var v2Id = DoctrineKeyMap.ToV2Id(v1Key);
                if (v2Id == null)
                {
                    Log.Warning($"[Migration] No v2 mapping for v1 doctrine '{v1Key}' – skipping.");
                    continue;
                }

                var doctrine = DoctrinesRegistry.GetDoctrine(v2Id);
                if (doctrine == null)
                {
                    Log.Warning(
                        $"[Migration] v2 doctrine '{v2Id}' not found in registry – skipping."
                    );
                    continue;
                }

                doctrine.ForceSet(Doctrine.ProgressTarget);
                doctrine.IsAcquired = true;
            }
        }

        private void MigrateFeatProgress()
        {
            if (_doctrines.FeatProgress == null || _doctrines.FeatProgress.Count == 0)
                return;

            DoctrinesRegistry.EnsureRegistered();

            // Build reverse map: v2 feat ID → owning doctrine (to skip acquired ones).
            var featToDoctrine = new Dictionary<string, Doctrine>(StringComparer.Ordinal);
            foreach (var d in DoctrinesRegistry.GetDoctrines())
            foreach (var f in d.Feats)
                featToDoctrine[f.Id] = d;

            foreach (var kvp in _doctrines.FeatProgress)
            {
                if (kvp.Value <= 0)
                    continue;

                var mappings = FeatKeyMap.GetMappings(kvp.Key);
                if (mappings == null)
                    continue;

                foreach (var m in mappings)
                {
                    // Skip feats whose doctrine was already fully acquired by MigrateDoctrines().
                    if (
                        featToDoctrine.TryGetValue(m.V2FeatId, out var owningDoctrine)
                        && owningDoctrine.IsAcquired
                    )
                        continue;

                    var feat = DoctrinesRegistry.GetFeat(m.V2FeatId);
                    if (feat == null)
                    {
                        Log.Warning($"[Migration] v2 feat '{m.V2FeatId}' not found – skipping.");
                        continue;
                    }

                    // Proportionally scale v1 progress into the v2 target range.
                    var v2Progress = Math.Min(kvp.Value, m.V1Target) * m.V2Target / m.V1Target;
                    if (v2Progress <= 0)
                        continue;

                    feat.ForceSet(v2Progress);
                }
            }

            // Recompute doctrine-level progress from newly set feat completions.
            foreach (var doctrine in DoctrinesRegistry.GetDoctrines())
            {
                if (doctrine.IsAcquired)
                    continue;

                var earned = 0;
                foreach (var feat in doctrine.Feats)
                    if (feat.Progress >= feat.Target)
                        earned += feat.Worth;

                if (earned > 0)
                    doctrine.ForceSet(Math.Min(earned, Doctrine.ProgressTarget));
            }
        }

        // ─── Helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Converts raw v1 unspent XP to a number of v2 skill points using the
        /// v1 default cost formula: <c>cost(n) = baseCost + perPoint * n</c>
        /// where baseCost = 100 and perPoint = 1.
        /// </summary>
        private static int XpToSkillPoints(int xp)
        {
            const int baseCost = 100; // Config.BaseSkillXpCost default
            const int perPoint = 1; // Config.SkillXpCostPerPoint default
            int points = 0;
            while (xp >= baseCost + perPoint * points)
            {
                xp -= baseCost + perPoint * points;
                points++;
            }
            return points;
        }

        /// <summary>
        /// Lazily yields every <see cref="TroopSaveData"/> from all four
        /// faction save slots, including nested UpgradeTargets and Captains.
        /// </summary>
        private IEnumerable<TroopSaveData> FlattenAllTroops()
        {
            if (_faction.ClanTroops != null)
                foreach (var t in FlattenFaction(_faction.ClanTroops))
                    yield return t;

            if (_faction.KingdomTroops != null)
                foreach (var t in FlattenFaction(_faction.KingdomTroops))
                    yield return t;

            if (_faction.CultureTroops != null)
                foreach (var f in _faction.CultureTroops)
                    if (f != null)
                        foreach (var t in FlattenFaction(f))
                            yield return t;

            if (_faction.MinorClanTroops != null)
                foreach (var f in _faction.MinorClanTroops)
                    if (f != null)
                        foreach (var t in FlattenFaction(f))
                            yield return t;
        }

        private static IEnumerable<TroopSaveData> FlattenFaction(FactionSaveData f)
        {
            foreach (var t in FlattenTroop(f.RetinueElite))
                yield return t;
            foreach (var t in FlattenTroop(f.RetinueBasic))
                yield return t;
            foreach (var t in FlattenTroop(f.RootElite))
                yield return t;
            foreach (var t in FlattenTroop(f.RootBasic))
                yield return t;
            foreach (var t in FlattenTroop(f.MilitiaMelee))
                yield return t;
            foreach (var t in FlattenTroop(f.MilitiaMeleeElite))
                yield return t;
            foreach (var t in FlattenTroop(f.MilitiaRanged))
                yield return t;
            foreach (var t in FlattenTroop(f.MilitiaRangedElite))
                yield return t;
            foreach (var t in FlattenTroop(f.CaravanGuard))
                yield return t;
            foreach (var t in FlattenTroop(f.CaravanMaster))
                yield return t;
            foreach (var t in FlattenTroop(f.Villager))
                yield return t;
            foreach (var t in FlattenTroop(f.PrisonGuard))
                yield return t;

            if (f.Civilians != null)
                foreach (var troop in f.Civilians)
                foreach (var t in FlattenTroop(troop))
                    yield return t;

            if (f.Bandits != null)
                foreach (var troop in f.Bandits)
                foreach (var t in FlattenTroop(troop))
                    yield return t;

            if (f.Heroes != null)
                foreach (var troop in f.Heroes)
                foreach (var t in FlattenTroop(troop))
                    yield return t;

            if (f.Mercenaries != null)
                foreach (var troop in f.Mercenaries)
                foreach (var t in FlattenTroop(troop))
                    yield return t;
        }

        /// <summary>
        /// Recursively yields <paramref name="troop"/> and all nested
        /// UpgradeTargets and Captain references.
        /// </summary>
        private static IEnumerable<TroopSaveData> FlattenTroop(TroopSaveData troop)
        {
            if (troop == null)
                yield break;
            yield return troop;

            if (troop.Captain != null)
                foreach (var t in FlattenTroop(troop.Captain))
                    yield return t;

            if (troop.UpgradeTargets != null)
                foreach (var up in troop.UpgradeTargets)
                foreach (var t in FlattenTroop(up))
                    yield return t;
        }
    }
}
