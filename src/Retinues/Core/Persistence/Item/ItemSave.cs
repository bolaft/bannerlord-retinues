using System.Linq;
using Retinues.Core.Game.Wrappers;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

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
                UnlockedItemIds = [.. WItem.UnlockedItems.Select(item => item.StringId)],
                StockedItems = WItem.Stocks.ToDictionary(
                    kv => kv.Key.StringId,
                    kv => kv.Value
                ),
            };
        }

        // =========================================================================
        // Loading
        // =========================================================================

        public static void Load(ItemSaveData data)
        {
            // Clear existing unlocked items
            WItem.UnlockedItems.Clear();
            WItem.Stocks.Clear();

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
                    WItem.SetStock(new WItem(item), kv.Value);
            }
        }
    }
}
