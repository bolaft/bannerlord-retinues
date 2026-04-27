using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Behaviors.Doctrines;
using Retinues.Behaviors.Doctrines.Definitions;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Migration.Legacy;
using Retinues.Migration.Shims;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

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
                MigrateFactionRoots();
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
            var troopByStringId = new Dictionary<string, TroopSaveData>(StringComparer.Ordinal);
            int migrated = 0,
                skipped = 0;

            foreach (var troop in FlattenAllTroops())
            {
                if (string.IsNullOrEmpty(troop.StringId))
                    continue;

                if (!seen.Add(troop.StringId))
                    continue;

                var wc = WCharacter.Get(troop.StringId);
                if (wc == null)
                {
                    Log.Debug($"[Migration] Troop '{troop.StringId}' not found in v2 – skipping.");
                    skipped++;
                    continue; // troop does not exist in v2 – skip
                }

                troopByStringId[troop.StringId] = troop;

                // Mark stub as active so it isn't reclaimed by GetFreeStub().
                if (wc.IsCustom)
                    wc.IsActiveStub = true;

                // ── Core identity ─────────────────────────────────────────
                if (!string.IsNullOrEmpty(troop.Name))
                    wc.Name = troop.Name;
                wc.Level = troop.Level;
                wc.IsFemale = troop.IsFemale;
                if (troop.Race >= 0)
                    wc.Race = troop.Race;
                if (!string.IsNullOrEmpty(troop.CultureId))
                {
                    var co = MBObjectManager.Instance.GetObject<CultureObject>(troop.CultureId);
                    if (co != null)
                        wc.Culture = WCulture.Get(co);
                }

                // ── Formation / captain flags ─────────────────────────────
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

                // ── Skills ────────────────────────────────────────────────
                if (!string.IsNullOrWhiteSpace(troop.SkillData?.Code))
                {
                    foreach (var part in troop.SkillData.Code.Split(';'))
                    {
                        var kv = part.Split(':');
                        if (kv.Length != 2)
                            continue;
                        var skill = MBObjectManager.Instance.GetObject<SkillObject>(kv[0].Trim());
                        if (skill == null)
                            continue;
                        if (!int.TryParse(kv[1].Trim(), out var sv) || sv <= 0)
                            continue;
                        wc.Skills.Set(skill, sv);
                    }
                }

                // ── Equipment ─────────────────────────────────────────────
                if (troop.EquipmentData?.Codes?.Count > 0)
                {
                    bool hasCivilianFlags =
                        troop.EquipmentData.Civilians?.Count == troop.EquipmentData.Codes.Count;
                    var roster = new List<MEquipment>();
                    for (int i = 0; i < troop.EquipmentData.Codes.Count; i++)
                    {
                        var code = troop.EquipmentData.Codes[i];
                        if (string.IsNullOrEmpty(code))
                            continue;
                        var me = MEquipment.FromCode(wc, code);
                        if (me == null)
                            continue;
                        me.IsCivilian = hasCivilianFlags
                            ? troop.EquipmentData.Civilians[i]
                            : (i == 1);
                        roster.Add(me);
                    }
                    if (roster.Count > 0)
                        wc.EquipmentRoster.Equipments = roster;
                }

                // ── Body ──────────────────────────────────────────────────
                if (troop.BodyData != null)
                {
                    if (troop.BodyData.AgeMin > 0)
                        wc.AgeMin = troop.BodyData.AgeMin;
                    if (troop.BodyData.AgeMax > 0)
                        wc.AgeMax = troop.BodyData.AgeMax;
                    if (troop.BodyData.WeightMin > 0)
                        wc.WeightMin = troop.BodyData.WeightMin;
                    if (troop.BodyData.WeightMax > 0)
                        wc.WeightMax = troop.BodyData.WeightMax;
                    if (troop.BodyData.BuildMin > 0)
                        wc.BuildMin = troop.BodyData.BuildMin;
                    if (troop.BodyData.BuildMax > 0)
                        wc.BuildMax = troop.BodyData.BuildMax;
                    if (troop.BodyData.HeightMin > 0)
                        wc.HeightMin = troop.BodyData.HeightMin;
                    if (troop.BodyData.HeightMax > 0)
                        wc.HeightMax = troop.BodyData.HeightMax;
                }

                // ── Skill points (from legacy XP pool) ───────────────────
                if (
                    _xp.XpPools != null
                    && _xp.XpPools.TryGetValue(troop.StringId, out var rawXp)
                    && rawXp > 0
                )
                {
                    // v1 XP is unspent raw experience. Convert to skill points
                    // using the v1 default cost formula: cost(n) = 100 + 1 * n.
                    wc.SkillPoints = XpToSkillPoints(rawXp);
                    Log.Debug(
                        $"[Migration] {troop.StringId}: {rawXp} XP → {wc.SkillPoints} skill points."
                    );
                }

                // ── Combat history ────────────────────────────────────────
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

                migrated++;
            }

            // ── Second pass: wire upgrade trees ───────────────────────────
            // All definitions are applied above; now link each troop to its upgrade targets.
            int treeWired = 0;
            foreach (var kvp in troopByStringId)
            {
                var id = kvp.Key;
                var troop = kvp.Value;
                if (troop.UpgradeTargets == null || troop.UpgradeTargets.Count == 0)
                    continue;

                var wc = WCharacter.Get(id);
                if (wc == null)
                    continue;

                var targets = troop
                    .UpgradeTargets.Where(t => !string.IsNullOrEmpty(t?.StringId))
                    .Select(t => WCharacter.Get(t.StringId))
                    .Where(w => w != null)
                    .ToList();

                if (targets.Count > 0)
                {
                    wc.UpgradeTargets = targets;
                    treeWired++;
                }
            }

            Log.Info(
                $"[Migration] Characters: {migrated} migrated, {skipped} skipped (not in v2); {treeWired} upgrade trees wired."
            );
        }

        // ─── Item data ─────────────────────────────────────────────────────

        private void MigrateItemData()
        {
            int unlocked = 0,
                partial = 0,
                stocks = 0;

            // Migrate fully-unlocked items first (sets progress to threshold)
            if (_unlocks.UnlockedItemIds != null)
            {
                foreach (var itemId in _unlocks.UnlockedItemIds)
                {
                    if (string.IsNullOrEmpty(itemId))
                        continue;
                    var wi = WItem.Get(itemId);
                    if (wi == null)
                    {
                        Log.Debug(
                            $"[Migration] Item '{itemId}' not found in v2 – skipping unlock."
                        );
                        continue;
                    }
                    wi.UnlockProgress = WItem.UnlockThreshold;
                    unlocked++;
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
                    partial++;
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
                    stocks++;
                }
            }

            Log.Info(
                $"[Migration] Items: {unlocked} unlocked, {partial} partial, {stocks} stocks."
            );
        }

        // ─── Doctrines ─────────────────────────────────────────────────────

        private void MigrateDoctrines()
        {
            if (_doctrines.UnlockedDoctrines == null)
            {
                Log.Debug("[Migration] No unlocked doctrines in save – skipping.");
                return;
            }

            DoctrinesRegistry.EnsureRegistered();
            int migrated = 0;

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
                Log.Debug($"[Migration] Doctrine '{v1Key}' → '{v2Id}' acquired.");
                migrated++;
            }

            Log.Info(
                $"[Migration] Doctrines: {migrated}/{_doctrines.UnlockedDoctrines.Count} migrated."
            );
        }

        private void MigrateFeatProgress()
        {
            if (_doctrines.FeatProgress == null || _doctrines.FeatProgress.Count == 0)
            {
                Log.Debug("[Migration] No feat progress in save – skipping.");
                return;
            }

            DoctrinesRegistry.EnsureRegistered();
            int migrated = 0,
                noMapping = 0;

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
                {
                    Log.Debug($"[Migration] No v2 feat mapping for '{kvp.Key}' – skipping.");
                    noMapping++;
                    continue;
                }

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
                    Log.Debug(
                        $"[Migration] Feat '{kvp.Key}' → '{m.V2FeatId}': {kvp.Value}/{m.V1Target} → {v2Progress}/{m.V2Target}."
                    );
                    migrated++;
                }
            }

            // Recompute doctrine-level progress from newly set feat completions.
            int doctrinesGainedProgress = 0;
            foreach (var doctrine in DoctrinesRegistry.GetDoctrines())
            {
                if (doctrine.IsAcquired)
                    continue;

                var earned = 0;
                foreach (var feat in doctrine.Feats)
                    if (feat.Progress >= feat.Target)
                        earned += feat.Worth;

                if (earned > 0)
                {
                    doctrine.ForceSet(Math.Min(earned, Doctrine.ProgressTarget));
                    Log.Debug(
                        $"[Migration] Doctrine '{doctrine.Id}' progress set to {doctrine.Progress}/{Doctrine.ProgressTarget} from feat completions."
                    );
                    doctrinesGainedProgress++;
                }
            }

            Log.Info(
                $"[Migration] Feats: {migrated} migrated, {noMapping} without v2 mapping; {doctrinesGainedProgress} doctrine(s) gained partial progress."
            );
        }

        // ─── Faction roots (clan / kingdom) ───────────────────────────────────

        /// <summary>
        /// Re-wires clan and kingdom faction root assignments that were stored
        /// only in-memory in v1 (not in BL native saves) and are therefore absent
        /// when loading a legacy save in v2.
        /// <para/>
        /// WCulture and minor-clan roots are read from BL-native CultureObject
        /// properties which ARE persisted by BL – no re-wiring needed for those.
        /// </summary>
        private void MigrateFactionRoots()
        {
            if (_faction.ClanTroops != null && Clan.PlayerClan != null)
            {
                var wClan = WClan.Get(Clan.PlayerClan.StringId);
                if (wClan != null)
                {
                    ApplyRoot(wClan.SetRootBasic, _faction.ClanTroops.RootBasic);
                    ApplyRoot(wClan.SetRootElite, _faction.ClanTroops.RootElite);

                    var clanRetinues = new List<WCharacter>();
                    var re = ResolveCharacter(_faction.ClanTroops.RetinueElite);
                    var rb = ResolveCharacter(_faction.ClanTroops.RetinueBasic);
                    if (re != null)
                        clanRetinues.Add(re);
                    if (rb != null)
                        clanRetinues.Add(rb);
                    if (clanRetinues.Count > 0)
                        wClan.SetRetinues(clanRetinues);

                    ApplyRoot(wClan.SetMeleeMilitiaTroop, _faction.ClanTroops.MilitiaMelee);
                    ApplyRoot(
                        wClan.SetMeleeEliteMilitiaTroop,
                        _faction.ClanTroops.MilitiaMeleeElite
                    );
                    ApplyRoot(wClan.SetRangedMilitiaTroop, _faction.ClanTroops.MilitiaRanged);
                    ApplyRoot(
                        wClan.SetRangedEliteMilitiaTroop,
                        _faction.ClanTroops.MilitiaRangedElite
                    );
                    ApplyRoot(wClan.SetCaravanGuard, _faction.ClanTroops.CaravanGuard);
                    ApplyRoot(wClan.SetCaravanMaster, _faction.ClanTroops.CaravanMaster);
                    ApplyRoot(wClan.SetVillager, _faction.ClanTroops.Villager);

                    Log.Info($"[Migration] Clan roots wired for '{Clan.PlayerClan.Name}'.");
                }
            }

            if (_faction.KingdomTroops != null && Clan.PlayerClan?.Kingdom != null)
            {
                var wKingdom = WKingdom.Get(Clan.PlayerClan.Kingdom.StringId);
                if (wKingdom != null)
                {
                    ApplyRoot(wKingdom.SetRootBasic, _faction.KingdomTroops.RootBasic);
                    ApplyRoot(wKingdom.SetRootElite, _faction.KingdomTroops.RootElite);

                    var kgRetinues = new List<WCharacter>();
                    var re = ResolveCharacter(_faction.KingdomTroops.RetinueElite);
                    var rb = ResolveCharacter(_faction.KingdomTroops.RetinueBasic);
                    if (re != null)
                        kgRetinues.Add(re);
                    if (rb != null)
                        kgRetinues.Add(rb);
                    if (kgRetinues.Count > 0)
                        wKingdom.SetRetinues(kgRetinues);

                    ApplyRoot(wKingdom.SetMeleeMilitiaTroop, _faction.KingdomTroops.MilitiaMelee);
                    ApplyRoot(
                        wKingdom.SetMeleeEliteMilitiaTroop,
                        _faction.KingdomTroops.MilitiaMeleeElite
                    );
                    ApplyRoot(wKingdom.SetRangedMilitiaTroop, _faction.KingdomTroops.MilitiaRanged);
                    ApplyRoot(
                        wKingdom.SetRangedEliteMilitiaTroop,
                        _faction.KingdomTroops.MilitiaRangedElite
                    );
                    ApplyRoot(wKingdom.SetCaravanGuard, _faction.KingdomTroops.CaravanGuard);
                    ApplyRoot(wKingdom.SetCaravanMaster, _faction.KingdomTroops.CaravanMaster);
                    ApplyRoot(wKingdom.SetVillager, _faction.KingdomTroops.Villager);

                    Log.Info(
                        $"[Migration] Kingdom roots wired for '{Clan.PlayerClan.Kingdom.Name}'."
                    );
                }
            }
        }

        private static void ApplyRoot(Action<WCharacter> setter, TroopSaveData troop)
        {
            var wc = ResolveCharacter(troop);
            if (wc != null)
                setter(wc);
        }

        private static WCharacter ResolveCharacter(TroopSaveData troop) =>
            string.IsNullOrEmpty(troop?.StringId) ? null : WCharacter.Get(troop.StringId);

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
