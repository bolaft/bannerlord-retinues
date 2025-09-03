using TaleWorlds.CampaignSystem;
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
            Log.Debug("OnSessionLaunched event triggered.");
        }

        private void OnGameLoaded(CampaignGameStarter starter)
        {
            Log.Debug("OnGameLoaded event triggered.");
        }

        private void OnBeforeSave()
        {
            Log.Debug("OnBeforeSave event triggered.");
        }
    }
}
