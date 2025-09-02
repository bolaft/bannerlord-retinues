using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem;
using CustomClanTroops.Wrappers.Campaign;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.Logic.Items
{
    public static class StockManager
    {
        // Tracks the number of each item in stock
        public static readonly Dictionary<ItemObject, int> Stocks = new();

        // Add one to the stock of an item
        public static void AddToStock(ItemObject item)
        {
            if (item == null) return;
            if (Stocks.ContainsKey(item))
                Stocks[item]++;
            else
                Stocks[item] = 1;
        }

        // Remove one from the stock of an item (minimum 0)
        public static void RemoveFromStock(ItemObject item)
        {
            if (item == null) return;
            if (Stocks.TryGetValue(item, out int count) && count > 0)
                Stocks[item] = count - 1;
        }

        public static int GetStock(ItemObject item)
        {
            if (item == null) return 0;
            if (Stocks.TryGetValue(item, out int count))
                return count;
            return 0;
        }
    }
}
