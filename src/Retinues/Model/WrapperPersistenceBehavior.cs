using TaleWorlds.CampaignSystem;

namespace Retinues.Model
{
    /// <summary>
    /// Internal entry used to sync all instances of a wrapper type.
    /// </summary>
    internal interface IWrapperSync
    {
        void Sync(IDataStore dataStore);
    }

    /// <summary>
    /// Central persistence for all Wrapper types.
    /// </summary>
    public class WrapperPersistenceBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents() { }

        public override void SyncData(IDataStore dataStore)
        {
            foreach (var entry in WrapperRegistry.Entries)
                entry.Sync(dataStore);
        }
    }
}
