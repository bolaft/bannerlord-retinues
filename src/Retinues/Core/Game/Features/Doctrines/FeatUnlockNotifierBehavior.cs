using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Retinues.Core.Utils;

namespace Retinues.Core.Game.Features.Doctrines
{
    /// Shows a popup when a feat is completed, as soon as the player is on the world map.
    public sealed class FeatUnlockNotifierBehavior : CampaignBehaviorBase
    {
        private readonly Queue<string> _pendingFeatKeys = new(); // featKey queue (Type.FullName)
        private bool _inMission;

        public override void RegisterEvents()
        {
            // Track mission enter/leave to know when we're back on the map
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, _ => _inMission = true);
            CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, _ => { _inMission = false; TryFlush(); }); // if available in your build
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);

            // Listen to feat completions
            DoctrineAPI.AddFeatCompletedListener(OnFeatCompleted);

            // Also try to flush on session launch (e.g., if something completed during load)
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, _ => TryFlush());
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Popups are ephemeral; do not persist the queue.
        }

        private void OnFeatCompleted(string featKey)
        {
            if (string.IsNullOrEmpty(featKey)) return;
            _pendingFeatKeys.Enqueue(featKey);
            TryFlush(); // if we happen to be on map already, show immediately
        }

        private void OnHourlyTick()
        {
            // Cheap, reliable heartbeat to show as soon as we're on the map.
            TryFlush();
        }

        /// Try to display queued popups if we're on the world map and no inquiry is active.
        public void TryFlush()
        {
            try
            {
                if (_inMission) return;
                if (Mission.Current != null) return; // safest map check across builds
                if (_pendingFeatKeys.Count == 0) return;
                if (InformationManager.IsAnyInquiryActive()) return;

                var featKey = _pendingFeatKeys.Dequeue();
                BuildAndShow(featKey);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        private void BuildAndShow(string featKey)
        {
            // Resolve feat + doctrine for nice text
            var doctrines = DoctrineAPI.AllDoctrines();
            var (doc, feat) = FindFeat(doctrines, featKey);

            var title = L.T("feat_completed_title", "Feat Completed");
            var desc = L.T("feat_completed_description", "{DOCTRINE}\n\n{REQ}")
                .SetTextVariable("DOCTRINE", doc?.Name ?? string.Empty)
                .SetTextVariable("REQ", feat?.Description ?? string.Empty);

            InformationManager.ShowInquiry(
                new InquiryData(title.ToString(), desc.ToString(),
                    true, false,
                    affirmativeText: GameTexts.FindText("str_ok").ToString(),
                    negativeText: null,
                    // On OK: try to show next one (if any) immediately
                    affirmativeAction: () => TryFlush(),
                    negativeAction: null));
        }

        private static (DoctrineDef doc, FeatDef feat) FindFeat(IReadOnlyList<DoctrineDef> all, string featKey)
        {
            foreach (var d in all)
            {
                var f = d.Feats?.FirstOrDefault(x => x.Key == featKey);
                if (f != null) return (d, f);
            }
            return (null, null);
        }
    }
}
