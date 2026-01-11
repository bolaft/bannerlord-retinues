using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Framework.Behaviors;
using Retinues.Framework.Runtime;
using Retinues.UI.Services;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;

namespace Retinues.Game.Doctrines
{
    /// <summary>
    /// Stores doctrine and feat progress, persists it, and applies feat completion to doctrine progress.
    /// </summary>
    [SafeClass]
    public sealed class DoctrinesBehavior : BaseCampaignBehavior<DoctrinesBehavior>
    {
        public override bool IsEnabled => Settings.EnableDoctrines;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Persistence                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const string SyncKeyDoctrineIds = "ret_doctrine_ids";
        private const string SyncKeyDoctrineProgress = "ret_doctrine_progress";
        private const string SyncKeyDoctrineAcquired = "ret_doctrine_acquired";

        private const string SyncKeyFeatIds = "ret_feat_ids";
        private const string SyncKeyFeatProgress = "ret_feat_progress";
        private const string SyncKeyFeatCompleted = "ret_feat_completed";

        private readonly Dictionary<string, int> _doctrineProgress = new(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> _doctrineAcquired = new(StringComparer.Ordinal);

        private readonly Dictionary<string, int> _featProgress = new(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> _featCompleted = new(StringComparer.Ordinal);

        public override void SyncData(IDataStore dataStore)
        {
            DoctrinesCatalog.EnsureBuilt();

            List<string> dids = null;
            List<int> dprog = null;
            List<bool> dacq = null;

            List<string> fids = null;
            List<int> fprog = null;
            List<bool> fdone = null;

            if (dataStore.IsSaving)
            {
                dids = new List<string>(_doctrineProgress.Count);
                dprog = new List<int>(_doctrineProgress.Count);
                dacq = new List<bool>(_doctrineProgress.Count);

                foreach (var kvp in _doctrineProgress)
                {
                    var id = kvp.Key;
                    if (string.IsNullOrEmpty(id))
                        continue;

                    dids.Add(id);
                    dprog.Add(kvp.Value);
                    dacq.Add(_doctrineAcquired.TryGetValue(id, out var a) && a);
                }

                fids = new List<string>(_featProgress.Count);
                fprog = new List<int>(_featProgress.Count);
                fdone = new List<bool>(_featProgress.Count);

                foreach (var kvp in _featProgress)
                {
                    var id = kvp.Key;
                    if (string.IsNullOrEmpty(id))
                        continue;

                    fids.Add(id);
                    fprog.Add(kvp.Value);
                    fdone.Add(_featCompleted.TryGetValue(id, out var c) && c);
                }
            }

            dataStore.SyncData(SyncKeyDoctrineIds, ref dids);
            dataStore.SyncData(SyncKeyDoctrineProgress, ref dprog);
            dataStore.SyncData(SyncKeyDoctrineAcquired, ref dacq);

            dataStore.SyncData(SyncKeyFeatIds, ref fids);
            dataStore.SyncData(SyncKeyFeatProgress, ref fprog);
            dataStore.SyncData(SyncKeyFeatCompleted, ref fdone);

            if (!dataStore.IsLoading)
                return;

            _doctrineProgress.Clear();
            _doctrineAcquired.Clear();
            _featProgress.Clear();
            _featCompleted.Clear();

            if (dids != null && dprog != null && dacq != null)
            {
                var n = Math.Min(dids.Count, Math.Min(dprog.Count, dacq.Count));
                for (var i = 0; i < n; i++)
                {
                    var id = dids[i];
                    if (string.IsNullOrEmpty(id))
                        continue;

                    _doctrineProgress[id] = Math.Max(0, dprog[i]);
                    _doctrineAcquired[id] = dacq[i];
                }
            }

            if (fids != null && fprog != null && fdone != null)
            {
                var n = Math.Min(fids.Count, Math.Min(fprog.Count, fdone.Count));
                for (var i = 0; i < n; i++)
                {
                    var id = fids[i];
                    if (string.IsNullOrEmpty(id))
                        continue;

                    _featProgress[id] = Math.Max(0, fprog[i]);
                    _featCompleted[id] = fdone[i];
                }
            }

            // Ensure new defs have entries.
            SeedMissing();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Notifications                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private struct PendingFeatNotification
        {
            public string FeatId;
            public TextObject Source;
        }

        private static readonly List<PendingFeatNotification> PendingFeatNotifs = new(16);

        [StaticClearAction]
        public static void ClearStatic()
        {
            PendingFeatNotifs.Clear();
        }

        protected override void RegisterCustomEvents()
        {
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
        }

        private void OnTick(float dt)
        {
            if (PendingFeatNotifs.Count == 0)
                return;

            FlushFeatNotifications();
        }

        private static void QueueFeatCompleted(string featId, TextObject source)
        {
            if (string.IsNullOrEmpty(featId))
                return;

            PendingFeatNotifs.Add(new PendingFeatNotification { FeatId = featId, Source = source });

            if (PendingFeatNotifs.Count > 64)
                PendingFeatNotifs.RemoveAt(0);
        }

        private static void FlushFeatNotifications()
        {
            if (PendingFeatNotifs.Count == 0)
                return;

            DoctrinesCatalog.EnsureBuilt();

            // Dedupe by feat id.
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var names = new List<string>(PendingFeatNotifs.Count);

            TextObject source = null;

            for (var i = 0; i < PendingFeatNotifs.Count; i++)
            {
                var id = PendingFeatNotifs[i].FeatId;
                if (string.IsNullOrEmpty(id) || !seen.Add(id))
                    continue;

                if (source == null)
                    source = PendingFeatNotifs[i].Source;

                if (DoctrinesCatalog.TryGetFeat(id, out var feat) && feat?.Name != null)
                    names.Add(feat.Name.ToString());
                else
                    names.Add(id);
            }

            PendingFeatNotifs.Clear();

            if (names.Count == 0)
                return;

            var title = L.T("feat_complete_title", "Feats completed");

            var list = JoinWithAnd(names, max: 5, out var plural);
            var desc = plural
                ? L.T("feat_complete_desc_many", "{FEATS} were completed.")
                : L.T("feat_complete_desc_one", "{FEATS} was completed.");

            desc.SetTextVariable("FEATS", list);

            if (source != null)
            {
                // Optional one-line source prefix if provided.
                var s = L.T("feat_complete_source", "{SOURCE}\n\n{DESC}");
                s.SetTextVariable("SOURCE", source);
                s.SetTextVariable("DESC", desc);
                desc = s;
            }

            ShowFeatNotification(title, desc);
        }

        private static void ShowFeatNotification(TextObject title, TextObject description)
        {
            switch (Settings.FeatCompleteNotification.Value)
            {
                case Settings.NotificationStyle.Popup:
                    Inquiries.Popup(title, description, delayUntilOnWorldMap: true);
                    break;

                case Settings.NotificationStyle.Message:
                    Notifications.Message($"{title}\n{description}");
                    break;

                default:
                    Inquiries.Popup(title, description, delayUntilOnWorldMap: true);
                    break;
            }
        }

        private static string JoinWithAnd(IReadOnlyList<string> items, int max, out bool isPlural)
        {
            if (items == null || items.Count == 0)
            {
                isPlural = true;
                return string.Empty;
            }

            var filtered = new List<string>(items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                var s = items[i];
                if (!string.IsNullOrEmpty(s))
                    filtered.Add(s);
            }

            if (filtered.Count == 0)
            {
                isPlural = true;
                return string.Empty;
            }

            isPlural = filtered.Count != 1;

            var take = Math.Min(max, filtered.Count);
            var shown = filtered.GetRange(0, take);
            var more = filtered.Count - take;

            var andWord = L.S("feat_and", "and");

            if (more > 0)
            {
                var prefix = string.Join(", ", shown);
                var moreText = L.T("feat_more", "{COUNT} more")
                    .SetTextVariable("COUNT", more)
                    .ToString();

                return $"{prefix} {andWord} {moreText}";
            }

            if (shown.Count == 1)
                return shown[0];

            if (shown.Count == 2)
                return $"{shown[0]} {andWord} {shown[1]}";

            return $"{string.Join(", ", shown.GetRange(0, shown.Count - 1))} {andWord} {shown[shown.Count - 1]}";
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Public                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public DoctrineState GetDoctrineState(string doctrineId)
        {
            if (!DoctrinesCatalog.TryGetDoctrine(doctrineId, out var def) || def == null)
                return DoctrineState.Locked;

            if (_doctrineAcquired.TryGetValue(def.Id, out var acquired) && acquired)
                return DoctrineState.Acquired;

            if (!IsDoctrineAvailable(def))
                return DoctrineState.Locked;

            var p = GetDoctrineProgress(def.Id);
            if (p >= def.ProgressTarget)
                return DoctrineState.Unlocked;

            return DoctrineState.InProgress;
        }

        public int GetDoctrineProgress(string doctrineId)
        {
            return _doctrineProgress.TryGetValue(doctrineId ?? string.Empty, out var p) ? p : 0;
        }

        public bool IsDoctrineAcquired(string doctrineId)
        {
            return _doctrineAcquired.TryGetValue(doctrineId ?? string.Empty, out var a) && a;
        }

        public int GetFeatProgress(string featId)
        {
            return _featProgress.TryGetValue(featId ?? string.Empty, out var p) ? p : 0;
        }

        public bool IsFeatCompleted(string featId)
        {
            return _featCompleted.TryGetValue(featId ?? string.Empty, out var c) && c;
        }

        public bool TryAddFeatProgress(string featId, int amount, TextObject source = null)
        {
            if (!IsEnabled || !Settings.EnableFeatRequirements)
                return false;

            if (!DoctrinesCatalog.TryGetFeat(featId, out var feat) || feat == null)
                return false;

            if (IsFeatCompleted(feat.Id))
                return false;

            if (amount <= 0)
                return false;

            var p = GetFeatProgress(feat.Id);
            p += amount;

            if (p >= feat.Target)
            {
                CompleteFeatInternal(feat.Id, source);
                return true;
            }

            _featProgress[feat.Id] = p;
            return true;
        }

        public bool TryResetFeat(string featId)
        {
            if (!IsEnabled || !Settings.EnableFeatRequirements)
                return false;

            if (!DoctrinesCatalog.TryGetFeat(featId, out var feat) || feat == null)
                return false;

            _featProgress[feat.Id] = 0;
            _featCompleted[feat.Id] = false;
            return true;
        }

        public bool TryCompleteFeat(string featId, TextObject source = null)
        {
            if (!IsEnabled || !Settings.EnableFeatRequirements)
                return false;

            if (!DoctrinesCatalog.TryGetFeat(featId, out var feat) || feat == null)
                return false;

            if (IsFeatCompleted(feat.Id))
                return false;

            CompleteFeatInternal(feat.Id, source);
            return true;
        }

        public bool TryAcquireDoctrine(string doctrineId, out TextObject error)
        {
            error = null;

            if (!IsEnabled)
            {
                error = L.T("doctrine_err_disabled", "Doctrines are disabled.");
                return false;
            }

            if (!DoctrinesCatalog.TryGetDoctrine(doctrineId, out var def) || def == null)
            {
                error = L.T("doctrine_err_not_found", "Doctrine not found.");
                return false;
            }

            var state = GetDoctrineState(def.Id);
            if (state != DoctrineState.Unlocked)
            {
                error = L.T("doctrine_err_not_ready", "Doctrine is not ready to be acquired.");
                return false;
            }

            if (Settings.EnableFeatRequirements && !AreRequiredFeatsMet(def))
            {
                error = L.T(
                    "doctrine_err_missing_feats",
                    "Required feats have not been completed."
                );
                return false;
            }

            if (Player.Gold < def.GoldCost)
            {
                error = L.TV(
                    "doctrine_err_not_enough_gold",
                    "Not enough gold (needs {COST}).",
                    ("COST", def.GoldCost)
                );
                return false;
            }

            if (Player.Influence < def.InfluenceCost)
            {
                error = L.TV(
                    "doctrine_err_not_enough_influence",
                    "Not enough influence (needs {COST}).",
                    ("COST", def.InfluenceCost)
                );
                return false;
            }

            try
            {
                Player.ChangeGold(-def.GoldCost);
                ChangeClanInfluenceAction.Apply(Clan.PlayerClan, -def.InfluenceCost);

                _doctrineAcquired[def.Id] = true;
                return true;
            }
            catch (Exception e)
            {
                Log.Exception(e, "Failed to acquire doctrine.");
                error = L.T("doctrine_err_failed", "Failed to acquire doctrine.");
                return false;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Internal                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override void OnSessionLaunched(CampaignGameStarter starter)
        {
            DoctrinesCatalog.EnsureBuilt();
            SeedMissing();
        }

        private void SeedMissing()
        {
            foreach (var kvp in DoctrinesCatalog.Doctrines)
            {
                var id = kvp.Key;
                if (!_doctrineProgress.ContainsKey(id))
                    _doctrineProgress[id] = 0;
                if (!_doctrineAcquired.ContainsKey(id))
                    _doctrineAcquired[id] = false;
            }

            foreach (var kvp in DoctrinesCatalog.Feats)
            {
                var id = kvp.Key;
                if (!_featProgress.ContainsKey(id))
                    _featProgress[id] = 0;
                if (!_featCompleted.ContainsKey(id))
                    _featCompleted[id] = false;
            }
        }

        private void CompleteFeatInternal(string featId, TextObject source)
        {
            if (!DoctrinesCatalog.TryGetFeat(featId, out var feat) || feat == null)
                return;

            _featProgress[feat.Id] = feat.Target;
            _featCompleted[feat.Id] = true;

            ApplyFeatCompletionToDoctrines(feat.Id);
            QueueFeatCompleted(feat.Id, source);
        }

        private void ApplyFeatCompletionToDoctrines(string featId)
        {
            var links = DoctrinesCatalog.GetDoctrineLinksForFeat(featId);
            if (links == null || links.Count == 0)
                return;

            for (var i = 0; i < links.Count; i++)
            {
                var (doctrineId, progress, _) = links[i];
                if (progress <= 0)
                    continue;

                if (!DoctrinesCatalog.TryGetDoctrine(doctrineId, out var def) || def == null)
                    continue;

                if (GetDoctrineState(def.Id) != DoctrineState.InProgress)
                    continue;

                var cur = GetDoctrineProgress(def.Id);
                var next = cur + progress;
                if (next > def.ProgressTarget)
                    next = def.ProgressTarget;

                _doctrineProgress[def.Id] = next;
            }
        }

        private bool IsDoctrineAvailable(DoctrineDefinition def)
        {
            if (def == null)
                return false;

            if (!DoctrinesCatalog.TryGetCategory(def.CategoryId, out var cat) || cat == null)
                return false;

            // First doctrine is always available.
            if (def.IndexInCategory <= 0)
                return true;

            var ids = cat.DoctrineIds;
            if (ids == null || ids.Count <= def.IndexInCategory - 1)
                return true;

            var prevId = ids[def.IndexInCategory - 1];
            if (string.IsNullOrEmpty(prevId))
                return true;

            if (!DoctrinesCatalog.TryGetDoctrine(prevId, out var prev) || prev == null)
                return true;

            var prevProgress = GetDoctrineProgress(prev.Id);
            return prevProgress >= prev.ProgressTarget;
        }

        private bool AreRequiredFeatsMet(DoctrineDefinition def)
        {
            if (def == null || def.Feats == null || def.Feats.Count == 0)
                return true;

            for (var i = 0; i < def.Feats.Count; i++)
            {
                var link = def.Feats[i];
                if (!link.Required)
                    continue;

                if (!IsFeatCompleted(link.FeatId))
                    return false;
            }

            return true;
        }
    }
}
