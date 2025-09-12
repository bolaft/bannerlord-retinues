// Retinues.Core.Game.Features.Doctrines/DoctrineServiceBehavior.cs
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using Retinues.Core.Utils; // your logger, optional

namespace Retinues.Core.Game.Features.Doctrines
{
    /// <summary>
    /// Persistent doctrine/feat state + query/mutation API.
    /// Call via Doctrines.* static facade or fetch behavior from Campaign.
    /// </summary>
    public sealed class DoctrineServiceBehavior : CampaignBehaviorBase
    {
        // -------- State (persisted) --------
        private HashSet<string> _unlocked = [];                // doctrineId -> unlocked
        private Dictionary<string, int> _featProgress = [];    // featId -> current value

        // -------- Definitions (rebuilt on load) --------
        private Dictionary<string, DoctrineDef> _defsById = [];
        private Dictionary<string, string> _featToDoctrine = []; // featId -> doctrineId

        // -------- Events (optional) --------
        public event Action<string> DoctrineUnlocked; // doctrineId
        public event Action<string> FeatCompleted;    // featId

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
            // Persist only the variable state; definitions are rebuilt
            var unlockedList = _unlocked?.ToList() ?? [];
            dataStore.SyncData("CCT_Doctrines_Unlocked", ref unlockedList);
            _unlocked = unlockedList != null ? unlockedList.ToHashSet() : [];

            dataStore.SyncData("CCT_Doctrines_FeatProgress", ref _featProgress);
            _featProgress ??= [];
        }

        // --------------------------------------------------------------------
        // Public API (call via Doctrines.* facade)
        // --------------------------------------------------------------------
        public IEnumerable<DoctrineDef> AllDoctrines() => _defsById.Values.OrderBy(d => d.Column).ThenBy(d => d.Row);

        public DoctrineDef GetDoctrine(string id) => _defsById.TryGetValue(id, out var d) ? d : null;

        public DoctrineStatus GetDoctrineStatus(string id)
        {
            Log.Debug($"Getting status for doctrine {id}");
            var def = GetDoctrine(id);
            if (def == null) return DoctrineStatus.Locked;

            if (_unlocked.Contains(id))
                return DoctrineStatus.Unlocked;

            // prerequisite gate
            if (!string.IsNullOrEmpty(def.PrerequisiteId) && !_unlocked.Contains(def.PrerequisiteId))
                return DoctrineStatus.Locked;

            // feats gate
            var feats = def.Feats;
            if (feats == null || feats.Count == 0)
                return DoctrineStatus.InProgress; // no feats => can be acquired

            // if any feat incomplete -> Unlockable (i.e., still working on feats)
            if (feats.Any(f => !IsFeatComplete(f.Id)))
                return DoctrineStatus.Unlockable;

            // all feats complete -> ready to acquire
            return DoctrineStatus.InProgress;
        }

        public bool IsDoctrineUnlocked(string id) => _unlocked.Contains(id);

        public bool TryAcquireDoctrine(string id, out string failReason)
        {
            Log.Debug($"Trying to acquire doctrine {id}");
            failReason = null;
            var def = GetDoctrine(id);
            if (def == null) { failReason = "Unknown doctrine."; return false; }

            var status = GetDoctrineStatus(id);
            if (status != DoctrineStatus.InProgress)
            {
                failReason = status == DoctrineStatus.Locked ? "Prerequisite not met." : "Feats incomplete.";
                return false;
            }

            // Check costs
            var gold = Hero.MainHero?.Gold ?? 0;
            var influence = Clan.PlayerClan?.Influence ?? 0f;

            if (gold < def.GoldCost)
            {
                failReason = "Not enough gold.";
                return false;
            }
            if (influence < def.InfluenceCost)
            {
                failReason = "Not enough influence.";
                return false;
            }

            // Pay & unlock
            Hero.MainHero?.ChangeHeroGold(-def.GoldCost);
            if (Clan.PlayerClan != null)
                Clan.PlayerClan.Influence = Math.Max(0f, Clan.PlayerClan.Influence - def.InfluenceCost);

            _unlocked.Add(id);
            Log.Debug($"[Doctrines] Unlocked: {def.Name} ({id})");
            DoctrineUnlocked?.Invoke(id);
            return true;
        }

        // -------- Feats --------

        public int GetFeatTarget(string featId)
        {
            var dId = _featToDoctrine.TryGetValue(featId, out var did) ? did : null;
            if (dId == null) return 0;
            var feat = GetDoctrine(dId)?.Feats?.FirstOrDefault(f => f.Id == featId);
            return feat?.Target ?? 0;
        }

        public int GetFeatProgress(string featId)
        {
            return _featProgress.TryGetValue(featId, out var v) ? v : 0;
        }

