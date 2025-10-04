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

        private Dictionary<string, int> _stocksByItemId;
        public Dictionary<string, int> StocksByItemId => _stocksByItemId ??= [];

        public bool HasSyncData => StocksByItemId.Count > 0;

        public override void SyncData(IDataStore ds)
        {
            ds.SyncData("Retinues_Stocks", ref _stocksByItemId);
            Log.Info($"{StocksByItemId.Count} entries.");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents() { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static bool HasStock(string itemId) => Get(itemId) > 0;

        public static int Get(string itemId)
        {
            if (Instance == null || itemId == null)
                return 0;
            return Instance.StocksByItemId.TryGetValue(itemId, out var c) ? c : 0;
        }

        public static void Set(string itemId, int count)
        {
            if (Instance == null || itemId == null)
                return;
            if (count <= 0)
                Instance.StocksByItemId.Remove(itemId);
            else
                Instance.StocksByItemId[itemId] = count;
        }

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
