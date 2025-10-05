using System;
using Retinues.Core.Features.Doctrines.Effects.Behaviors;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core.Features.Doctrines.Effects
{
    /// <summary>
    /// Campaign behavior for adding doctrine effect mission behaviors to battles and tournaments.
    /// </summary>
    [SafeClass]
    public sealed class DoctrineEffectRuntimeBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// No sync data needed for doctrine effect runtime behavior.
        /// </summary>
        public override void SyncData(IDataStore dataStore) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Registers event listener for mission start to add doctrine effect mission behaviors.
        /// </summary>
        public override void RegisterEvents()
        {
            // Missions
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Adds ImmortalsBehavior and IndomitableBehavior to the mission if not already present.
        /// </summary>
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
