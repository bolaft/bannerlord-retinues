using Retinues.Core.Features.Stocks.Behaviors;
using Retinues.Core.Features.Unlocks.Behaviors;
using Retinues.Core.Game;
using Retinues.Core.Troops;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Retinues.Core.Safety.Backup
{
    [SafeClass]
    public sealed class BackupBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool _retinuesWasUsed = false;

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData(nameof(_retinuesWasUsed), ref _retinuesWasUsed);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            // Fire when the campaign UI is ready (works for both new and loaded games)
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void OnGameLoaded(CampaignGameStarter starter)
        {
            if (_retinuesWasUsed == true)
            {
                Log.Debug("Backup prompt already handled for this save.");
                return; // already handled for this save
            }

            _retinuesWasUsed = true;

            if (HasSaveData())
            {
                Log.Debug("Retinues save data found; skipping first-run backup.");
                return; // from previous version, no need to prompt again
            }
            else
            {
                Log.Debug("No Retinues save data found; assuming first run.");
                ShowFirstRunPopup();
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool HasSyncData(CampaignBehaviorBase behavior)
        {
            if (behavior == null)
                return false;

            var prop = behavior.GetType().GetProperty("HasSyncData");
            if (prop == null || prop.PropertyType != typeof(bool))
                return false;

            return (bool)prop.GetValue(behavior);
        }

        private bool HasSaveData()
        {
            foreach (
                var behavior in new CampaignBehaviorBase[]
                {
                    Campaign.Current.GetCampaignBehavior<TroopBehavior>(),
                    Campaign.Current.GetCampaignBehavior<StocksBehavior>(),
                    Campaign.Current.GetCampaignBehavior<UnlocksBehavior>(),
                }
            )
            {
                if (HasSyncData(behavior))
                {
                    Log.Debug($"{behavior.GetType().Name} save data found.");
                    return true;
                }
            }

            Log.Debug("No Retinues save data found.");
            return false;
        }

        private void CreateBackupSave()
        {
            string backupName = $"[RetinuesBackup] {Player.Name} - {Player.Clan.Name}";
            Campaign.Current.SaveHandler.SaveAs(backupName);
        }

        private void ShowFirstRunPopup()
        {
            InformationManager.ShowInquiry(
                new InquiryData(
                    titleText: L.S("first_run_title", "Retinues\nExisting Save Detected"),
                    text: L.S(
                        "first_run_text",
                        "Welcome to Retinues!\n\nIt looks like you are using Retinues for the first time with an existing save. Since Retinues modifies troop data and introduces new ones in the game world, it is recommended to create a backup of your save before proceeding, just in case.\n\nWould you like to automatically create a backup now?"
                    ),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("yes", "Yes"),
                    negativeText: L.S("no", "No"),
                    affirmativeAction: () =>
                    {
                        CreateBackupSave();
                    },
                    negativeAction: () => { }
                )
            );
        }
    }
}
