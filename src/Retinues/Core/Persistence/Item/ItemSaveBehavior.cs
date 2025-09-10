using Retinues.Core.Game.Features.Stocks;
using Retinues.Core.Game.Features.Unlocks;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Persistence.Item
{
    public class ItemSaveBehavior : CampaignBehaviorBase
    {
        private ItemSaveData _itemData = null;

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, OnBeforeSave);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Persist the troops inside the native save.
            dataStore.SyncData("Retinues_Items", ref _itemData);
        }

        private void OnBeforeSave()
        {
            Log.Debug("Collecting item data.");

            _itemData = ItemSave.Save();

            Log.Debug(
                $"Collected {_itemData.UnlockedItemIds.Count} unlocked items and {_itemData.StockedItems.Count} stocked items."
            );
        }

        private void OnGameLoaded(CampaignGameStarter starter)
        {
            // Restore stocks and unlocked items
            if (_itemData != null)
            {
                // Clear existing items, if any
                UnlocksManager.UnlockedItems.Clear();
                StocksManager.Stocks.Clear();

                // Restore from save
                ItemSave.Load(_itemData);

                Log.Debug(
                    $"Restored {_itemData.UnlockedItemIds.Count} unlocked items and {_itemData.StockedItems.Count} stocked items."
                );
            }
            else
            {
                Log.Debug("No item data found in save.");
            }
        }
    }
}
