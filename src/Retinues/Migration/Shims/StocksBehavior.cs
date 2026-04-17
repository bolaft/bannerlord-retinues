using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

// ReSharper disable once CheckNamespace
namespace Retinues.Migration.Shims
{
    /// <summary>
    /// Read-only shim for v1 <c>StocksBehavior</c>.
    /// Deserializes per-item stock counts from old saves.
    /// </summary>
    internal sealed class StocksBehavior : CampaignBehaviorBase
    {
        internal Dictionary<string, int> StocksByItemId;

        public override void RegisterEvents() { }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("Retinues_Stocks", ref StocksByItemId);
        }
    }
}
