using System.Collections.Generic;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Safety.Legacy.Behaviors
{
    /// <summary>
    /// Legacy campaign behavior for migrating item unlock progress from older saves.
    /// Transfers defeat counts to the new unlocks system after session launch.
    /// </summary>
    [SafeClass]
    public sealed class UnlocksBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private Dictionary<string, int> _defeatsByItemId = [];

        /// <summary>
        /// Returns true if legacy item unlock progress data is present.
        /// </summary>
        public bool HasSyncData => _defeatsByItemId != null && _defeatsByItemId.Count > 0;

        /// <summary>
        /// Syncs legacy item unlock progress to and from the campaign save file.
        /// </summary>
        public override void SyncData(IDataStore ds)
        {
            if (ds.IsSaving)
                _defeatsByItemId = null; // Clear reference before saving

            ds.SyncData(nameof(_defeatsByItemId), ref _defeatsByItemId);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Registers event listener for session launch to migrate item unlock progress.
        /// </summary>
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

        /// <summary>
        /// Migrates item unlock progress from legacy save data after session launch.
        /// </summary>
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
