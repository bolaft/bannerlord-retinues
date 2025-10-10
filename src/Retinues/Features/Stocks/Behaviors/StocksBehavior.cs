using System.Collections.Generic;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Features.Stocks.Behaviors
{
    /// <summary>
    /// Campaign behavior for tracking item stocks. Provides static API for getting, setting, and adding stock counts.
    /// </summary>
    [SafeClass]
    public sealed class StocksBehavior : CampaignBehaviorBase
    {
        public static StocksBehavior Instance { get; private set; }

        public StocksBehavior() => Instance = this;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private Dictionary<string, int> _stocksByItemId;
        public Dictionary<string, int> StocksByItemId => _stocksByItemId ??= [];

        public override void SyncData(IDataStore ds)
        {
            ds.SyncData("Retinues_Stocks", ref _stocksByItemId);
            Log.Info($"{StocksByItemId.Count} entries.");
            Log.Dump(_stocksByItemId, LogLevel.Debug);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents() { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns true if the item is stocked (count > 0).
        /// </summary>
        public static bool HasStock(string itemId) => Get(itemId) > 0;

        /// <summary>
        /// Gets the stock count for an item.
        /// </summary>
        public static int Get(string itemId)
        {
            if (Instance == null || itemId == null)
                return 0;
            return Instance.StocksByItemId.TryGetValue(itemId, out var c) ? c : 0;
        }

        /// <summary>
        /// Sets the stock count for an item (removes if count <= 0).
        /// </summary>
        public static void Set(string itemId, int count)
        {
            if (Instance == null || itemId == null)
                return;
            if (count <= 0)
                Instance.StocksByItemId.Remove(itemId);
            else
                Instance.StocksByItemId[itemId] = count;
        }

        /// <summary>
        /// Adds delta to the stock count for an item (removes if count <= 0).
        /// </summary>
        public static void Add(string itemId, int delta)
        {
            if (Instance == null || itemId == null || delta == 0)
                return;
            Instance.StocksByItemId.TryGetValue(itemId, out var cur);
            var next = cur + delta;
            if (next <= 0)
                Instance.StocksByItemId.Remove(itemId);
            else
                Instance.StocksByItemId[itemId] = next;
        }
    }
}
