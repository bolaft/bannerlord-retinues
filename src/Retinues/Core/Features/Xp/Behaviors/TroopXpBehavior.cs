using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Core.Features.Xp.Behaviors
{
    public sealed class TroopXpBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void SyncData(IDataStore dataStore)
        {
            // XP saved in TroopSaveBehavior
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            Log.Info("Registering TroopXpBehavior events.");

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
    }
}
