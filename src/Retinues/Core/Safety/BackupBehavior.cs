using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using Retinues.Core.Utils;
using Retinues.Core.Persistence.Troop;
using Retinues.Core.Persistence.Item;

namespace Retinues.Core.Safety
{
    [SafeClass]
    public sealed class BackupBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool _retUsed;

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("Retinues_Backup", ref _retUsed);
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
            if (_retUsed) return; // already handled for this save

            _retUsed = true;

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

        private bool HasSaveData()
        {
            // Check for TroopSaveBehavior data
            var troopBehavior = Campaign.Current.GetCampaignBehavior<TroopSaveBehavior>();
            if (troopBehavior.HasTroopData)
            {
                Log.Debug("Troop save data found.");
                return true;
            }

            // Check for ItemSaveBehavior data
            var itemBehavior = Campaign.Current.GetCampaignBehavior<ItemSaveBehavior>();
            if (itemBehavior.HasItemData)
            {
                Log.Debug("Item save data found.");
                return true;
            }

            Log.Debug("No Retinues save data found.");
            return false; // no relevant save data found
        }

        private void CreateBackupSave()
        {
            string backupName = $"[RetinuesBackup] {Campaign.Current.UniqueGameId}";
            Campaign.Current.SaveHandler.SaveAs(backupName);

            InformationManager.DisplayMessage(
                new InformationMessage(
                    L.S(
                        "backup_created",
                        $"Backup save '{backupName}' created successfully."
                    )
                )
            );
        }

        private void ShowFirstRunPopup()
        {
            InformationManager.ShowInquiry(
                new InquiryData(
                    titleText: L.S("first_run_title", "Retinues - Existing Save Detected"),
                    text: L.S(
                        "first_run_text",
                        "Welcome to Retinues!\n\nIt looks like you are using Retinues with an existing save. Since Retinues modifies troop data and introduces new troops in the game world, it is strongly recommended to create a backup of your save before proceeding. This will allow you to restore your game if anything goes wrong.\n\nWould you like to automatically create a backup now?"
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
