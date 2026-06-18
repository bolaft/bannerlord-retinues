using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace Retinues.Mods.OldRealms
{
    /// <summary>
    /// Registers Retinues custom troops into The Old Realms' extended-info registry so they keep
    /// their TOR attributes in battle. Syncs at session start and before each mission (the latter
    /// is the reliable one — TOR has loaded its XML by then and agents have not spawned yet).
    /// Only added to the campaign when TOR is installed.
    /// </summary>
    [SafeClass]
    public class OldRealmsBehavior : CampaignBehaviorBase
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
