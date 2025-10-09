using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Retinues.Doctrines
{
    /// <summary>
    /// Campaign behavior for showing popups when feats are completed.
    /// Queues notifications and flushes them when on the world map.
    /// </summary>
    [SafeClass]
    public sealed class FeatNotificationBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// No sync data needed for feat notification behavior.
        /// </summary>
        public override void SyncData(IDataStore dataStore) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Registers event listeners for mission state, hourly tick, feat completion, and session launch.
        /// </summary>
        public override void RegisterEvents()
        {
            // Track mission enter/leave to know when we're back on the map
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(
                this,
                _ => _inMission = true
            );
            CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(
                this,
                _ =>
                {
                    _inMission = false;
                    TryFlush();
                }
            );
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);

            // Listen to feat completions
            DoctrineAPI.AddFeatCompletedListener(OnFeatCompleted);

            // Also try to flush on session launch (e.g., if something completed during load)
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, _ => TryFlush());
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void OnFeatCompleted(string featKey)
        {
            if (string.IsNullOrEmpty(featKey))
                return;
            _pendingFeatKeys.Enqueue(featKey);
            TryFlush(); // if we happen to be on map already, show immediately
        }

        private void OnHourlyTick()
        {
            // Cheap, reliable heartbeat to show as soon as we're on the map.
            TryFlush();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Try to display queued popups if we're on the world map and no inquiry is active.
        /// </summary>
        public void TryFlush()
        {
            try
            {
                if (_inMission)
                    return;
                if (Mission.Current != null)
                    return; // safest map check across builds
                if (_pendingFeatKeys.Count == 0)
                    return;
                if (InformationManager.IsAnyInquiryActive())
                    return;

                var featKey = _pendingFeatKeys.Dequeue();
                BuildAndShow(featKey);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Queue<string> _pendingFeatKeys = new(); // featKey queue (Type.FullName)
        private bool _inMission;

        /// <summary>
        /// Builds and shows the popup for a completed feat.
        /// </summary>
        private void BuildAndShow(string featKey)
        {
            // Resolve feat + doctrine for nice text
            var doctrines = DoctrineAPI.AllDoctrines();
            var (doc, feat) = FindFeat(doctrines, featKey);

            var title = L.T("feat_completed_title", "Feat Completed");

            InformationManager.ShowInquiry(
                new InquiryData(
                    title.ToString(),
                    feat.Description.ToString(),
                    true,
                    false,
                    affirmativeText: GameTexts.FindText("str_ok").ToString(),
                    negativeText: null,
                    // On OK: try to show next one (if any) immediately
                    affirmativeAction: () => TryFlush(),
                    negativeAction: null
                )
            );
        }

        private static (DoctrineDefinition doc, FeatDefinition feat) FindFeat(
            IReadOnlyList<DoctrineDefinition> all,
            string featKey
        )
        {
            foreach (var d in all)
            {
                var f = d.Feats?.FirstOrDefault(x => x.Key == featKey);
                if (f != null)
                    return (d, f);
            }
            return (null, null);
        }
    }
}
