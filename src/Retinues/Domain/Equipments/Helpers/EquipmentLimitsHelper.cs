using System;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Framework.Runtime;
using TaleWorlds.Core;

namespace Retinues.Domain.Equipments.Helpers
{
    /// <summary>
    /// Shared equipment limit logic (tier limits + totals simulation + fit checks).
    /// Does not depend on editor state; callers provide current slot items.
    /// </summary>
    [SafeClass]
    public static class EquipmentLimitsHelper
    {
        /// <summary>
        /// Aggregated totals for an equipment snapshot (weight and value).
        /// </summary>
        public struct Totals
        {
            public float Weight;
            public int Value;
        }

        // Weight limits are in "kg" and match MEquipment.Weight semantics:
        // horse and harness weight are ignored.
        private static readonly float[] WeightLimitByTier =
        [
            8f, // T0
            8f, // T1
            16f, // T2
            24f, // T3
            48f, // T4
            54f, // T5
            62f, // T6
            62f, // T7
        ];

        // Value limits are in denars and match MEquipment.Value semantics:
        // all slots (including horse/harness) contribute to value.
        private static readonly int[] ValueLimitByTier =
        [
            6000, // T0
            6000, // T1
            12000, // T2
            48000, // T3
            320000, // T4
            640000, // T5
            720000, // T6
            720000, // T7
        ];

        /// <summary>
        /// Clamps the provided tier into the supported range [0..7].
        /// </summary>
        public static int ClampTier(int tier)
        {
            if (tier < 0)
                return 0;
            if (tier > 7)
                return 7;
            return tier;
        }

        /// <summary>
        /// Returns the weight limit (kg) for the given tier and multiplier.
        /// </summary>
        public static float GetWeightLimit(int tier, float multiplier)
        {
            tier = ClampTier(tier);

            float baseLimit = WeightLimitByTier[tier];
            float limit = baseLimit * multiplier;

            return Math.Max(limit, 0f);
        }

        /// <summary>
        /// Returns the value limit (denars) for the given tier and multiplier.
        /// </summary>
        public static int GetValueLimit(int tier, double multiplier)
        {
            tier = ClampTier(tier);

            int baseLimit = ValueLimitByTier[tier];
            int limit = (int)Math.Round(baseLimit * multiplier, MidpointRounding.AwayFromZero);

            return Math.Max(limit, 0);
        }

        /// <summary>
        /// Computes totals for an equipment snapshot, optionally overriding one slot.
        /// Simulates horse/harness incompatibility; value includes horse/harness while weight ignores them.
        /// </summary>
        public static Totals GetTotals(
            Func<EquipmentIndex, WItem> getItem,
            EquipmentIndex? overrideSlot = null,
            WItem overrideItem = null
        )
        {
            if (getItem == null)
                return default;

            var oSlot = overrideSlot;

            var finalHorse =
                oSlot == EquipmentIndex.Horse ? overrideItem : getItem(EquipmentIndex.Horse);

            var finalHarness =
                oSlot == EquipmentIndex.HorseHarness
                    ? overrideItem
                    : getItem(EquipmentIndex.HorseHarness);

            if (finalHorse == null)
                finalHarness = null;
            else if (finalHarness != null && !finalHorse.IsCompatibleWith(finalHarness))
                finalHarness = null;

            float weight = 0f;
            int value = 0;

            int slotCount = (int)EquipmentIndex.NumEquipmentSetSlots;
            for (int i = 0; i < slotCount; i++)
            {
                var idx = (EquipmentIndex)i;

                WItem it;
                if (idx == EquipmentIndex.Horse)
                    it = finalHorse;
                else if (idx == EquipmentIndex.HorseHarness)
                    it = finalHarness;
                else if (oSlot.HasValue && idx == oSlot.Value)
                    it = overrideItem;
                else
                    it = getItem(idx);

                if (it == null)
                    continue;

                value += it.Value;

                if (idx != EquipmentIndex.Horse && idx != EquipmentIndex.HorseHarness)
                    weight += it.Weight;
            }

            return new Totals { Weight = weight, Value = value };
        }

        /// <summary>
        /// Checks whether the next totals fit the weight limit, handling "allow non-increasing when already over" semantics.
        /// </summary>
        public static bool FitsWeight(
            Totals current,
            Totals next,
            float limit,
            bool allowNonIncreasingWhenOver
        )
        {
            if (limit <= 0f)
                return true;

            // If already over the limit (settings changed), allow only non-increasing changes.
            if (allowNonIncreasingWhenOver && current.Weight > limit)
                return next.Weight <= current.Weight + 0.0001f;

            return next.Weight <= limit + 0.0001f;
        }

        /// <summary>
        /// Checks whether the next totals fit the value limit, handling "allow non-increasing when already over" semantics.
        /// </summary>
        public static bool FitsValue(
            Totals current,
            Totals next,
            int limit,
            bool allowNonIncreasingWhenOver
        )
        {
            if (limit <= 0)
                return true;

            if (allowNonIncreasingWhenOver && current.Value > limit)
                return next.Value <= current.Value;

            return next.Value <= limit;
        }

        /// <summary>
        /// Convenience check: verifies both weight and value limits if a slot were set to the given item.
        /// </summary>
        public static bool FitsLimitsAfterSet(
            Func<EquipmentIndex, WItem> getItem,
            EquipmentIndex slot,
            WItem newItem,
            bool weightLimitActive,
            float weightLimit,
            bool valueLimitActive,
            int valueLimit,
            bool allowNonIncreasingWhenOver = false
        )
        {
            if (!weightLimitActive && !valueLimitActive)
                return true;

            Totals current = GetTotals(getItem);
            Totals next = GetTotals(getItem, slot, newItem);

            if (
                weightLimitActive
                && !FitsWeight(current, next, weightLimit, allowNonIncreasingWhenOver)
            )
                return false;

            if (
                valueLimitActive
                && !FitsValue(current, next, valueLimit, allowNonIncreasingWhenOver)
            )
                return false;

            return true;
        }
    }
}
