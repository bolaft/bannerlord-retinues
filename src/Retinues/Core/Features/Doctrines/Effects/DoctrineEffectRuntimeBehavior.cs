using System;
using Retinues.Core.Features.Doctrines.Effects.Behaviors;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core.Features.Doctrines.Effects
{
    [SafeClass]
    public sealed class DoctrineEffectRuntimeBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void SyncData(IDataStore dataStore)
        {
            // No persistent state here — feat progress is persisted by DoctrineServiceBehavior.
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            // Missions
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void OnMissionStarted(IMission iMission)
        {
            try
            {
                if (iMission is not Mission mission)
                    return; // Not a battle or a tournament

                if (mission.GetMissionBehavior<ImmortalsBehavior>() == null)
                    mission.AddMissionBehavior(new ImmortalsBehavior());

                if (mission.GetMissionBehavior<IndomitableBehavior>() == null)
                    mission.AddMissionBehavior(new IndomitableBehavior());
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }
    }
}
