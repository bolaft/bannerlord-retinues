using System.Collections.Generic;
using Retinues.Core.Features.Stocks.Behaviors;
using Retinues.Core.Features.Unlocks.Behaviors;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace Retinues.Core.Safety.Legacy
{
    /* ━━━━━━━ Save Data ━━━━━━ */

    public class ItemSaveData
    {
        [SaveableField(1)]
        public List<string> UnlockedItemIds = [];

        [SaveableField(2)]
        public Dictionary<string, int> StockedItems = [];
    }

    [SafeClass]
    public sealed class ItemSaveBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private ItemSaveData _items;

        public override void SyncData(IDataStore ds)
        {
            if (ds.IsSaving)
                _items = null; // Clear reference before saving

            ds.SyncData("Retinues_Items", ref _items);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(
                this,
                OnAfterSessionLaunched
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void OnAfterSessionLaunched(CampaignGameStarter starter)
        {
            if (_items == null)
                return;

            // Unlocks
            if (_items.UnlockedItemIds is { Count: > 0 })
            {
                foreach (var id in _items.UnlockedItemIds)
                {
                    var item = MBObjectManager.Instance.GetObject<ItemObject>(id);
                    if (item != null)
                        UnlocksBehavior.Unlock(item);
                }
            }

            // Stocks
            if (_items.StockedItems is { Count: > 0 } && StocksBehavior.Instance != null)
            {
                foreach (var kv in _items.StockedItems)
                    StocksBehavior.Set(kv.Key, kv.Value);
            }

            Log.Info(
                $"Items migrated: unlocked={_items.UnlockedItemIds?.Count ?? 0}, stocks={_items.StockedItems?.Count ?? 0}"
            );
        }
    }
}
