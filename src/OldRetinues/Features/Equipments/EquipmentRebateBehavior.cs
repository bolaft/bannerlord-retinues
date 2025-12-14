using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;

namespace OldRetinues.Features.Equipments
{
    /// <summary>
    /// Tracks per-item purchase counts to apply global cost rebates on future purchases of that item.
    /// </summary>
    [SafeClass]
    public sealed class EquipmentRebateBehavior : CampaignBehaviorBase
    {
        public static EquipmentRebateBehavior Instance { get; private set; }

        // ItemId -> purchaseCount (global, not per troop).
        private Dictionary<string, int> _itemPurchaseCounts = [];

        public EquipmentRebateBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            // No events; accessed via static API from EquipmentManager.
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("Retinues_EquipmentRebates", ref _itemPurchaseCounts);

            if (dataStore.IsLoading && _itemPurchaseCounts == null)
                _itemPurchaseCounts = [];
        }

        /// <summary>
        /// Returns a multiplicative rebate multiplier for this item.
        /// 1.0 = no reduction; 0.0 = free.
        /// With Config.EquipmentCostReductionPerPurchase = r,
        /// multiplier = (1 - r) ^ purchaseCount.
        /// </summary>
        public static float GetRebateMultiplier(WItem item)
        {
            if (Instance == null || item == null)
                return 1f;

            // Clamp r to [0,1].
            float r = Config.EquipmentCostReductionPerPurchase;
            if (r <= 0f)
                return 1f;
            if (r > 1f)
                r = 1f;

            if (
                !Instance._itemPurchaseCounts.TryGetValue(item.StringId, out var count)
                || count <= 0
            )
                return 1f;

            // Per-purchase factor (e.g. r=0.1 -> 0.9 per purchase).
            float perPurchaseFactor = 1f - r;

            // r=1 => perPurchaseFactor=0 => free after first purchase.
            if (perPurchaseFactor <= 0f)
                return 0f;

            double factor = Math.Pow(perPurchaseFactor, count);
            if (factor < 0.0)
                factor = 0.0;

            return (float)factor;
        }

        /// <summary>
        /// Returns true if this item has any rebate applied (i.e. has been purchased before
        /// by any troop).
        /// </summary>
        public static bool HasRebate(WItem item)
        {
            if (Config.EquipmentCostReductionPerPurchase <= 0f)
                return false; // feature disabled

            if (Instance == null || item == null)
                return false;

            return Instance._itemPurchaseCounts.ContainsKey(item.StringId);
        }

        /// <summary>
        /// Register that some copies of item have just been purchased (by any troop).
        /// </summary>
        public static void RegisterPurchase(WItem item, int copies)
        {
            if (Instance == null || item == null)
                return;
            if (copies <= 0)
                return;

            float r = Config.EquipmentCostReductionPerPurchase;
            if (r <= 0f)
                return; // feature disabled; don't bloat saves

            if (!Instance._itemPurchaseCounts.TryGetValue(item.StringId, out var count))
                count = 0;

            long newCount = (long)count + copies;
            if (newCount > int.MaxValue)
                newCount = int.MaxValue;

            Instance._itemPurchaseCounts[item.StringId] = (int)newCount;
        }

        /// <summary>
        /// Optional: clear all rebates for an item (if you ever need a cheat / reset).
        /// </summary>
        public static void ClearItem(WItem item)
        {
            if (Instance == null || item == null)
                return;

            Instance._itemPurchaseCounts.Remove(item.StringId);
        }
    }
}
