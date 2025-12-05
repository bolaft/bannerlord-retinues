using TaleWorlds.CampaignSystem;

namespace Retinues.Wrappers
{
    /// <summary>
    /// Internal entry used to sync all instances of a wrapped type.
    /// </summary>
    internal interface IWrappedSync
    {
        void Sync(IDataStore dataStore);
    }

    /// <summary>
    /// Central persistence for all WrappedObject types.
    /// </summary>
    public class WrappedObjectPersistenceBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents() { }

        public override void SyncData(IDataStore dataStore)
        {
            foreach (var entry in WrappedRegistry.Entries)
                entry.Sync(dataStore);
        }
    }
}