        public bool IsFeatComplete(string featId)
        {
            int target = GetFeatTarget(featId);
            if (target <= 0) return true; // feats with no target are trivially complete
            return GetFeatProgress(featId) >= target;
        }

        /// <summary>Advance feat progress by amount (>=1). Returns new value (clamped).</summary>
        public int AdvanceFeat(string featId, int amount = 1)
        {
            if (string.IsNullOrEmpty(featId) || amount <= 0) return GetFeatProgress(featId);

            if (!_featToDoctrine.ContainsKey(featId))
                return GetFeatProgress(featId); // unknown feat id, ignore safely

            int target = GetFeatTarget(featId);
            int cur = GetFeatProgress(featId);
            int next = Math.Min(target <= 0 ? cur + amount : cur + amount, target > 0 ? target : int.MaxValue);

            _featProgress[featId] = next;

            if (target > 0 && cur < target && next >= target)
            {
                Log.Debug($"[Doctrines] Feat complete: {featId}");
                FeatCompleted?.Invoke(featId);
            }
            return next;
        }

        // --------------------------------------------------------------------
        // Definitions — build once per session (mirror your DoctrineTree)
        // --------------------------------------------------------------------
        private void BuildCatalogIfNeeded()
        {
            Log.Debug("Building doctrine catalog");
            if (_defsById.Count > 0) return;

            var defs = new List<DoctrineDef>();
            string[,] ids = {
                { "lions_share", "battlefield_tithes", "pragmatic_scavengers", "ancestral_heritage" },
                { "cultural_pride", "clanic_traditions", "royal_patronage", "ironclad" },
                { "iron_discipline", "steadfast_soldiers", "masters_at_arms", "adaptive_training" },
                { "indomitable", "bound_by_honor", "vanguard", "immortals" }
            };
            string[,] names = {
                { "Lion's Share", "Battlefield Tithes", "Pragmatic Scavengers", "Ancestral Heritage" },
                { "Cultural Pride", "Clanic Traditions", "Royal Patronage", "Ironclad" },
                { "Iron Discipline", "Steadfast Soldiers", "Masters-At-Arms", "Adaptive Training" },
                { "Indomitable", "Bound by Honor", "Vanguard", "Immortals" }
            };
            string[,] desc = {
                { "Hero kills count twice for unlocks.", "Unlock items from allied kills.", "Unlock items from allied casualties.", "Unlocks all items of clan culture." },
                { "10% rebate on items of the troop's culture.", "10% rebate on items of the clan's culture.", "10% rebate on items of the kingdom's culture.", "No tier restriction for arms and armor." },
                { "+5% skill cap.", "+5% skill points.", "+1 upgrade branch for elite troops.", "Experience refunds for retraining." },
                { "+25% retinue health.", "+20% retinue morale.", "+15% retinue cap.", "+25% retinue survival chance." }
            };

            for (int c = 0; c < 4; c++)
            {
                for (int r = 0; r < 4; r++)
                {
                    var id = ids[c, r];
                    var d = new DoctrineDef
                    {
                        Id = id,
                        Name = names[c, r],
                        Description = desc[c, r],
                        Column = c,
                        Row = r,
                        PrerequisiteId = r > 0 ? ids[c, r - 1] : null,
                        GoldCost = r switch { 0 => 1000, 1 => 5000, 2 => 25000, 3 => 100000, _ => 0 },
                        InfluenceCost = 0,
                        // InfluenceCost = r switch { 0 => 50, 1 => 100, 2 => 200, 3 => 500, _ => 0 },
                        Feats = [] // define feats here or add later via RegisterFeat
                    };
                    defs.Add(d);
                }
            }

            _defsById = defs.ToDictionary(d => d.Id, d => d);

            // If you already know some feats, register them here. You can expand later from behaviors.
            // Example:
            // RegisterFeat("feat_defeat_king_party", "Defeat a ruling monarch's war party.", 1, "lions_share");

            RebuildFeatIndex();
        }

        public void RegisterFeat(string featId, string description, int target, string doctrineId)
        {
            Log.Debug($"Registering feat {featId} for doctrine {doctrineId}");
            if (!_defsById.TryGetValue(doctrineId, out var d)) return;
            if (d.Feats.Any(f => f.Id == featId)) return;
            d.Feats.Add(new FeatDef { Id = featId, Description = description, Target = target });
            _featToDoctrine[featId] = doctrineId;
        }

        private void RebuildFeatIndex()
        {
            Log.Debug("Rebuilding feat index");
            _featToDoctrine.Clear();
            foreach (var d in _defsById.Values)
                foreach (var f in d.Feats)
                    _featToDoctrine[f.Id] = d.Id;
        }
    }
}
