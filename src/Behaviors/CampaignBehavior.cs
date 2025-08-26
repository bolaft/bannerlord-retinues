using TaleWorlds.CampaignSystem;
using System;
using System.Linq;
using System.Collections.Generic;
using CustomClanTroops.Utils;

namespace CustomClanTroops.Behaviors
{
    public class CampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, OnBeforeSave);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            Log.Info("OnSessionLaunched event triggered.");
        }

        private void OnGameLoaded(CampaignGameStarter starter)
        {
            Log.Info("OnGameLoaded event triggered.");
        }

        private void OnBeforeSave()
        {
            Log.Info("OnBeforeSave event triggered.");
        }
    }
}
