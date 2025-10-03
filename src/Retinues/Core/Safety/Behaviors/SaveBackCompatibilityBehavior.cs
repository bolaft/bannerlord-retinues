using System;
using System.Collections.Generic;
using Retinues.Core.Features.Stocks.Behaviors;
using Retinues.Core.Features.Unlocks.Behaviors;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Safety.Behaviors
{
    public class ItemSaveData
    {
        public List<string> UnlockedItemIds = [];
        public Dictionary<string, int> StockedItems = [];
    }

    [SafeClass]
    public sealed class SaveBackCompatibilityBehavior : CampaignBehaviorBase
    {
        private bool _migrationDone = false;

        private ItemSaveData _legacyItemBlob = null;

        private List<string> _pendingUnlockIds = null;
        private Dictionary<string, int> _pendingStocks = null;

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, _ => TryFlush());
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(
                this,
                _ => TryFlush()
            );
        }

        public override void SyncData(IDataStore ds)
        {
            // Keep state
            ds.SyncData(nameof(_migrationDone), ref _migrationDone);

            // Read the legacy container
            ds.SyncData("Retinues_Items", ref _legacyItemBlob);

            if (_migrationDone || _legacyItemBlob == null)
                return; // nothing to do

            try
            {
                // Stage unlocks
                if (_legacyItemBlob.UnlockedItemIds is { Count: > 0 })
                {
                    _pendingUnlockIds ??= new List<string>(_legacyItemBlob.UnlockedItemIds.Count);
                    foreach (var id in _legacyItemBlob.UnlockedItemIds)
                        if (!string.IsNullOrWhiteSpace(id))
                            _pendingUnlockIds.Add(id);
                }

                // Stage stocks
                if (_legacyItemBlob.StockedItems is { Count: > 0 })
                {
                    _pendingStocks ??= new Dictionary<string, int>(StringComparer.Ordinal);
                    foreach (var kv in _legacyItemBlob.StockedItems)
                    {
                        if (string.IsNullOrWhiteSpace(kv.Key))
                            continue;
                        if (kv.Value <= 0)
                            continue;
                        _pendingStocks[kv.Key] = kv.Value;
                    }
                }

                // Mark legacy blob for deletion on next save
                _legacyItemBlob = null;
                _migrationDone = true;

                Log.Info(
                    $"[RetroCompat] Staged migration: unlocks={_pendingUnlockIds?.Count ?? 0}, stocks={_pendingStocks?.Count ?? 0}."
                );
            }
            catch (Exception e)
            {
                Log.Exception(e, "[RetroCompat] Error staging legacy migration.");
                // Don't set _migrationDone so we can retry next load if needed.
            }
        }

        private void TryFlush()
        {
            if (
                (_pendingUnlockIds == null || _pendingUnlockIds.Count == 0)
                && (_pendingStocks == null || _pendingStocks.Count == 0)
            )
                return; // nothing to do

            bool unlockedOk = false,
                stocksOk = false;

            try
            {
                // Flush unlocks if UnlocksBehavior is ready
                if (_pendingUnlockIds is { Count: > 0 } && UnlocksBehavior.Instance != null)
                {
                    int count = 0;
                    foreach (var id in _pendingUnlockIds)
                    {
                        var item =
                            TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObject<TaleWorlds.Core.ItemObject>(
                                id
                            );
                        if (item == null)
                            continue;
                        UnlocksBehavior.Unlock(item);
                        count++;
                    }
                    Log.Info($"[RetroCompat] Migrated {count} legacy unlocked items.");
                    _pendingUnlockIds.Clear();
                    _pendingUnlockIds = null;
                    unlockedOk = true;
                }

                // Flush stocks if StocksBehavior is ready
                if (_pendingStocks is { Count: > 0 } && StocksBehavior.Instance != null)
                {
                    int count = 0;
                    foreach (var kv in _pendingStocks)
                    {
                        StocksBehavior.Set(kv.Key, kv.Value);
                        count++;
                    }
                    Log.Info($"[RetroCompat] Migrated {count} legacy stock entries.");
                    _pendingStocks.Clear();
                    _pendingStocks = null;
                    stocksOk = true;
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "[RetroCompat] Error flushing staged migration.");
            }

            // If either target wasn't ready yet, try again on the next event.
            if (!unlockedOk || !stocksOk)
                Log.Debug("[RetroCompat] Waiting for target behaviors to initialize…");
        }
    }
}
