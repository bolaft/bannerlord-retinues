using System.Collections.Generic;
using Retinues.Core.Game.Wrappers;

namespace Retinues.Core.Game.Features.Stocks
{
    public static class StocksManager
    {
        public static Dictionary<WItem, int> Stocks { get; } = [];

        public static void SetStock(WItem item, int count)
        {
            if (count <= 0)
                Stocks.Remove(item);
            else
                Stocks[item] = count;
        }
    }
}
