using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Framework.Behaviors;
using Retinues.Framework.Runtime;
using Retinues.Game.Doctrines.Definitions;
using Retinues.UI.Services;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace Retinues.Game.Doctrines
{
    /// <summary>
    /// Stores doctrine and feat progress, persists it, and applies feat completion to doctrine progress.
    /// </summary>
    public sealed class DoctrinesBehavior : BaseCampaignBehavior<DoctrinesBehavior>
    {
        public override bool IsActive => Settings.EnableDoctrines;

        private const int DoctrineUnlockTarget = Doctrine.ProgressTarget;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Persistence                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const string SyncKeyDoctrineAcquired = "ret_doctrineAcquired";
        private const string SyncKeyDoctrineProgress = "ret_doctrineProgress";
        private const string SyncKeyFeatProgress = "ret_featProgress";

        private Dictionary<string, bool> _doctrineAcquired = new(StringComparer.Ordinal);
        private Dictionary<string, int> _doctrineProgress = new(StringComparer.Ordinal);
        private Dictionary<string, int> _featProgress = new(StringComparer.Ordinal);

        /// <summary>
        /// Synchronizes doctrine and feat progress data.
        /// </summary>
        public override void SyncData(IDataStore dataStore)
        {
            DoctrinesRegistry.EnsureRegistered();

            if (dataStore.IsSaving)
            {
                _doctrineProgress.Clear();
                _doctrineAcquired.Clear();
                _featProgress.Clear();

                foreach (var doctrine in DoctrinesRegistry.GetDoctrines())
                {
                    _doctrineProgress[doctrine.Id] = doctrine.Progress;
                    _doctrineAcquired[doctrine.Id] = doctrine.IsAcquired;

                    foreach (var feat in doctrine.Feats)
                    {
                        _featProgress[feat.Id] = feat.Progress;
                    }
                }
            }

            dataStore.SyncData(SyncKeyDoctrineAcquired, ref _doctrineAcquired);
            dataStore.SyncData(SyncKeyDoctrineProgress, ref _doctrineProgress);
            dataStore.SyncData(SyncKeyFeatProgress, ref _featProgress);

            if (dataStore.IsLoading)
            {
                // Apply progress first.
                foreach (var kvp in _doctrineProgress)
                {
                    var doctrine = DoctrinesRegistry.GetDoctrine(kvp.Key);
                    if (doctrine == null)
                        continue;

                    doctrine.ForceSet(kvp.Value);
                }

                // Apply acquired state after progress to ensure consistency.
                foreach (var kvp in _doctrineAcquired)
                {
                    var doctrine = DoctrinesRegistry.GetDoctrine(kvp.Key);
                    if (doctrine == null)
                        continue;

                    doctrine.IsAcquired = kvp.Value;
                }

                // Apply feat progress last.
                foreach (var kvp in _featProgress)
                {
                    var feat = DoctrinesRegistry.GetFeat(kvp.Key);
                    if (feat == null)
                        continue;

                    feat.ForceSet(kvp.Value);
                }
            }
        }

        /// <summary>
        /// Ensure doctrines are registered on launch.
        /// </summary>
        protected override void OnSessionLaunched(CampaignGameStarter starter)
        {
            DoctrinesRegistry.EnsureRegistered();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Notifications                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly List<string> PendingFeatNotifs = new(16);

        [StaticClearAction]
        public static void ClearStatic() => PendingFeatNotifs.Clear();

        /// <summary>
        /// Processes pending feat completion notifications.
        /// </summary>
        protected override void OnTick()
        {
            if (PendingFeatNotifs.Count == 0)
                return;

            FlushFeatNotifications();
        }

        /// <summary>
        /// Queues a feat completion notification.
        /// </summary>
        private static void QueueFeatCompleted(string featId)
        {
            if (string.IsNullOrEmpty(featId))
                return;

            PendingFeatNotifs.Add(featId);

            if (PendingFeatNotifs.Count > 64)
                PendingFeatNotifs.RemoveAt(0);
        }

        /// <summary>
        /// Flushes pending feat completion notifications.
        /// </summary>
        private static void FlushFeatNotifications()
        {
            if (PendingFeatNotifs.Count == 0)
                return;

            DoctrinesRegistry.EnsureRegistered();

            // Dedupe by feat id, preserve order.
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var ids = new List<string>(PendingFeatNotifs.Count);

            for (var i = 0; i < PendingFeatNotifs.Count; i++)
            {
                var id = PendingFeatNotifs[i];
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
                var feat = Feat.Get(id);

                if (feat == null)
                    continue;

                var title = L.T("feat_complete_title", "Feat Completed");
                var body = L.T("feat_notification_body", "{NAME}:\n\n{DESC}")
                    .SetTextVariable("NAME", feat.Name)
                    .SetTextVariable("DESC", feat.Description);

                ShowFeatNotification(title, body);
            }
        }

        /// <summary>
        /// Shows a feat completion notification according to user settings.
        /// </summary>
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

        /// <summary>
        /// Called when a feat is completed.
        /// </summary>
        internal void OnFeatCompleted(Feat feat, int completions)
        {
            if (feat == null || completions <= 0)
                return;

            // Queue notification once per completion batch (or per feat id; your current dedupe is per flush).
            QueueFeatCompleted(feat.Id);
        }
    }
}
