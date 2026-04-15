using System;
using Retinues.Framework.Behaviors;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;

namespace Retinues.Behaviors.Experience
{
    /// <summary>
    /// Persists the shared skill points pool used when the Shared Skill Points Pool setting is on.
    /// When that setting is active, all custom troops contribute XP to and spend skill points from
    /// this single campaign-wide pool instead of per-troop pools.
    /// </summary>
    public sealed class SharedSkillPoolBehavior : BaseCampaignBehavior
    {
        private static SharedSkillPoolBehavior _instance;

        private int _sharedSkillPoints;
        private int _sharedSkillPointsExperience;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Static Access                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Available skill points in the shared pool.
        /// </summary>
        public static int SharedSkillPoints
        {
            get => _instance?._sharedSkillPoints ?? 0;
            set
            {
                if (_instance != null)
                    _instance._sharedSkillPoints = value;
            }
        }

        /// <summary>
        /// Accumulated XP toward the next skill point in the shared pool.
        /// </summary>
        public static int SharedSkillPointsExperience
        {
            get => _instance?._sharedSkillPointsExperience ?? 0;
            set
            {
                if (_instance != null)
                    _instance._sharedSkillPointsExperience = value;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Construction                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public SharedSkillPoolBehavior()
        {
            _instance = this;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Sync Data                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const string SharedSkillPointsKey = "Retinues_SharedSkillPoints";
        private const string SharedSkillPointsExperienceKey =
            "Retinues_SharedSkillPointsExperience";

        public override void SyncData(IDataStore dataStore)
        {
            try
            {
                dataStore.SyncData(SharedSkillPointsKey, ref _sharedSkillPoints);
                dataStore.SyncData(
                    SharedSkillPointsExperienceKey,
                    ref _sharedSkillPointsExperience
                );
            }
            catch (Exception e)
            {
                Log.Exception(e, "SharedSkillPoolBehavior.SyncData failed.");
            }
        }
    }
}
