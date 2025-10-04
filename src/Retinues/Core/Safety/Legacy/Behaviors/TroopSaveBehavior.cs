using System.Collections.Generic;
using Retinues.Core.Features.Xp.Behaviors;
using Retinues.Core.Troops;
using Retinues.Core.Troops.Save;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Safety.Legacy.Behaviors
{
    [SafeClass]
    public sealed class TroopSaveBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private List<TroopSaveData> _troops;

        public bool HasSyncData => _troops != null && _troops.Count > 0;

        public override void SyncData(IDataStore ds)
        {
            if (ds.IsSaving)
                _troops = null; // Clear reference before saving

            ds.SyncData("Retinues_Troops", ref _troops);
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
