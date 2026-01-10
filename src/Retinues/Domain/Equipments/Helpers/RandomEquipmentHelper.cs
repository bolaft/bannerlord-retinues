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
    /// Helpers for building random equipment sets.
    /// Item picking logic lives in RandomItemHelper.
    /// </summary>
    [SafeClass]
    public static class RandomEquipmentHelper
    {
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
                    RandomItemHelper.PickLikeSource(
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

            // Mirror weapon slots (two-pass):
            // - pass 1: pick non-ammo weapons, enforcing "no duplicates" (except thrown/ammo)
            //           and enforcing weapon-family constraints (thrown stays thrown).
            // - pass 2: pick ammo, constrained to match selected ranged weapon(s)
            var srcWeapons = new WItem[WeaponSlots.Length];
            for (int i = 0; i < WeaponSlots.Length; i++)
                srcWeapons[i] = source.Get(WeaponSlots[i]);

            // Pass 1: non-ammo weapons (and thrown)
            for (int i = 0; i < WeaponSlots.Length; i++)
            {
                var slot = WeaponSlots[i];
                var src = srcWeapons[i];

                if (src == null)
                {
                    me.Set(slot, null);
                    continue;
                }

                if (src.IsAmmo)
                    continue;

                // Enforce: source thrown -> picked must be thrown (never bow/crossbow/etc).
                Func<WItem, bool> forceFamily = null;
                if (src.IsThrownWeapon)
                    forceFamily = it => it != null && it.IsThrownWeapon;

                Func<WItem, bool> noDup = null;
                if (PreventDuplicateInWeaponSlots(src))
                {
                    noDup = it =>
                    {
                        if (it == null)
                            return false;

                        if (IsDuplicateAllowedInWeaponSlots(it))
                            return true;

                        if (!PreventDuplicateInWeaponSlots(it))
                            return true;

                        return !HasItemInWeaponSlots(me, it.StringId);
                    };
                }

                var predicate = RandomItemHelper.And(forceFamily, noDup);

                var picked = RandomItemHelper.PickLikeSource(
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
                    extraPredicate: predicate
                );

                // If uniqueness made us fail, retry once without uniqueness (but KEEP family constraint).
                if (picked == null && noDup != null)
                {
                    picked = RandomItemHelper.PickLikeSource(
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
                        extraPredicate: forceFamily
                    );
                }

                me.Set(slot, picked);
            }

            // Pass 2: ammo (match to ranged weapons chosen above)
            var allowedAmmoTypes = GetAllowedAmmoTypes(me);

            for (int i = 0; i < WeaponSlots.Length; i++)
            {
                var slot = WeaponSlots[i];
                var src = srcWeapons[i];

                if (src == null)
                    continue;

                if (!src.IsAmmo)
                    continue;

                if (allowedAmmoTypes == null || allowedAmmoTypes.Count == 0)
                {
                    me.Set(slot, null);
                    continue;
                }

                var requiredAmmoType = allowedAmmoTypes.Contains(src.Type)
                    ? src.Type
                    : allowedAmmoTypes.First();

                var ammo = RandomItemHelper.PickAmmoMatchingType(
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
                    requiredAmmoType
                );

                me.Set(slot, ammo);
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

            var horse = RandomItemHelper.PickLikeSource(
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

            var harness = RandomItemHelper.PickLikeSource(
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

        private static bool HasItemInWeaponSlots(MEquipment me, string itemId)
        {
            if (me == null || string.IsNullOrEmpty(itemId))
                return false;

            for (int i = 0; i < WeaponSlots.Length; i++)
            {
                var w = me.Get(WeaponSlots[i]);
                if (w != null && w.StringId == itemId)
                    return true;
            }

            return false;
        }

        private static bool IsDuplicateAllowedInWeaponSlots(WItem it) =>
            it != null && (it.IsAmmo || it.IsThrownWeapon);

        private static bool PreventDuplicateInWeaponSlots(WItem it) =>
            it != null && it.IsWeapon && !it.IsAmmo && !it.IsThrownWeapon;

        private static ItemObject.ItemTypeEnum? GetRequiredAmmoType(WItem rangedWeapon)
        {
            if (rangedWeapon == null || !rangedWeapon.IsRangedWeapon)
                return null;

            switch (rangedWeapon.Type)
            {
                case ItemObject.ItemTypeEnum.Bow:
                    return ItemObject.ItemTypeEnum.Arrows;
                case ItemObject.ItemTypeEnum.Crossbow:
                    return ItemObject.ItemTypeEnum.Bolts;
                case ItemObject.ItemTypeEnum.Pistol:
                case ItemObject.ItemTypeEnum.Musket:
                    return ItemObject.ItemTypeEnum.Bullets;
#if BL13
                case ItemObject.ItemTypeEnum.Sling:
                    return ItemObject.ItemTypeEnum.SlingStones;
#endif
                default:
                    return null;
            }
        }

        private static HashSet<ItemObject.ItemTypeEnum> GetAllowedAmmoTypes(MEquipment me)
        {
            HashSet<ItemObject.ItemTypeEnum> set = null;

            if (me == null)
                return set;

            for (int i = 0; i < WeaponSlots.Length; i++)
            {
                var w = me.Get(WeaponSlots[i]);
                if (w == null || !w.IsRangedWeapon)
                    continue;

                var req = GetRequiredAmmoType(w);
                if (req == null)
                    continue;

                set ??= [];
                set.Add(req.Value);
            }

            return set;
        }
    }
}
