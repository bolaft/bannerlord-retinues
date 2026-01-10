using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Runtime;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Domain.Equipments.Helpers
{
    /// <summary>
    /// Helpers for selecting random items and building random equipment sets.
    /// </summary>
    [SafeClass]
    public static class RandomEquipmentHelper
    {
        public static string[] InvalidTokensForMale = ["skirt", "dress", "lady", "moccasin"];

        private static readonly EquipmentIndex[] ArmorSlots =
        [
            EquipmentIndex.Head,
            EquipmentIndex.Cape,
            EquipmentIndex.Body,
            EquipmentIndex.Gloves,
            EquipmentIndex.Leg,
        ];

        private static readonly EquipmentIndex[] WeaponSlots =
        [
            EquipmentIndex.Weapon0,
            EquipmentIndex.Weapon1,
            EquipmentIndex.Weapon2,
            EquipmentIndex.Weapon3,
        ];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Context for reusing picked items across multiple calls to CreateRandomEquipment.
        /// Helps reduce variety when creating equipment for multiple troops in a retinue tree.
        /// </summary>
        public sealed class RandomEquipmentReuseContext
        {
            private readonly Dictionary<string, string> _pickedByKey = new(StringComparer.Ordinal);

            public bool TryGet(string key, out string itemId) =>
                _pickedByKey.TryGetValue(key, out itemId);

            public void Remember(string key, WItem item)
            {
                if (string.IsNullOrEmpty(key) || item?.Base == null)
                    return;

                _pickedByKey[key] = item.StringId;
            }
        }

        /// <summary>
        /// Creates a new random equipment set for the given owner, based on the provided source equipment.
        /// The source equipment is used to determine slot categories and tiers to mirror.
        /// </summary>
        public static MEquipment CreateRandomEquipment(
            WCharacter owner,
            MEquipment source,
            bool civilian,
            IEnumerable<WCulture> acceptableCultures = null,
            bool acceptNeutralCulture = true,
            bool requireSkillForItem = true,
            Func<WItem, bool> itemFilter = null,
            bool fromStocks = false,
            bool pickBest = false,
            bool enforceLimits = false,
            RandomEquipmentReuseContext reuseContext = null,
            bool preferUnlocked = true
        )
        {
            if (owner?.Base == null)
                return null;

            if (source == null)
                throw new ArgumentNullException(
                    nameof(source),
                    "Random equipment now requires a source set to mirror slot category/tier."
                );

            var me = MEquipment.Create(owner, civilian: civilian, source: null);

            HashSet<string> cultureIds = null;
            if (acceptableCultures != null)
            {
                var list = acceptableCultures
                    .Where(c => c != null)
                    .Select(c => c.StringId)
                    .ToList();
                if (list.Count > 0)
                    cultureIds = [.. list];
            }

            bool weightLimitActive = enforceLimits && Settings.EquipmentWeightLimit;
            bool valueLimitActive = enforceLimits && Settings.EquipmentValueLimit;

            float weightLimit = weightLimitActive
                ? EquipmentLimitsHelper.GetWeightLimit(
                    owner.Tier,
                    Settings.EquipmentWeightLimitMultiplier
                )
                : 0f;

            int valueLimit = valueLimitActive
                ? EquipmentLimitsHelper.GetValueLimit(
                    owner.Tier,
                    Settings.EquipmentValueLimitMultiplier
                )
                : 0;

            Func<WItem, bool> finalFilter = itemFilter;

            if (fromStocks)
            {
                finalFilter = it =>
                {
                    if (it?.Base == null)
                        return false;

                    if (it.Stock <= 0)
                        return false;

                    int usedInSet = CountInEquipment(me, it.StringId);
                    if (usedInSet >= it.Stock)
                        return false;

                    return itemFilter == null || itemFilter(it);
                };
            }

            // Mirror armor slots (null stays null)
            for (int i = 0; i < ArmorSlots.Length; i++)
            {
                var slot = ArmorSlots[i];
                var src = source.Get(slot);

                if (src == null)
                {
                    me.Set(slot, null);
                    continue;
                }

                me.Set(
                    slot,
                    PickLikeSource(
                        owner,
                        me,
                        slot,
                        civilian,
                        src,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        finalFilter,
                        pickBest,
                        weightLimitActive,
                        weightLimit,
                        valueLimitActive,
                        valueLimit,
                        reuseContext,
                        preferUnlocked,
                        extraPredicate: it => it.IsArmor
                    )
                );
            }

            // Mirror weapon slots (null stays null)
            for (int i = 0; i < WeaponSlots.Length; i++)
            {
                var slot = WeaponSlots[i];
                var src = source.Get(slot);

                if (src == null)
                {
                    me.Set(slot, null);
                    continue;
                }

                me.Set(
                    slot,
                    PickLikeSource(
                        owner,
                        me,
                        slot,
                        civilian,
                        src,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        finalFilter,
                        pickBest,
                        weightLimitActive,
                        weightLimit,
                        valueLimitActive,
                        valueLimit,
                        reuseContext,
                        preferUnlocked,
                        extraPredicate: null
                    )
                );
            }

            // Mounts are battle-only, mirror null stays null; harness must match horse.
            if (civilian)
            {
                me.Set(EquipmentIndex.Horse, null);
                me.Set(EquipmentIndex.HorseHarness, null);
                return me;
            }

            var srcHorse = source.Get(EquipmentIndex.Horse);
            if (srcHorse == null)
            {
                me.Set(EquipmentIndex.Horse, null);
                me.Set(EquipmentIndex.HorseHarness, null);
                return me;
            }

            var horse = PickLikeSource(
                owner,
                me,
                EquipmentIndex.Horse,
                false,
                srcHorse,
                cultureIds,
                acceptNeutralCulture,
                requireSkillForItem,
                finalFilter,
                pickBest,
                weightLimitActive,
                weightLimit,
                valueLimitActive,
                valueLimit,
                reuseContext,
                preferUnlocked,
                extraPredicate: it => it.IsHorse
            );

            if (horse == null)
            {
                me.Set(EquipmentIndex.Horse, null);
                me.Set(EquipmentIndex.HorseHarness, null);
                return me;
            }

            me.Set(EquipmentIndex.Horse, horse);

            var srcHarness = source.Get(EquipmentIndex.HorseHarness);
            if (srcHarness == null)
            {
                me.Set(EquipmentIndex.HorseHarness, null);
                return me;
            }

            var harness = PickLikeSource(
                owner,
                me,
                EquipmentIndex.HorseHarness,
                false,
                srcHarness,
                cultureIds,
                acceptNeutralCulture,
                requireSkillForItem,
                finalFilter,
                pickBest,
                weightLimitActive,
                weightLimit,
                valueLimitActive,
                valueLimit,
                reuseContext,
                preferUnlocked,
                extraPredicate: it => it.IsHorseHarness && it.IsCompatibleWith(horse)
            );

            me.Set(EquipmentIndex.HorseHarness, harness);
            return me;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Picks an item similar to the given source item, trying to match category and tier.
        /// </summary>
        private static WItem PickLikeSource(
            WCharacter owner,
            MEquipment currentEquipment,
            EquipmentIndex slot,
            bool civilian,
            WItem sourceItem,
            HashSet<string> cultureIds,
            bool acceptNeutralCulture,
            bool requireSkillForItem,
            Func<WItem, bool> itemFilter,
            bool pickBest,
            bool weightLimitActive,
            float weightLimit,
            bool valueLimitActive,
            int valueLimit,
            RandomEquipmentReuseContext reuseContext,
            bool preferUnlocked,
            Func<WItem, bool> extraPredicate
        )
        {
            if (sourceItem?.Base == null)
                return null;

            int desiredTier = MBMath.ClampInt(sourceItem.Tier, 1, 6);
            var desiredCategoryId = sourceItem.Category?.StringId;
            var desiredType = sourceItem.Type;

            // Strict first: category + exact tier (or type if category is null)
            var picked = TryPickBySpec(
                owner,
                currentEquipment,
                slot,
                civilian,
                desiredTier,
                desiredTier,
                cultureIds,
                acceptNeutralCulture,
                requireSkillForItem,
                itemFilter,
                pickBest,
                weightLimitActive,
                weightLimit,
                valueLimitActive,
                valueLimit,
                desiredCategoryId,
                desiredType,
                matchCategory: !string.IsNullOrEmpty(desiredCategoryId),
                reuseContext,
                preferUnlocked,
                extraPredicate
            );

            if (picked != null)
                return picked;

            // Small fallback: - 1 tier
            int lower = MBMath.ClampInt(desiredTier - 1, 1, 6);

            return TryPickBySpec(
                owner,
                currentEquipment,
                slot,
                civilian,
                lower,
                lower,
                cultureIds,
                acceptNeutralCulture,
                requireSkillForItem,
                itemFilter,
                pickBest,
                weightLimitActive,
                weightLimit,
                valueLimitActive,
                valueLimit,
                desiredCategoryId,
                desiredType,
                matchCategory: !string.IsNullOrEmpty(desiredCategoryId),
                reuseContext,
                preferUnlocked,
                extraPredicate
            );
        }

        /// <summary>
        /// Tries to pick an item matching the given specifications.
        /// </summary>
        private static WItem TryPickBySpec(
            WCharacter owner,
            MEquipment currentEquipment,
            EquipmentIndex slot,
            bool civilian,
            int minTier,
            int maxTier,
            HashSet<string> cultureIds,
            bool acceptNeutralCulture,
            bool requireSkillForItem,
            Func<WItem, bool> itemFilter,
            bool pickBest,
            bool weightLimitActive,
            float weightLimit,
            bool valueLimitActive,
            int valueLimit,
            string desiredCategoryId,
            ItemObject.ItemTypeEnum desiredType,
            bool matchCategory,
            RandomEquipmentReuseContext reuseContext,
            bool preferUnlocked,
            Func<WItem, bool> extraPredicate
        )
        {
            var items = WItem.GetEquipmentsForSlot(slot);
            if (items == null || items.Count == 0)
                return null;

            NormalizeTierRange(ref minTier, ref maxTier);

            List<WItem> culturedUnlocked = null,
                culturedLocked = null,
                neutralUnlocked = null,
                neutralLocked = null;

            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it?.Base == null)
                    continue;

                if (
                    !owner.IsFemale
                    && InvalidTokensForMale.Any(token => it.StringId.ToLower().Contains(token))
                )
                    continue;

                if (!it.IsEquippableInSlot(slot))
                    continue;

                if (civilian && !it.IsCivilian)
                    continue;

                int tier = MBMath.ClampInt(it.Tier, 1, 6);
                if (tier < minTier || tier > maxTier)
                    continue;

                if (matchCategory)
                {
                    if (string.IsNullOrEmpty(desiredCategoryId))
                        continue;
                    if (it.Category == null || it.Category.StringId != desiredCategoryId)
                        continue;
                }
                else
                {
                    if (it.Type != desiredType)
                        continue;
                }

                var c = it.Culture;
                bool isNeutral = c == null;

                if (cultureIds != null)
                {
                    if (!isNeutral)
                    {
                        if (!cultureIds.Contains(c.StringId))
                            continue;
                    }
                    else if (!acceptNeutralCulture)
                        continue;
                }
                else
                {
                    if (isNeutral && !acceptNeutralCulture)
                        continue;
                }

                if (requireSkillForItem && !it.IsEquippableByCharacter(owner))
                    continue;

                if (itemFilter != null && !itemFilter(it))
                    continue;

                if (extraPredicate != null && !extraPredicate(it))
                    continue;

                if (currentEquipment != null && (weightLimitActive || valueLimitActive))
                {
                    if (
                        !EquipmentLimitsHelper.FitsLimitsAfterSet(
                            idx => currentEquipment.Get(idx),
                            slot,
                            it,
                            weightLimitActive,
                            weightLimit,
                            valueLimitActive,
                            valueLimit,
                            allowNonIncreasingWhenOver: false
                        )
                    )
                    {
                        continue;
                    }
                }

                bool unlocked = !preferUnlocked || it.IsUnlocked;

                if (isNeutral)
                {
                    if (unlocked)
                        (neutralUnlocked ??= []).Add(it);
                    else
                        (neutralLocked ??= []).Add(it);
                }
                else
                {
                    if (unlocked)
                        (culturedUnlocked ??= []).Add(it);
                    else
                        (culturedLocked ??= []).Add(it);
                }
            }

            // Priority: culture first, then unlocked.
            var pool =
                (culturedUnlocked != null && culturedUnlocked.Count > 0) ? culturedUnlocked
                : (culturedLocked != null && culturedLocked.Count > 0) ? culturedLocked
                : (neutralUnlocked != null && neutralUnlocked.Count > 0) ? neutralUnlocked
                : (neutralLocked != null && neutralLocked.Count > 0) ? neutralLocked
                : null;

            if (pool == null || pool.Count == 0)
                return null;

            // Reuse to minimize variety across tree clone.
            string reuseKey = null;
            if (reuseContext != null)
            {
                reuseKey =
                    $"{(civilian ? 1 : 0)}|{(int)slot}|{minTier}-{maxTier}|{(matchCategory ? desiredCategoryId : "")}|{(matchCategory ? -1 : (int)desiredType)}|{(cultureIds == null ? "any" : string.Join(",", cultureIds.OrderBy(x => x)))}";
                if (reuseContext.TryGet(reuseKey, out var remembered))
                {
                    for (int i = 0; i < pool.Count; i++)
                    {
                        if (pool[i].StringId == remembered)
                            return pool[i];
                    }
                }
            }

            var chosen =
                (!pickBest || pool.Count == 1)
                    ? pool[MBRandom.RandomInt(pool.Count)]
                    : PickBest(pool);

            if (reuseContext != null && reuseKey != null)
                reuseContext.Remember(reuseKey, chosen);

            return chosen;
        }

        /// <summary>
        /// Normalizes the given tier range to valid values.
        /// </summary>
        private static void NormalizeTierRange(ref int minTier, ref int maxTier)
        {
            minTier = MBMath.ClampInt(minTier, 1, 6);
            maxTier = MBMath.ClampInt(maxTier, 1, 6);

            if (maxTier < minTier)
                (maxTier, minTier) = (minTier, maxTier);
        }

        /// <summary>
        /// Tries to pick the best item from the given pool.
        /// </summary>
        private static WItem PickBest(List<WItem> pool)
        {
            if (pool == null || pool.Count == 0)
                return null;

            WItem best = pool[0];

            for (int i = 1; i < pool.Count; i++)
            {
                var cand = pool[i];
                if (cand == null)
                    continue;

                cand.GetComparisonChevrons(best, out int pos, out int neg);

                // If comparable and clearly better, take it.
                if (pos > neg)
                {
                    best = cand;
                    continue;
                }

                // If not comparable (0/0) or tie-ish, break ties by tier/value.
                if (pos == neg)
                {
                    if (cand.Tier > best.Tier)
                        best = cand;
                    else if (cand.Tier == best.Tier && cand.Value > best.Value)
                        best = cand;
                }
            }

            return best;
        }

        /// <summary>
        /// Counts how many times the given item ID appears in the equipment set.
        /// </summary>
        private static int CountInEquipment(MEquipment me, string itemId)
        {
            if (me == null || string.IsNullOrEmpty(itemId))
                return 0;

            int count = 0;

            for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
            {
                var w = me.Get((EquipmentIndex)i);
                if (w != null && w.StringId == itemId)
                    count++;
            }

            return count;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Get Random Item                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets a random item for the given slot, with the specified constraints.
        /// </summary>
        public static WItem GetRandomItemForSlot(
            WCharacter owner,
            EquipmentIndex slot,
            bool civilian,
            int minTier,
            int maxTier,
            IEnumerable<WCulture> acceptableCultures,
            bool acceptNeutralCulture,
            bool requireSkillForItem = true,
            Func<WItem, bool> itemFilter = null
        )
        {
            HashSet<string> cultureIds = null;
            if (acceptableCultures != null)
            {
                cultureIds = [.. acceptableCultures.Where(c => c != null).Select(c => c.StringId)];

                if (cultureIds.Count == 0)
                    cultureIds = null;
            }

            // No limits here (this method returns a slot item without equipment context).
            return GetRandomItemForSlotFiltered(
                owner,
                currentEquipment: null,
                slot,
                civilian,
                minTier,
                maxTier,
                cultureIds,
                acceptNeutralCulture,
                requireSkillForItem,
                predicate: null,
                itemFilter: itemFilter,
                pickBest: false,
                weightLimitActive: false,
                weightLimit: 0f,
                valueLimitActive: false,
                valueLimit: 0
            );
        }

        /// <summary>
        /// Gets a random item for the given slot, with the specified constraints.
        /// </summary>
        private static WItem GetRandomItemForSlotFiltered(
            WCharacter owner,
            MEquipment currentEquipment,
            EquipmentIndex slot,
            bool civilian,
            int minTier,
            int maxTier,
            HashSet<string> cultureIds,
            bool acceptNeutralCulture,
            bool requireSkillForItem,
            Func<WItem, bool> predicate,
            Func<WItem, bool> itemFilter,
            bool pickBest,
            bool weightLimitActive,
            float weightLimit,
            bool valueLimitActive,
            int valueLimit
        )
        {
            var items = WItem.GetEquipmentsForSlot(slot);
            if (items == null || items.Count == 0)
                return null;

            List<WItem> cultured = null;
            List<WItem> neutral = null;

            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it?.Base == null)
                    continue;

                if (!it.IsEquippableInSlot(slot))
                    continue;

                if (
                    !owner.IsFemale
                    && InvalidTokensForMale.Any(token => it.StringId.ToLower().Contains(token))
                )
                    continue;

                if (civilian && !it.IsCivilian)
                    continue;

                var tier = MBMath.ClampInt(it.Tier, 0, 6);
                if (tier < minTier || tier > maxTier)
                    continue;

                var c = it.Culture;
                var isNeutral = c == null;

                if (cultureIds != null)
                {
                    if (!isNeutral)
                    {
                        if (!cultureIds.Contains(c.StringId))
                            continue;
                    }
                    else
                    {
                        if (!acceptNeutralCulture)
                            continue;
                    }
                }
                else
                {
                    if (isNeutral && !acceptNeutralCulture)
                        continue;
                }

                if (requireSkillForItem && !it.IsEquippableByCharacter(owner))
                    continue;

                if (itemFilter != null && !itemFilter(it))
                    continue;

                if (predicate != null && !predicate(it))
                    continue;

                // Limits (only if we have an equipment context).
                if (currentEquipment != null && (weightLimitActive || valueLimitActive))
                {
                    bool fits = EquipmentLimitsHelper.FitsLimitsAfterSet(
                        idx => currentEquipment.Get(idx),
                        slot,
                        it,
                        weightLimitActive,
                        weightLimit,
                        valueLimitActive,
                        valueLimit,
                        allowNonIncreasingWhenOver: false
                    );

                    if (!fits)
                        continue;
                }

                if (isNeutral)
                {
                    neutral ??= [];
                    neutral.Add(it);
                }
                else
                {
                    cultured ??= [];
                    cultured.Add(it);
                }
            }

            var pool = (cultured != null && cultured.Count > 0) ? cultured : neutral;

            if (pool == null || pool.Count == 0)
                return null;

            if (!pickBest || pool.Count == 1)
                return pool[MBRandom.RandomInt(pool.Count)];

            return PickBest(pool);
        }
    }
}
