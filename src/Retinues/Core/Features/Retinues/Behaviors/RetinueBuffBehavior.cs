using System;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core.Features.Retinues.Behaviors
{
    /// <summary>
    /// Campaign behavior for adding retinue buff mission behavior to battles and tournaments.
    /// </summary>
    [SafeClass]
    public sealed class RetinueBuffBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// No sync data needed for retinue buff behavior.
        /// </summary>
        public override void SyncData(IDataStore dataStore) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Registers event listener for mission start to add retinue buff mission behavior.
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
        /// Adds RetinueBuffMissionBehavior to the mission if not already present.
        /// </summary>
        private void OnMissionStarted(IMission iMission)
        {
            try
            {
                if (iMission is not Mission mission)
                    return; // Not a battle or a tournament

                if (mission.GetMissionBehavior<RetinueBuffMissionBehavior>() == null)
                    mission.AddMissionBehavior(new RetinueBuffMissionBehavior());
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }
    }
}
