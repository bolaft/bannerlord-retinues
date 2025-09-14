using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using Retinues.Core.Utils;

namespace Retinues.Core.Game.Features.Doctrines
{
    public sealed class DoctrineServiceBehavior : CampaignBehaviorBase
    {
        // -------- State (persisted using type-key strings) --------
        private HashSet<string> _unlocked = []; // doctrineKey -> unlocked
        private Dictionary<string, int> _featProgress = []; // featKey -> current

        // -------- Definitions (rebuilt on load) --------
        private Dictionary<string, DoctrineDef> _defsByKey = [];
        private readonly Dictionary<string, string> _featToDoctrine = []; // featKey -> doctrineKey

        public override void RegisterEvents()
        {
            Log.Debug("Registering doctrine service events");
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, _ =>
            {
                BuildCatalogIfNeeded();
            });
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Persist state by string keys (Type.FullName)
            var unlockedList = _unlocked?.ToList() ?? [];
            dataStore.SyncData("Retinues_Doctrines_Unlocked", ref unlockedList);
            _unlocked = unlockedList != null ? unlockedList.ToHashSet() : [];

            dataStore.SyncData("Retinues_Doctrines_FeatProgress", ref _featProgress);
            _featProgress ??= [];
        }

        // Public API uses *string keys* internally; DoctrineAPI will expose type-based overloads.
        public IEnumerable<DoctrineDef> AllDoctrines() => _defsByKey.Values.OrderBy(d => d.Column).ThenBy(d => d.Row);
        public DoctrineDef GetDoctrine(string key) => _defsByKey.TryGetValue(key, out var d) ? d : null;
        public bool IsDoctrineUnlocked(string key) => _unlocked.Contains(key);

        public DoctrineStatus GetDoctrineStatus(string key)
        {
            var def = GetDoctrine(key);
            if (def == null) return DoctrineStatus.Locked;

            if (_unlocked.Contains(key)) return DoctrineStatus.Unlocked;

            // prereq
            if (!string.IsNullOrEmpty(def.PrerequisiteKey) && !_unlocked.Contains(def.PrerequisiteKey))
                return DoctrineStatus.Locked;

            // feats
            var feats = def.Feats;
            if (feats == null || feats.Count == 0) return DoctrineStatus.InProgress;

            if (feats.Any(f => !IsFeatComplete(f.Key))) return DoctrineStatus.Unlockable;
            return DoctrineStatus.InProgress;
        }

        public bool TryAcquireDoctrine(string key, out string reason)
        {
            reason = null;
            var def = GetDoctrine(key);
            if (def == null) { reason = "Unknown doctrine."; return false; }

            var status = GetDoctrineStatus(key);
            if (status != DoctrineStatus.InProgress)
            {
                reason = status == DoctrineStatus.Locked ? "Prerequisite not met." : "Feats incomplete.";
                return false;
            }

            var gold = Hero.MainHero?.Gold ?? 0;
            var influence = Clan.PlayerClan?.Influence ?? 0f;

            if (gold < def.GoldCost) { reason = "Not enough gold."; return false; }
            if (influence < def.InfluenceCost) { reason = "Not enough influence."; return false; }

            Player.ChangeGold(-def.GoldCost);
            Player.ChangeInfluence(-def.InfluenceCost);

            _unlocked.Add(key);
            DoctrineUnlocked?.Invoke(key);
            return true;
        }

        // -------- Feats (by feat key string) --------

        public int GetFeatTarget(string featKey)
        {
            var dKey = _featToDoctrine.TryGetValue(featKey, out var did) ? did : null;
            if (dKey == null) return 0;
            var feat = GetDoctrine(dKey)?.Feats?.FirstOrDefault(f => f.Key == featKey);
            return feat?.Target ?? 0;
        }

        public int GetFeatProgress(string featKey) => _featProgress.TryGetValue(featKey, out var v) ? v : 0;

        public bool IsFeatComplete(string featKey)
        {
            int target = GetFeatTarget(featKey);
            if (target <= 0) return true;
            return GetFeatProgress(featKey) >= target;
        }

        public void SetFeatProgress(string featKey, int amount)
        {
            if (string.IsNullOrEmpty(featKey) || amount < 0) return;
            if (!_featToDoctrine.ContainsKey(featKey)) return;

            int target = GetFeatTarget(featKey);
            int next = Math.Min(target <= 0 ? amount : amount, target > 0 ? target : int.MaxValue);

            _featProgress[featKey] = next;

            Log.Info($"Set feat progress: {featKey} = {next}/{target}");

            if (target > 0 && next >= target) FeatCompleted?.Invoke(featKey);
        }

        public int AdvanceFeat(string featKey, int amount = 1)
        {
            if (string.IsNullOrEmpty(featKey) || amount <= 0) return GetFeatProgress(featKey);
            if (!_featToDoctrine.ContainsKey(featKey)) return GetFeatProgress(featKey);

            int target = GetFeatTarget(featKey);
            int cur = GetFeatProgress(featKey);
            int next = Math.Min(target <= 0 ? cur + amount : cur + amount, target > 0 ? target : int.MaxValue);

            _featProgress[featKey] = next;

            Log.Info($"Advanced feat progress: {featKey} = {next}/{target}");

            if (target > 0 && cur < target && next >= target) FeatCompleted?.Invoke(featKey);
            return next;
        }

        // -------- Catalog build from types --------

        private void BuildCatalogIfNeeded()
        {
            if (_defsByKey.Count > 0) return;

            var doctrines = DoctrineCatalog.DiscoverDoctrines(); // type discovery
            var defs = new List<DoctrineDef>();
            _featToDoctrine.Clear();

            foreach (var d in doctrines)
            {
                var key = d.Key; // Type.FullName
                var feats = new List<FeatDef>();

                foreach (var f in d.InstantiateFeats())
                {
                    var fKey = f.Key;
                    feats.Add(new FeatDef { Key = fKey, Description = f.Description, Target = f.Target });
                    _featToDoctrine[fKey] = key;
                    f.OnRegister(); // lifecycle hook
                }

                defs.Add(new DoctrineDef
                {
                    Key = key,
                    Name = d.Name,
                    Description = d.Description,
                    Column = d.Column,
                    Row = d.Row,
                    // PrerequisiteKey will be set in a second pass
                    GoldCost = d.GoldCost,
                    InfluenceCost = d.InfluenceCost,
                    Feats = feats
                });
            }

            // Second pass: prerequisite = doctrine one row above in same column
            var byPos = new Dictionary<(int col, int row), DoctrineDef>();
            foreach (var def in defs)
                byPos[(def.Column, def.Row)] = def;

            foreach (var def in defs)
            {
                if (def.Row <= 0)
                {
                    def.PrerequisiteKey = null;
                    continue;
                }

                if (byPos.TryGetValue((def.Column, def.Row - 1), out var prev))
                    def.PrerequisiteKey = prev.Key;
                else
                    def.PrerequisiteKey = null; // optional: log a warning if you expect a strict 4x4
            }

            _defsByKey = defs.ToDictionary(d => d.Key, d => d);
        }

        // -------- Events --------

        public event Action<string> DoctrineUnlocked; // doctrineKey
        public event Action<string> FeatCompleted; // featKey
    }
}
