using System.Collections.Generic;
using Retinues.Core.Features.Stocks.Behaviors;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace Retinues.Core.Safety.Legacy.Behaviors
{
    /// <summary>
    /// Legacy campaign behavior for migrating unlocked items and stocks from older saves.
    /// Unlocks items and restores stocks on session launch if legacy save data is present.
    /// </summary>
    [SafeClass]
    public sealed class ItemSaveBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private ItemSaveData _items;

        /// <summary>
        /// Returns true if legacy item save data is present.
        /// </summary>
        public bool HasSyncData => _items != null;

        /// <summary>
        /// Syncs legacy item data to and from the campaign save file.
        /// </summary>
        public override void SyncData(IDataStore ds)
        {
            if (ds.IsSaving)
                _items = null; // Clear reference before saving

            ds.SyncData("Retinues_Items", ref _items);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Registers event listener for session launch to migrate items and stocks.
        /// </summary>
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

        /// <summary>
        /// Migrates unlocked items and stocks from legacy save data after session launch.
        /// </summary>
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
                        Features.Unlocks.Behaviors.UnlocksBehavior.Unlock(item);
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

    /// <summary>
    /// Legacy save data for unlocked items and stocks.
    /// </summary>
    public class ItemSaveData
    {
        [SaveableField(1)]
        public List<string> UnlockedItemIds = [];

        [SaveableField(2)]
        public Dictionary<string, int> StockedItems = [];
    }
}
