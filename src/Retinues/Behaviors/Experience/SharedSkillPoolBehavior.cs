using System;
using Retinues.Domain.Characters.Wrappers;
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
        private double _sharedSkillPointProgress;

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
        /// Legacy raw XP, retained only for migrating old saves.
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

        internal static int AddExperience(int experience, int requiredExperience)
        {
            if (_instance == null)
                return 0;

            _instance.MigrateLegacyExperience();
            int points = SkillPointProgress.Add(
                ref _instance._sharedSkillPointProgress,
                experience,
                requiredExperience
            );
            _instance._sharedSkillPoints += points;
            return points;
        }

        protected override void OnGameLoadFinished() => MigrateLegacyExperience();

        private void MigrateLegacyExperience()
        {
            if (_sharedSkillPointsExperience <= 0)
                return;

            // Old saves did not record which troop earned the remainder. Preserve its best
            // attainable value using the cheapest eligible troop, independent of XP event order.
            // If no eligible troop exists yet, keep the old XP until one becomes available.
            int cheapest = int.MaxValue;
            foreach (var troop in WCharacter.All)
            {
                if (troop == null || troop.IsHero || troop.IsVanilla || !troop.IsPlayerFactionTroop)
                    continue;
                int cost = SkillPointExperienceGain.GetXpRequiredForSkillPoint(troop.Base);
                if (cost > 0 && cost < 100000000)
                    cheapest = Math.Min(cheapest, cost);
            }

            if (cheapest == int.MaxValue)
                return;

            _sharedSkillPoints += SkillPointProgress.MigrateLegacy(
                ref _sharedSkillPointProgress,
                ref _sharedSkillPointsExperience,
                cheapest
            );
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
                dataStore.SyncData("Retinues_SharedSkillPointProgress", ref _sharedSkillPointProgress);
            }
            catch (Exception e)
            {
                Log.Exception(e, "SharedSkillPoolBehavior.SyncData failed.");
            }
        }
    }
}
