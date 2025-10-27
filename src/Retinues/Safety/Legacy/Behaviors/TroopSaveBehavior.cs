using System.Collections.Generic;
using Retinues.Features.Xp.Behaviors;
using Retinues.Troops;
using Retinues.Troops.Save;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Safety.Legacy.Behaviors
{
    /// <summary>
    /// Legacy campaign behavior for migrating custom troop roots and XP from older saves.
    /// Loads troop data and restores XP pools after session launch if legacy save data is present.
    /// </summary>
    [SafeClass]
    public sealed class TroopSaveBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private List<TroopSaveData> _troops;

        /// <summary>
        /// Returns true if legacy troop save data is present.
        /// </summary>
        public bool HasSyncData => _troops != null && _troops.Count > 0;

        /// <summary>
        /// Syncs legacy troop data to and from the campaign save file.
        /// </summary>
        public override void SyncData(IDataStore ds)
        {
            if (ds.IsSaving)
                _troops = null; // Clear reference before saving

            ds.SyncData("Retinues_Troops", ref _troops);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Registers event listener for session launch to migrate troop roots and XP.
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
        /// Migrates troop roots and XP pools from legacy save data after session launch.
        /// </summary>
        private void OnAfterSessionLaunched(CampaignGameStarter starter)
        {
            if (_troops is { Count: > 0 })
            {
                foreach (var data in _troops)
                {
                    var troop = TroopLoader.Load(data);
                    TroopXpBehavior.Add(troop, data.XpPool);
                }

                Log.Info($"Troops migrated: {_troops.Count} roots.");
            }
        }
    }
}
