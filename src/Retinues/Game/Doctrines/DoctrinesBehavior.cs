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
        public override bool IsActive => Settings.EnableDoctrines;

        private const int DoctrineUnlockTarget = DoctrineDefinition.UnlockProgressTarget;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Persistence                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const string SyncKeyDoctrineIds = "ret_doctrine_ids";
        private const string SyncKeyDoctrineProgress = "ret_doctrine_progress";
        private const string SyncKeyDoctrineAcquired = "ret_doctrine_acquired";

        private const string SyncKeyFeatIds = "ret_feat_ids";
        private const string SyncKeyFeatProgress = "ret_feat_progress";
        private const string SyncKeyFeatConsumed = "ret_feat_consumed";
        private const string SyncKeyFeatTimesCompleted = "ret_feat_times_completed";

        private readonly Dictionary<string, int> _doctrineProgress = new(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> _doctrineAcquired = new(StringComparer.Ordinal);

        private readonly Dictionary<string, int> _featProgress = new(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> _featConsumed = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _featTimesCompleted = new(StringComparer.Ordinal);

        public override void SyncData(IDataStore dataStore)
        {
            DoctrinesCatalog.EnsureBuilt();

            List<string> dids = null;
            List<int> dprog = null;
            List<bool> dacq = null;

            List<string> fids = null;
            List<int> fprog = null;
            List<bool> fcons = null;
            List<int> ftimes = null;

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
                fcons = new List<bool>(_featProgress.Count);
                ftimes = new List<int>(_featProgress.Count);

                foreach (var kvp in _featProgress)
                {
                    var id = kvp.Key;
                    if (string.IsNullOrEmpty(id))
                        continue;

                    fids.Add(id);
                    fprog.Add(kvp.Value);
                    fcons.Add(_featConsumed.TryGetValue(id, out var c) && c);
                    ftimes.Add(_featTimesCompleted.TryGetValue(id, out var t) ? Math.Max(0, t) : 0);
                }
            }

            dataStore.SyncData(SyncKeyDoctrineIds, ref dids);
            dataStore.SyncData(SyncKeyDoctrineProgress, ref dprog);
            dataStore.SyncData(SyncKeyDoctrineAcquired, ref dacq);

            dataStore.SyncData(SyncKeyFeatIds, ref fids);
            dataStore.SyncData(SyncKeyFeatProgress, ref fprog);
            dataStore.SyncData(SyncKeyFeatConsumed, ref fcons);
            dataStore.SyncData(SyncKeyFeatTimesCompleted, ref ftimes);

            if (!dataStore.IsLoading)
                return;

            _doctrineProgress.Clear();
            _doctrineAcquired.Clear();
            _featProgress.Clear();
            _featConsumed.Clear();
            _featTimesCompleted.Clear();

            if (dids != null && dprog != null && dacq != null)
            {
                var n = Math.Min(dids.Count, Math.Min(dprog.Count, dacq.Count));
                for (var i = 0; i < n; i++)
                {
                    var id = dids[i];
                    if (string.IsNullOrEmpty(id))
                        continue;

                    _doctrineProgress[id] = ClampDoctrineProgress(dprog[i]);
                    _doctrineAcquired[id] = dacq[i];
                }
            }

            if (fids != null && fprog != null && fcons != null && ftimes != null)
            {
                var n = Math.Min(
                    fids.Count,
                    Math.Min(fprog.Count, Math.Min(fcons.Count, ftimes.Count))
                );
                for (var i = 0; i < n; i++)
                {
                    var id = fids[i];
                    if (string.IsNullOrEmpty(id))
                        continue;

                    _featProgress[id] = Math.Max(0, fprog[i]);
                    _featConsumed[id] = fcons[i];
                    _featTimesCompleted[id] = Math.Max(0, ftimes[i]);
                }
            }

            SeedMissing();
        }

        private static int ClampDoctrineProgress(int value)
        {
            if (value < 0)
                return 0;
            if (value > DoctrineUnlockTarget)
                return DoctrineUnlockTarget;
            return value;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Notifications                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private struct PendingFeatNotification
        {
            public string FeatId;
        }

        private static readonly List<PendingFeatNotification> PendingFeatNotifs = new(16);

        [StaticClearAction]
        public static void ClearStatic()
        {
            PendingFeatNotifs.Clear();
        }

        protected override void OnTick()
        {
            if (PendingFeatNotifs.Count == 0)
                return;

            FlushFeatNotifications();
        }

        private static void QueueFeatCompleted(string featId)
        {
            if (string.IsNullOrEmpty(featId))
                return;

            PendingFeatNotifs.Add(new PendingFeatNotification { FeatId = featId });

            if (PendingFeatNotifs.Count > 64)
                PendingFeatNotifs.RemoveAt(0);
        }

        private static void FlushFeatNotifications()
        {
            if (PendingFeatNotifs.Count == 0)
                return;

            DoctrinesCatalog.EnsureBuilt();

            // Dedupe by feat id, preserve order.
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var ids = new List<string>(PendingFeatNotifs.Count);

            for (var i = 0; i < PendingFeatNotifs.Count; i++)
            {
                var id = PendingFeatNotifs[i].FeatId;
                if (string.IsNullOrEmpty(id) || !seen.Add(id))
                    continue;

                ids.Add(id);
            }

            PendingFeatNotifs.Clear();

            if (ids.Count == 0)
                return;

            for (var i = 0; i < ids.Count; i++)
            {
                var id = ids[i];

                if (!DoctrinesCatalog.TryGetFeat(id, out var feat) || feat == null)
                    continue;

                var title = L.T("feat_complete_title", "Feat Completed");
                var body = L.T("feat_notification_body", "{NAME}:\n\n{DESC}")
                    .SetTextVariable("NAME", feat.Name)
                    .SetTextVariable("DESC", feat.Description);

                ShowFeatNotification(title, body);
            }
        }

        private static void ShowFeatNotification(TextObject title, TextObject description)
        {
            switch (Settings.FeatCompleteNotification.Value)
            {
                case Settings.NotificationStyle.Popup:
                    Inquiries.Popup(title, description, delayUntilOnWorldMap: true);
                    break;

                case Settings.NotificationStyle.Message:
                    // Body only: description, no extra text.
                    Notifications.Message(description?.ToString() ?? string.Empty);
                    break;

                default:
                    Inquiries.Popup(title, description, delayUntilOnWorldMap: true);
                    break;
            }
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
            if (p >= DoctrineUnlockTarget)
                return DoctrineState.Unlocked;

            return DoctrineState.InProgress;
        }

        public int GetDoctrineProgress(string doctrineId)
        {
            if (string.IsNullOrEmpty(doctrineId))
                return 0;

            // If feat requirements are disabled, treat all doctrines as "fully progressed"
            // (do NOT persist this; user may re-enable the setting later).
            if (!Settings.EnableFeatRequirements)
            {
                // Only for real doctrines; unknown ids stay 0.
                if (DoctrinesCatalog.TryGetDoctrine(doctrineId, out var def) && def != null)
                    return DoctrineUnlockTarget;

                return 0;
            }

            return _doctrineProgress.TryGetValue(doctrineId, out var p) ? p : 0;
        }

        public bool IsDoctrineAcquired(string doctrineId)
        {
            return _doctrineAcquired.TryGetValue(doctrineId ?? string.Empty, out var a) && a;
        }

        public int GetFeatProgress(string featId)
        {
            return _featProgress.TryGetValue(featId ?? string.Empty, out var p) ? p : 0;
        }

        // "Completed" means one-time feat consumed (non-repeatable completed once).
        public bool IsFeatCompleted(string featId)
        {
            return _featConsumed.TryGetValue(featId ?? string.Empty, out var c) && c;
        }

        public bool CanProgressFeat(string featId)
        {
            var links = DoctrinesCatalog.GetDoctrineLinksForFeat(featId);
            if (links == null || links.Count == 0)
                return false;

            for (var i = 0; i < links.Count; i++)
            {
                var (doctrineId, _) = links[i];
                if (string.IsNullOrEmpty(doctrineId))
                    continue;

                if (GetDoctrineState(doctrineId) == DoctrineState.InProgress)
                    return true;
            }

            return false;
        }

        public int GetFeatTimesCompleted(string featId)
        {
            return _featTimesCompleted.TryGetValue(featId ?? string.Empty, out var t)
                ? Math.Max(0, t)
                : 0;
        }

        public bool TryAddFeatProgress(string featId, int amount)
        {
            if (!IsEnabled || !Settings.EnableFeatRequirements)
                return false;

            if (!DoctrinesCatalog.TryGetFeat(featId, out var feat) || feat == null)
                return false;

            if (amount <= 0)
                return false;

            if (!CanProgressFeat(feat.Id))
                return false;

            if (!feat.Repeatable && IsFeatCompleted(feat.Id))
                return false;

            var p = GetFeatProgress(feat.Id);
            p += amount;

            if (p >= feat.Target)
            {
                CompleteFeatInternal(feat.Id);
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
            _featConsumed[feat.Id] = false;
            _featTimesCompleted[feat.Id] = 0;
            return true;
        }

        public bool TrySetFeatProgress(string featId, int amount)
        {
            if (!IsEnabled || !Settings.EnableFeatRequirements)
                return false;

            if (!DoctrinesCatalog.TryGetFeat(featId, out var feat) || feat == null)
                return false;

            if (amount < 0)
                return false;

            if (!CanProgressFeat(feat.Id))
                return false;

            if (!feat.Repeatable && IsFeatCompleted(feat.Id))
                return false;

            if (amount >= feat.Target)
            {
                CompleteFeatInternal(feat.Id);
                return true;
            }

            _featProgress[feat.Id] = amount;
            return true;
        }

        public bool TryCompleteFeat(string featId)
        {
            if (!IsEnabled || !Settings.EnableFeatRequirements)
                return false;

            if (!DoctrinesCatalog.TryGetFeat(featId, out var feat) || feat == null)
                return false;

            if (!CanProgressFeat(feat.Id))
                return false;

            if (!feat.Repeatable && IsFeatCompleted(feat.Id))
                return false;

            CompleteFeatInternal(feat.Id);
            return true;
        }

        public bool TryAcquireDoctrine(string doctrineId, out TextObject error)
        {
            error = null;

            if (!IsEnabled)
            {
                error = L.T("doctrine_disabled", "Doctrines are disabled.");
                return false;
            }

            if (!DoctrinesCatalog.TryGetDoctrine(doctrineId, out var def) || def == null)
            {
                error = L.T("doctrine_not_found", "Doctrine not found.");
                return false;
            }

            var state = GetDoctrineState(def.Id);
            if (state != DoctrineState.Unlocked)
            {
                error = L.T("doctrine_not_ready", "Doctrine is not ready to be acquired.");
                return false;
            }

            if (Player.Gold < def.GoldCost)
            {
                error = L.TV(
                    "doctrine_not_enough_gold",
                    "Not enough gold (needs {COST}).",
                    ("COST", def.GoldCost)
                );
                return false;
            }

            if (Player.Influence < def.InfluenceCost)
            {
                error = L.TV(
                    "doctrine_not_enough_influence",
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
                error = L.T("doctrine_failed", "Failed to acquire doctrine.");
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
                if (!_featConsumed.ContainsKey(id))
                    _featConsumed[id] = false;
                if (!_featTimesCompleted.ContainsKey(id))
                    _featTimesCompleted[id] = 0;
            }
        }

        private void CompleteFeatInternal(string featId)
        {
            if (!DoctrinesCatalog.TryGetFeat(featId, out var feat) || feat == null)
                return;

            // Feat completes: apply worth, then reset progress.
            _featProgress[feat.Id] = 0;

            var times = GetFeatTimesCompleted(feat.Id) + 1;
            _featTimesCompleted[feat.Id] = times;

            if (!feat.Repeatable)
                _featConsumed[feat.Id] = true;

            // Only notify if this completion contributed doctrine progress.
            var applied = ApplyFeatCompletionToDoctrines(feat.Id);
            if (applied)
                QueueFeatCompleted(feat.Id);
        }

        private bool ApplyFeatCompletionToDoctrines(string featId)
        {
            var links = DoctrinesCatalog.GetDoctrineLinksForFeat(featId);
            if (links == null || links.Count == 0)
                return false;

            // Snapshot which doctrines are eligible before we change any progress.
            var eligible = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < links.Count; i++)
            {
                var (doctrineId, _) = links[i];
                if (string.IsNullOrEmpty(doctrineId))
                    continue;

                if (!DoctrinesCatalog.TryGetDoctrine(doctrineId, out var def) || def == null)
                    continue;

                if (GetDoctrineState(def.Id) == DoctrineState.InProgress)
                    eligible.Add(def.Id);
            }

            if (eligible.Count == 0)
                return false;

            var appliedAny = false;

            // Apply worth only to doctrines that were already InProgress at snapshot time.
            for (var i = 0; i < links.Count; i++)
            {
                var (doctrineId, worth) = links[i];
                if (worth <= 0 || string.IsNullOrEmpty(doctrineId))
                    continue;

                if (!eligible.Contains(doctrineId))
                    continue;

                var cur = GetDoctrineProgress(doctrineId);
                var next = ClampDoctrineProgress(cur + worth);

                if (next != cur)
                {
                    _doctrineProgress[doctrineId] = next;
                    appliedAny = true;
                }
            }

            return appliedAny;
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

            return IsDoctrineAcquired(prev.Id);
        }
    }
}
