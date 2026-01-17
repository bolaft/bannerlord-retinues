using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Helpers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Runtime;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Domain.Equipments.Services.Random
{
    /// <summary>
    /// Helpers for selecting random items based on criteria.
    /// Used by RandomEquipmentHelper.
    /// </summary>
    [SafeClass]
    public static class ItemRandomizer
    {
        /// <summary>
        /// Tokens that make an item unsuitable for male characters.
        /// </summary>
        public static string[] InvalidTokensForMale = ["skirt", "dress", "lady", "moccasin"];

        /// <summary>
        /// Compose two predicates with logical AND.
        /// </summary>
        public static Func<WItem, bool> And(Func<WItem, bool> a, Func<WItem, bool> b)
        {
            if (a == null)
                return b;
            if (b == null)
                return a;
            return it => a(it) && b(it);
        }

        /// <summary>
        /// Picks an item similar to the given source item, trying to match category and tier.
        /// Special rule: for ammo and thrown weapons, we DO NOT use category matching (too broad),
        /// we always match by ItemType to avoid "thrown -> bow" and "bow -> sling stones" style drift.
        /// </summary>
        public static WItem PickLikeSource(
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

            int desiredTier = GetDesiredTier(sourceItem, owner);

            var desiredCategoryId = sourceItem.Category?.StringId;
            var desiredType = sourceItem.Type;

            bool forceTypeMatch = sourceItem.IsAmmo || sourceItem.IsThrownWeapon;
            bool hasCategory = !forceTypeMatch && !string.IsNullOrEmpty(desiredCategoryId);

            // 1) strict: category (or type) exact tier
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
                matchCategory: hasCategory,
                reuseContext,
                preferUnlocked,
                extraPredicate
            );

            if (picked != null)
                return picked;

            // 1b) if category match failed, try type match at exact tier
            if (hasCategory)
            {
                picked = TryPickBySpec(
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
                    matchCategory: false,
                    reuseContext,
                    preferUnlocked,
                    extraPredicate
                );

                if (picked != null)
                    return picked;
            }

            // 2) fallback: lower tiers
            int lower = MBMath.ClampInt(desiredTier - 1, 1, 6);
            lower = Math.Min(lower, MBMath.ClampInt(owner.Tier, 1, 6));

            picked = TryPickBySpec(
                owner,
                currentEquipment,
                slot,
                civilian,
                0,
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
                matchCategory: hasCategory,
                reuseContext,
                preferUnlocked,
                extraPredicate
            );

            if (picked != null)
                return picked;

            // 2b) if still nothing and we had a category, try type match for lower tiers
            if (hasCategory)
            {
                return TryPickBySpec(
                    owner,
                    currentEquipment,
                    slot,
                    civilian,
                    0,
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
                    matchCategory: false,
                    reuseContext,
                    preferUnlocked,
                    extraPredicate
                );
            }

            return null;
        }

        /// <summary>
        /// Tries to pick an item matching the given specifications.
        /// </summary>
        public static WItem TryPickBySpec(
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
                            currentEquipment.Get,
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

            var pool =
                (culturedUnlocked != null && culturedUnlocked.Count > 0) ? culturedUnlocked
                : (culturedLocked != null && culturedLocked.Count > 0) ? culturedLocked
                : (neutralUnlocked != null && neutralUnlocked.Count > 0) ? neutralUnlocked
                : (neutralLocked != null && neutralLocked.Count > 0) ? neutralLocked
                : null;

            if (pool == null || pool.Count == 0)
                return null;

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
        /// Picks ammo matching a required ammo type, preferring same tier then falling back.
        /// </summary>
        public static WItem PickAmmoMatchingType(
            WCharacter owner,
            MEquipment currentEquipment,
            EquipmentIndex slot,
            bool civilian,
            WItem sourceAmmo,
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
            ItemObject.ItemTypeEnum requiredAmmoType
        )
        {
            if (sourceAmmo?.Base == null)
                return null;

            int desiredTier = GetDesiredTier(sourceAmmo, owner);

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
                desiredCategoryId: null,
                desiredType: requiredAmmoType,
                matchCategory: false,
                reuseContext,
                preferUnlocked,
                extraPredicate: it => it.IsAmmo
            );

            if (picked != null)
                return picked;

            int lower = MBMath.ClampInt(desiredTier - 1, 1, 6);
            return TryPickBySpec(
                owner,
                currentEquipment,
                slot,
                civilian,
                0,
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
                desiredCategoryId: null,
                desiredType: requiredAmmoType,
                matchCategory: false,
                reuseContext,
                preferUnlocked,
                extraPredicate: it => it.IsAmmo
            );
        }

        /// <summary>
        /// Returns a random item for the given slot within the specified tiers and culture constraints.
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

            // No limits here (returns a slot item without equipment context).
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
        /// Computes the desired tier for a given item capped by owner tier and settings.
        /// </summary>
        private static int GetDesiredTier(WItem item, WCharacter owner)
        {
            int desiredTier = MBMath.ClampInt(item.Tier, 1, 6);
            int ownerTierCap = MBMath.ClampInt(owner.Tier, 1, 6);

            // Ensure we don't pick items above owner's tier.
            desiredTier = Math.Min(desiredTier, ownerTierCap);

            // Ensure we don't pick above max configured tier.
            desiredTier = Math.Min(desiredTier, Settings.RandomItemMaxTier);

            return desiredTier;
        }

        /// <summary>
        /// Random item selection with filtering and equipment-context limits.
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

                if (currentEquipment != null && (weightLimitActive || valueLimitActive))
                {
                    bool fits = EquipmentLimitsHelper.FitsLimitsAfterSet(
                        currentEquipment.Get,
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

        /// <summary>
        /// Normalize min/max tier bounds to the valid range [1..6].
        /// </summary>
        private static void NormalizeTierRange(ref int minTier, ref int maxTier)
        {
            minTier = MBMath.ClampInt(minTier, 1, 6);
            maxTier = MBMath.ClampInt(maxTier, 1, 6);

            if (maxTier < minTier)
                (maxTier, minTier) = (minTier, maxTier);
        }

        /// <summary>
        /// Chooses the best item from a pool using comparison chevrons, tier and value.
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

                if (pos > neg)
                {
                    best = cand;
                    continue;
                }

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
    }
}
