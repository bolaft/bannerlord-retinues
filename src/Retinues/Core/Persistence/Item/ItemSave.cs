using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Game.Features.Stocks;
using Retinues.Core.Game.Features.Unlocks;

namespace Retinues.Core.Persistence.Item
{
    public static class ItemSave
    {
        // =========================================================================
        // Saving
        // =========================================================================

        public static ItemSaveData Save()
        {
            return new ItemSaveData
            {
                UnlockedItemIds = [.. UnlocksManager.UnlockedItems.Select(item => item.StringId)],
                StockedItems = StocksManager.Stocks.ToDictionary(kv => kv.Key.StringId, kv => kv.Value)
            };
        }

        // =========================================================================
        // Loading
        // =========================================================================

        public static void Load(ItemSaveData data)
        {
            // Clear existing unlocked items
            UnlocksManager.UnlockedItems.Clear();
            StocksManager.Stocks.Clear();

            // Restore unlocked items
            foreach (var id in data.UnlockedItemIds)
            {
                var item = MBObjectManager.Instance.GetObject<ItemObject>(id);
                if (item != null)
                    new WItem(item).Unlock();
            }

            // Restore stocked items
            foreach (var kv in data.StockedItems)
            {
                var item = MBObjectManager.Instance.GetObject<ItemObject>(kv.Key);
                if (item != null)
                    StocksManager.SetStock(new WItem(item), kv.Value);
            }
        }
    }
}
