using Retinues.Framework.Runtime;
using TaleWorlds.CampaignSystem;

namespace Retinues.Compatibility.Interops.OldRealms
{
    /// <summary>
    /// Registers Retinues custom troops into The Old Realms' extended-info registry so they keep
    /// their TOR attributes in battle. Syncs at session start and before each mission (the latter
    /// is the reliable one — TOR has loaded its XML by then and agents have not spawned yet).
    /// Only registered when TOR is installed (see InteropsManager.RegisterBehaviors).
    /// </summary>
    [SafeClass]
    internal sealed class OldRealmsBehavior : CampaignBehaviorBase
    {
        public override void SyncData(IDataStore dataStore) { }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                this,
                _ => OldRealmsExtendedInfo.Sync()
            );
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(
                this,
                _ => OldRealmsExtendedInfo.Sync()
            );
        }
    }
}
