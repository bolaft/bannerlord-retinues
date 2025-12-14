using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Doctrines.Model;
using Retinues.Game;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace OldRetinues.Doctrines
{
    /// <summary>
    /// Campaign behavior for doctrine and feat management.
    /// Tracks unlocked doctrines, feat progress, and builds doctrine catalog from discovered types.
    /// </summary>
    [SafeClass]
    public sealed class DoctrineServiceBehavior : CampaignBehaviorBase
    {
        /* ━━━━━━━━━ State ━━━━━━━━ */

        private HashSet<string> _unlocked = []; // doctrineKey -> unlocked
        private Dictionary<string, int> _featProgress = []; // featKey -> current

        /* ━━━━━━ Definitions ━━━━━ */

        private Dictionary<string, DoctrineDefinition> _defsByKey = [];
        private readonly Dictionary<string, string> _featToDoctrine = []; // featKey -> doctrineKey

        /* ━━━━━━ Live Models ━━━━━ */

        private Dictionary<string, Doctrine> _modelsByKey = [];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Syncs unlocked doctrines and feat progress by string keys.
        /// </summary>
        public override void SyncData(IDataStore dataStore)
        {
            // Persist state by string keys (Type.FullName)
            var unlockedList = _unlocked?.ToList() ?? [];
            dataStore.SyncData("Retinues_Doctrines_Unlocked", ref unlockedList);
            _unlocked = unlockedList != null ? [.. unlockedList] : [];

            dataStore.SyncData("Retinues_Doctrines_FeatProgress", ref _featProgress);
            _featProgress ??= [];
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Registers session launch event to build doctrine catalog.
        /// </summary>
        public override void RegisterEvents()
        {
            Log.Debug("Registering doctrine service events");
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                this,
                _ =>
                {
                    BuildCatalogIfNeeded();
                }
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public event Action<string> DoctrineUnlocked; // doctrineKey
        public event Action<string> FeatCompleted; // featKey
        public event Action CatalogBuilt; // catalog built

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns all doctrine definitions, ordered by grid position.
        /// </summary>
        public IEnumerable<DoctrineDefinition> AllDoctrines() =>
            _defsByKey.Values.OrderBy(d => d.Column).ThenBy(d => d.Row);

        /// <summary>
        /// Returns the doctrine definition for the given key.
        /// </summary>
        public DoctrineDefinition GetDoctrine(string key) =>
            _defsByKey.TryGetValue(key, out var d) ? d : null;

        /// <summary>
        /// Returns true if the doctrine is unlocked.
        /// </summary>
        public bool IsDoctrineUnlocked(string key) => _unlocked.Contains(key);

        /// <summary>
        /// Gets the status of a doctrine (locked, unlockable, in progress, unlocked).
        /// </summary>
        public DoctrineStatus GetDoctrineStatus(string key)
        {
            var def = GetDoctrine(key);
            if (def == null)
                return DoctrineStatus.Locked;

            if (_unlocked.Contains(key))
                return DoctrineStatus.Unlocked;

            // prereq
            var effectivePrereq = GetEffectivePrerequisiteKey(key);
            if (!string.IsNullOrEmpty(effectivePrereq) && !_unlocked.Contains(effectivePrereq))
                return DoctrineStatus.Locked;

            // ignore feats if disabled
            if (Config.EnableFeatRequirements == false)
                return DoctrineStatus.InProgress;

            // feats
            var feats = def.Feats;
            if (feats == null || feats.Count == 0)
                return DoctrineStatus.InProgress;

            if (feats.Any(f => !IsFeatComplete(f.Key)))
                return DoctrineStatus.Unlockable;
            return DoctrineStatus.InProgress;
        }

        /// <summary>
        /// Returns the effective prerequisite for a doctrine, skipping any disabled doctrines.
        /// </summary>
        private string GetEffectivePrerequisiteKey(string key)
        {
            var def = GetDoctrine(key);
            if (def == null)
                return null;

            var prereqKey = def.PrerequisiteKey;
            if (string.IsNullOrEmpty(prereqKey))
                return null;

            // Guard against weird cycles
            var visited = new HashSet<string>();

            while (!string.IsNullOrEmpty(prereqKey) && visited.Add(prereqKey))
            {
                // If this prerequisite is not disabled, it is the effective prerequisite.
                if (!IsDoctrineDisabled(prereqKey))
                    return prereqKey;

                // Otherwise, skip it and look at its own prerequisite.
                var prereqDef = GetDoctrine(prereqKey);
                if (prereqDef == null)
                    break;

                prereqKey = prereqDef.PrerequisiteKey;
            }

            // No non-disabled prerequisite found.
            return null;
        }

        /// <summary>
        /// Attempts to acquire a doctrine, paying costs if eligible.
        /// </summary>
        public bool TryAcquireDoctrine(string key, out string reason)
        {
            reason = null;
            var def = GetDoctrine(key);
            if (def == null)
            {
                reason = "Unknown doctrine.";
                return false;
            }

            var status = GetDoctrineStatus(key);
            if (status != DoctrineStatus.InProgress)
            {
                reason =
                    status == DoctrineStatus.Locked ? "Prerequisite not met." : "Feats incomplete.";
                return false;
            }

            var gold = Hero.MainHero?.Gold ?? 0;
            var influence = Clan.PlayerClan?.Influence ?? 0f;

            if (gold < def.GoldCost)
            {
                reason = "Not enough gold.";
                return false;
            }
            if (influence < def.InfluenceCost)
            {
                reason = "Not enough influence.";
                return false;
            }

            Player.ChangeGold(-def.GoldCost);
            Player.ChangeInfluence(-def.InfluenceCost);

            _unlocked.Add(key);
            DoctrineUnlocked?.Invoke(key);
            return true;
        }

        /* ━━━━ Feats (by key) ━━━━ */

        /// <summary>
        /// Gets the target value for a feat by key.
        /// </summary>
        public int GetFeatTarget(string featKey)
        {
            if (Config.EnableFeatRequirements == false)
                return 0;
            var dKey = _featToDoctrine.TryGetValue(featKey, out var did) ? did : null;
            if (dKey == null)
                return 0;
            var feat = GetDoctrine(dKey)?.Feats?.FirstOrDefault(f => f.Key == featKey);
            return feat?.Target ?? 0;
        }

        /// <summary>
        /// Gets the current progress for a feat by key.
        /// </summary>
        public int GetFeatProgress(string featKey) =>
            _featProgress.TryGetValue(featKey, out var v) ? v : 0;

        /// <summary>
        /// Returns true if the feat is complete (progress >= target).
        /// </summary>
        public bool IsFeatComplete(string featKey)
        {
            if (Config.EnableFeatRequirements == false)
                return true;
            int target = GetFeatTarget(featKey);
            if (target <= 0)
                return true;
            return GetFeatProgress(featKey) >= target;
        }

        /// <summary>
        /// Sets the progress for a feat by key.
        /// </summary>
        public void SetFeatProgress(string featKey, int amount)
        {
            if (string.IsNullOrEmpty(featKey) || amount < 0)
                return;
            if (!_featToDoctrine.ContainsKey(featKey))
                return;

            int target = GetFeatTarget(featKey);
            int next = Math.Min(target <= 0 ? amount : amount, target > 0 ? target : int.MaxValue);

            _featProgress[featKey] = next;

            if (target > 0 && next >= target)
                FeatCompleted?.Invoke(featKey);
        }

        /// <summary>
        /// Advances the progress for a feat by key.
        /// </summary>
        public int AdvanceFeat(string featKey, int amount = 1)
        {
            if (string.IsNullOrEmpty(featKey) || amount <= 0)
                return GetFeatProgress(featKey);
            if (!_featToDoctrine.ContainsKey(featKey))
                return GetFeatProgress(featKey);

            int target = GetFeatTarget(featKey);
            int cur = GetFeatProgress(featKey);
            int next = Math.Min(
                target <= 0 ? cur + amount : cur + amount,
                target > 0 ? target : int.MaxValue
            );

            _featProgress[featKey] = next;

            if (target > 0 && cur < target && next >= target)
                FeatCompleted?.Invoke(featKey);
            return next;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Builds the doctrine catalog from discovered types and sets up prerequisites.
        /// </summary>
        private void BuildCatalogIfNeeded()
        {
            if (_defsByKey.Count > 0)
                return;

            Log.Debug("Building doctrine catalog...");

            var doctrines = DoctrineDiscovery.DiscoverDoctrines(); // type discovery
            var defs = new List<DoctrineDefinition>();
            _featToDoctrine.Clear();
            _modelsByKey = doctrines.ToDictionary(d => d.Key, d => d);

            foreach (var d in doctrines)
            {
                var key = d.Key; // Type.FullName
                var feats = new List<FeatDefinition>();

                foreach (var f in d.InstantiateFeats())
                {
                    var fKey = f.Key;
                    feats.Add(
                        new FeatDefinition
                        {
                            Key = fKey,
                            Description = f.Description,
                            Target = f.Target,
                        }
                    );
                    _featToDoctrine[fKey] = key;
                    f.OnRegister(); // lifecycle hook
                }

                defs.Add(
                    new DoctrineDefinition
                    {
                        Key = key,
                        Name = d.Name,
                        Description = d.Description,
                        Column = d.Column,
                        Row = d.Row,
                        // PrerequisiteKey will be set in a second pass
                        GoldCost = d.GoldCost,
                        InfluenceCost = d.InfluenceCost,
                        Feats = feats,
                    }
                );
            }

            // Second pass: prerequisite = doctrine one row above in same column
            var byPos = new Dictionary<(int col, int row), DoctrineDefinition>();
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

            CatalogBuilt?.Invoke();
        }

        /// <summary>
        /// Returns true if the doctrine is disabled according to its model.
        /// Evaluated against the current config.
        /// </summary>
        public bool IsDoctrineDisabled(string key)
        {
            if (_modelsByKey != null && _modelsByKey.TryGetValue(key, out var model))
                return model.IsDisabled;

            return false;
        }

        /// <summary>
        /// Returns the doctrine description, swapping to the model's DisabledMessage when disabled.
        /// </summary>
        public TextObject GetDoctrineDescription(string key)
        {
            var def = GetDoctrine(key);
            if (def == null)
                return new TextObject(string.Empty);

            if (_modelsByKey != null && _modelsByKey.TryGetValue(key, out var model))
            {
                if (model.IsDisabled)
                    return model.DisabledMessage ?? model.Description ?? def.Description;
            }

            return def.Description;
        }
    }
}
