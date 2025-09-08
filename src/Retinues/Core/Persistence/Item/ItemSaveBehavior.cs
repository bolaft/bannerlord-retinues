using TaleWorlds.CampaignSystem;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

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
            dataStore.SyncData("Retinues", ref _itemData);
        }

        private void OnGameLoaded(CampaignGameStarter starter)
        {
            Log.Debug("OnGameLoaded.");

            // Restore stocks and unlocked items
            if (_itemData != null)
            {
                // Clear existing items, if any
                WItem.UnlockedItems.Clear();
                WItem.Stocks.Clear();

                // Restore from save
                ItemSave.Load(_itemData);

                Log.Debug($"Restored {_itemData.UnlockedItemIds.Count} unlocked items and {_itemData.StockedItems.Count} stocked items.");
            }
            else
            {
                Log.Debug("No item data found in save.");
            }
        }

        private void OnBeforeSave()
        {
            Log.Debug("Collecting item data.");

            _itemData = ItemSave.Save();

            Log.Debug($"Collected {_itemData.UnlockedItemIds.Count} unlocked items and {_itemData.StockedItems.Count} stocked items.");
        }
    }
}
