using System.Collections.Generic;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Safety.Legacy.Behaviors
{
    [SafeClass]
    public sealed class UnlocksBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private Dictionary<string, int> _defeatsByItemId = [];

        public bool HasSyncData => _defeatsByItemId != null && _defeatsByItemId.Count > 0;

        public override void SyncData(IDataStore ds)
        {
            if (ds.IsSaving)
                _defeatsByItemId = null; // Clear reference before saving

            ds.SyncData(nameof(_defeatsByItemId), ref _defeatsByItemId);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(
                this,
                OnAfterSessionLaunched
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void OnAfterSessionLaunched(CampaignGameStarter starter)
        {
            if (_defeatsByItemId == null)
                return;

            if (_defeatsByItemId.Count > 0)
            {
                foreach (var kvp in _defeatsByItemId)
                {
                    var progress = Features
                        .Unlocks
                        .Behaviors
                        .UnlocksBehavior
                        .Instance
                        .ProgressByItemId;
                    progress.TryGetValue(kvp.Key, out int prev);
                    progress[kvp.Key] = prev + kvp.Value;
                }
            }

            Log.Info($"Item unlock progress migrated: items={_defeatsByItemId.Count}");
        }
    }
}
