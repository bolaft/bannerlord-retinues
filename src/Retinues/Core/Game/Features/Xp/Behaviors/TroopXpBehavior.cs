using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Core.Game.Features.Xp.Behaviors
{
    public sealed class TroopXpBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                this,
                _ =>
                {
                    TroopXpService.InitializeSnapshotFromRoster();
                }
            );

            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(
                this,
                mission =>
                {
                    var mapEvent = MobileParty.MainParty?.MapEvent;
                    if (mapEvent == null || !mapEvent.IsPlayerMapEvent)
                        return;

                    if (
                        mission is TaleWorlds.MountAndBlade.Mission realMission
                        && realMission.GetMissionBehavior<TroopXpMissionBehavior>() == null
                    )
                        realMission.AddMissionBehavior(new TroopXpMissionBehavior());
                }
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void SyncData(IDataStore dataStore)
        {
            // unchanged
            var dict = TroopXpService._pool;
            dataStore.SyncData("CCT_TroopXpPool", ref dict);
            TroopXpService._pool = dict ?? [];

            Log.Debug($"Troop XP pools loaded: {TroopXpService._pool.Count} entries:");
            foreach (var kv in TroopXpService._pool)
                Log.Debug($"  {kv.Key}: {kv.Value}");
        }
    }
}
