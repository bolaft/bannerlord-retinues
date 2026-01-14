using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Framework.Behaviors;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Retinues
{
    /// <summary>
    /// Retinue management behavior.
    /// Keeps the retinue creation entrypoint but does not auto-run on campaign start.
    /// Also tracks per-culture progress to unlock one retinue per culture.
    /// </summary>
    public partial class RetinuesBehavior : BaseCampaignBehavior<RetinuesBehavior>
    {
        public override bool IsActive => Settings.EnableRetinues;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Constants                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public const int UnlockProgressTarget = 1000;

        /* ━━━━━━━━ Points ━━━━━━━━ */
        private const int Progress_TournamentWin = 50;
        private const int Progress_QuestCompleted = 25;
        private const int Progress_DailyPerOwnedFief = 5;
        private const int Progress_DailyPerOwnedWorkshop = 2;
        private const int Progress_WonBattleWithAllies = 10;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Sync Data                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const string DataStoreKey_ProgressCultureIds =
            "Retinues_RetinueUnlock_ProgressCultureIds";
        private const string DataStoreKey_ProgressValues = "Retinues_RetinueUnlock_ProgressValues";
        private const string DataStoreKey_UnlockedCultureIds =
            "Retinues_RetinueUnlock_UnlockedCultureIds";

        // Stored (safe) representation: parallel lists.
        private List<string> _progressCultureIds = [];
        private List<int> _progressValues = [];
        private List<string> _unlockedCultureIds = [];

        /// <summary>
        /// Syncs retinue unlock progress data.
        /// </summary>
        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData(DataStoreKey_ProgressCultureIds, ref _progressCultureIds);
            dataStore.SyncData(DataStoreKey_ProgressValues, ref _progressValues);
            dataStore.SyncData(DataStoreKey_UnlockedCultureIds, ref _unlockedCultureIds);

            NormalizeProgressLists();
        }
    }
}
