using System.Collections.Generic;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Features.Stocks.Behaviors
{
    [SafeClass]
    public sealed class StocksBehavior : CampaignBehaviorBase
    {
        public static StocksBehavior Instance { get; private set; }

        public StocksBehavior() => Instance = this;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private Dictionary<string, int> _stockByItemId = [];

        public bool HasSyncData => _stockByItemId != null && _stockByItemId.Count > 0;

        public override void SyncData(IDataStore ds)
        {
            ds.SyncData(nameof(_stockByItemId), ref _stockByItemId);
            Log.Info($"Stocks SyncData: {_stockByItemId.Count} entries.");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents() { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static int Get(string itemId)
        {
            if (Instance == null || itemId == null)
                return 0;
            return Instance._stockByItemId.TryGetValue(itemId, out var c) ? c : 0;
        }

        public static bool HasStock(string itemId) => Get(itemId) > 0;

        public static void Set(string itemId, int count)
        {
            if (Instance == null || itemId == null)
                return;
            if (count <= 0)
                Instance._stockByItemId.Remove(itemId);
            else
                Instance._stockByItemId[itemId] = count;
        }

        public static void Add(string itemId, int delta)
        {
            if (Instance == null || itemId == null || delta == 0)
                return;
            Instance._stockByItemId.TryGetValue(itemId, out var cur);
            var next = cur + delta;
            if (next <= 0)
                Instance._stockByItemId.Remove(itemId);
            else
                Instance._stockByItemId[itemId] = next;
        }
    }
}
