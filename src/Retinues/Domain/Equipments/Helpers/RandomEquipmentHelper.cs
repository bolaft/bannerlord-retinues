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
        public static string[] InvalidTokensForMale = ["skirt", "dress", "lady"];

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

        private static readonly ItemObject.ItemTypeEnum[] OneHandedTypes =
        [
            ItemObject.ItemTypeEnum.OneHandedWeapon,
        ];

        private static readonly ItemObject.ItemTypeEnum[] TwoHandedTypes =
        [
            ItemObject.ItemTypeEnum.TwoHandedWeapon,
        ];

        private static readonly ItemObject.ItemTypeEnum[] PolearmTypes =
        [
            ItemObject.ItemTypeEnum.Polearm,
        ];

        private static readonly ItemObject.ItemTypeEnum[] RangedWeaponTypes =
        [
            ItemObject.ItemTypeEnum.Bow,
            ItemObject.ItemTypeEnum.Crossbow,
            ItemObject.ItemTypeEnum.Pistol,
            ItemObject.ItemTypeEnum.Musket,
#if BL13
            ItemObject.ItemTypeEnum.Sling,
#endif
        ];

        private static readonly ItemObject.ItemTypeEnum[] ThrownTypes =
        [
            ItemObject.ItemTypeEnum.Thrown,
        ];

        private static readonly ItemObject.ItemTypeEnum[] ShieldTypes =
        [
            ItemObject.ItemTypeEnum.Shield,
        ];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns a random equipment (battle or civilian) for the given owner.
        /// itemFilter: optional global filter applied to every candidate (ex: unlocked-only).
        /// fromStocks: if true, only pick items with Stock > 0 (and do not exceed stock count within this equipment).
        /// pickBest: if true, pick the best candidate from the computed pool instead of random.
        /// enforceLimits: if true, applies current tier-based weight/value limits while generating.
        /// </summary>
        public static MEquipment CreateRandomEquipment(
            WCharacter owner,
            bool civilian,
            int minTier,
            int maxTier,
            IEnumerable<WCulture> acceptableCultures = null,
            bool acceptNeutralCulture = false,
            Dictionary<EquipmentIndex, float> noItemChanceBySlotPercent = null,
            bool requireSkillForItem = true,
            Func<WItem, bool> itemFilter = null,
            bool fromStocks = false,
            bool pickBest = false,
            bool enforceLimits = false
        )
        {
            if (owner?.Base == null)
                return null;

            NormalizeTierRange(ref minTier, ref maxTier);

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

            // Limits
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
                // Enforce: stock-only, and never pick more copies inside a single equipment than current stock.
                // This matches the roster's conceptual logic (max-per-equipment).
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

            for (int i = 0; i < ArmorSlots.Length; i++)
            {
                var slot = ArmorSlots[i];

                if (ShouldSkipSlot(slot, noItemChanceBySlotPercent))
                {
                    me.Set(slot, null);
                    continue;
                }

                var item = GetRandomItemForSlotFiltered(
                    owner,
                    me,
                    slot,
                    civilian,
                    minTier,
                    maxTier,
                    cultureIds,
                    acceptNeutralCulture,
                    requireSkillForItem,
                    predicate: it => it.IsArmor,
                    itemFilter: finalFilter,
                    pickBest: pickBest,
                    weightLimitActive: weightLimitActive,
                    weightLimit: weightLimit,
                    valueLimitActive: valueLimitActive,
                    valueLimit: valueLimit
                );

                me.Set(slot, item);
            }

            if (!civilian)
                FillMounts(
                    owner,
                    me,
                    minTier,
                    maxTier,
                    cultureIds,
                    acceptNeutralCulture,
                    noItemChanceBySlotPercent,
                    requireSkillForItem,
                    finalFilter,
                    pickBest,
                    weightLimitActive,
                    weightLimit,
                    valueLimitActive,
                    valueLimit
                );

            if (civilian)
            {
                FillCivilianWeapons(
                    owner,
                    me,
                    minTier,
                    maxTier,
                    cultureIds,
                    acceptNeutralCulture,
                    requireSkillForItem,
                    finalFilter,
                    pickBest,
                    weightLimitActive,
                    weightLimit,
                    valueLimitActive,
                    valueLimit
                );
            }
            else
            {
                FillBattleWeaponsWithPresets(
                    owner,
                    me,
                    minTier,
                    maxTier,
                    cultureIds,
                    acceptNeutralCulture,
                    requireSkillForItem,
                    finalFilter,
                    pickBest,
                    weightLimitActive,
                    weightLimit,
                    valueLimitActive,
                    valueLimit
                );
            }

            return me;
        }

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Mounts                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void FillMounts(
            WCharacter owner,
            MEquipment me,
            int minTier,
            int maxTier,
            HashSet<string> cultureIds,
            bool acceptNeutralCulture,
            Dictionary<EquipmentIndex, float> noItemChanceBySlotPercent,
            bool requireSkillForItem,
            Func<WItem, bool> itemFilter,
            bool pickBest,
            bool weightLimitActive,
            float weightLimit,
            bool valueLimitActive,
            int valueLimit
        )
        {
            if (ShouldSkipSlot(EquipmentIndex.Horse, noItemChanceBySlotPercent))
            {
                me.Set(EquipmentIndex.Horse, null);
                me.Set(EquipmentIndex.HorseHarness, null);
                return;
            }

            var horse = GetRandomItemForSlotFiltered(
                owner,
                me,
                EquipmentIndex.Horse,
                civilian: false,
                minTier,
                maxTier,
                cultureIds,
                acceptNeutralCulture,
                requireSkillForItem,
                predicate: it => it.IsHorse,
                itemFilter: itemFilter,
                pickBest: pickBest,
                weightLimitActive: weightLimitActive,
                weightLimit: weightLimit,
                valueLimitActive: valueLimitActive,
                valueLimit: valueLimit
            );

            if (horse == null)
            {
                me.Set(EquipmentIndex.Horse, null);
                me.Set(EquipmentIndex.HorseHarness, null);
                return;
            }

            me.Set(EquipmentIndex.Horse, horse);

            var harness = GetRandomItemForSlotFiltered(
                owner,
                me,
                EquipmentIndex.HorseHarness,
                civilian: false,
                minTier,
                maxTier,
                cultureIds,
                acceptNeutralCulture,
                requireSkillForItem,
                predicate: it => it.IsHorseHarness && it.IsCompatibleWith(horse),
                itemFilter: itemFilter,
                pickBest: pickBest,
                weightLimitActive: weightLimitActive,
                weightLimit: weightLimit,
                valueLimitActive: valueLimitActive,
                valueLimit: valueLimit
            );

            me.Set(EquipmentIndex.HorseHarness, harness);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Weapons                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private enum WeaponPreset
        {
            ShieldAndOneHanded,
            TwoHanded,
            Polearm,
            RangedAmmoOneHanded,
            RangedAmmoShieldOneHanded,
            ThrownShieldOneHanded,
        }

        private static void FillBattleWeaponsWithPresets(
            WCharacter owner,
            MEquipment me,
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
            int valueLimit
        )
        {
            for (int i = 0; i < WeaponSlots.Length; i++)
                me.Set(WeaponSlots[i], null);

            for (int attempt = 0; attempt < 8; attempt++)
            {
                var preset = PickWeaponPreset();

                if (
                    TryApplyWeaponPreset(
                        owner,
                        me,
                        preset,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        itemFilter,
                        pickBest,
                        weightLimitActive,
                        weightLimit,
                        valueLimitActive,
                        valueLimit
                    )
                )
                    return;

                for (int i = 0; i < WeaponSlots.Length; i++)
                    me.Set(WeaponSlots[i], null);
            }

            // Fallback: avoid ranged-without-ammo.
            var meleeOrThrown = PickByTypes(
                owner,
                me,
                EquipmentIndex.Weapon0,
                civilian: false,
                minTier,
                maxTier,
                cultureIds,
                acceptNeutralCulture,
                requireSkillForItem,
                itemFilter,
                OneHandedTypes
                    .Concat(TwoHandedTypes)
                    .Concat(PolearmTypes)
                    .Concat(ThrownTypes)
                    .ToArray(),
                pickBest,
                weightLimitActive,
                weightLimit,
                valueLimitActive,
                valueLimit
            );

            if (meleeOrThrown != null)
            {
                me.Set(EquipmentIndex.Weapon0, meleeOrThrown);
                return;
            }

            // Last resort: try ranged + ammo (2 slots).
            var ranged = PickByTypes(
                owner,
                me,
                EquipmentIndex.Weapon0,
                civilian: false,
                minTier,
                maxTier,
                cultureIds,
                acceptNeutralCulture,
                requireSkillForItem,
                itemFilter,
                RangedWeaponTypes,
                pickBest,
                weightLimitActive,
                weightLimit,
                valueLimitActive,
                valueLimit
            );

            if (ranged == null)
                return;

            var ammoType = GetAmmoTypeForRanged(ranged.Type);
            if (ammoType == null)
                return;

            var ammo = PickByTypes(
                owner,
                me,
                EquipmentIndex.Weapon1,
                civilian: false,
                minTier,
                maxTier,
                cultureIds,
                acceptNeutralCulture,
                requireSkillForItem,
                itemFilter,
                [ammoType.Value],
                pickBest,
                weightLimitActive,
                weightLimit,
                valueLimitActive,
                valueLimit
            );

            if (ammo == null)
                return;

            me.Set(EquipmentIndex.Weapon0, ranged);
            me.Set(EquipmentIndex.Weapon1, ammo);
        }

        private static bool TryApplyWeaponPreset(
            WCharacter owner,
            MEquipment me,
            WeaponPreset preset,
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
            int valueLimit
        )
        {
            switch (preset)
            {
                case WeaponPreset.ShieldAndOneHanded:
                {
                    var oneHand = PickByTypes(
                        owner,
                        me,
                        EquipmentIndex.Weapon0,
                        false,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        itemFilter,
                        OneHandedTypes,
                        pickBest,
                        weightLimitActive,
                        weightLimit,
                        valueLimitActive,
                        valueLimit
                    );
                    var shield = PickByTypes(
                        owner,
                        me,
                        EquipmentIndex.Weapon1,
                        false,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        itemFilter,
                        ShieldTypes,
                        pickBest,
                        weightLimitActive,
                        weightLimit,
                        valueLimitActive,
                        valueLimit
                    );

                    if (oneHand == null || shield == null)
                        return false;

                    me.Set(EquipmentIndex.Weapon0, oneHand);
                    me.Set(EquipmentIndex.Weapon1, shield);
                    return true;
                }

                case WeaponPreset.TwoHanded:
                {
                    var twoHand = PickByTypes(
                        owner,
                        me,
                        EquipmentIndex.Weapon0,
                        false,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        itemFilter,
                        TwoHandedTypes,
                        pickBest,
                        weightLimitActive,
                        weightLimit,
                        valueLimitActive,
                        valueLimit
                    );
                    if (twoHand == null)
                        return false;

                    me.Set(EquipmentIndex.Weapon0, twoHand);
                    return true;
                }

                case WeaponPreset.Polearm:
                {
                    var polearm = PickByTypes(
                        owner,
                        me,
                        EquipmentIndex.Weapon0,
                        false,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        itemFilter,
                        PolearmTypes,
                        pickBest,
                        weightLimitActive,
                        weightLimit,
                        valueLimitActive,
                        valueLimit
                    );
                    if (polearm == null)
                        return false;

                    me.Set(EquipmentIndex.Weapon0, polearm);
                    return true;
                }

                case WeaponPreset.RangedAmmoOneHanded:
                case WeaponPreset.RangedAmmoShieldOneHanded:
                {
                    var ranged = PickByTypes(
                        owner,
                        me,
                        EquipmentIndex.Weapon0,
                        false,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        itemFilter,
                        RangedWeaponTypes,
                        pickBest,
                        weightLimitActive,
                        weightLimit,
                        valueLimitActive,
                        valueLimit
                    );
                    if (ranged == null)
                        return false;

                    var ammoType = GetAmmoTypeForRanged(ranged.Type);
                    if (ammoType == null)
                        return false;

                    var ammo = PickByTypes(
                        owner,
                        me,
                        EquipmentIndex.Weapon1,
                        false,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        itemFilter,
                        [ammoType.Value],
                        pickBest,
                        weightLimitActive,
                        weightLimit,
                        valueLimitActive,
                        valueLimit
                    );
                    var oneHand = PickByTypes(
                        owner,
                        me,
                        EquipmentIndex.Weapon2,
                        false,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        itemFilter,
                        OneHandedTypes,
                        pickBest,
                        weightLimitActive,
                        weightLimit,
                        valueLimitActive,
                        valueLimit
                    );

                    if (ammo == null || oneHand == null)
                        return false;

                    me.Set(EquipmentIndex.Weapon0, ranged);
                    me.Set(EquipmentIndex.Weapon1, ammo);
                    me.Set(EquipmentIndex.Weapon2, oneHand);

                    if (preset == WeaponPreset.RangedAmmoShieldOneHanded)
                    {
                        var shield = PickByTypes(
                            owner,
                            me,
                            EquipmentIndex.Weapon3,
                            false,
                            minTier,
                            maxTier,
                            cultureIds,
                            acceptNeutralCulture,
                            requireSkillForItem,
                            itemFilter,
                            ShieldTypes,
                            pickBest,
                            weightLimitActive,
                            weightLimit,
                            valueLimitActive,
                            valueLimit
                        );
                        if (shield == null)
                            return false;

                        me.Set(EquipmentIndex.Weapon3, shield);
                    }

                    return true;
                }

                case WeaponPreset.ThrownShieldOneHanded:
                {
                    var thrown = PickByTypes(
                        owner,
                        me,
                        EquipmentIndex.Weapon0,
                        false,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        itemFilter,
                        ThrownTypes,
                        pickBest,
                        weightLimitActive,
                        weightLimit,
                        valueLimitActive,
                        valueLimit
                    );
                    var oneHand = PickByTypes(
                        owner,
                        me,
                        EquipmentIndex.Weapon1,
                        false,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        itemFilter,
                        OneHandedTypes,
                        pickBest,
                        weightLimitActive,
                        weightLimit,
                        valueLimitActive,
                        valueLimit
                    );
                    var shield = PickByTypes(
                        owner,
                        me,
                        EquipmentIndex.Weapon2,
                        false,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        itemFilter,
                        ShieldTypes,
                        pickBest,
                        weightLimitActive,
                        weightLimit,
                        valueLimitActive,
                        valueLimit
                    );

                    if (thrown == null || oneHand == null || shield == null)
                        return false;

                    me.Set(EquipmentIndex.Weapon0, thrown);
                    me.Set(EquipmentIndex.Weapon1, oneHand);
                    me.Set(EquipmentIndex.Weapon2, shield);
                    return true;
                }
            }

            return false;
        }

        private static WeaponPreset PickWeaponPreset()
        {
            WeaponPreset[] presets =
            [
                WeaponPreset.ShieldAndOneHanded,
                WeaponPreset.ShieldAndOneHanded,
                WeaponPreset.TwoHanded,
                WeaponPreset.Polearm,
                WeaponPreset.RangedAmmoOneHanded,
                WeaponPreset.RangedAmmoOneHanded,
                WeaponPreset.RangedAmmoShieldOneHanded,
                WeaponPreset.ThrownShieldOneHanded,
            ];

            return presets[MBRandom.RandomInt(presets.Length)];
        }

        private static ItemObject.ItemTypeEnum? GetAmmoTypeForRanged(
            ItemObject.ItemTypeEnum rangedType
        )
        {
            return rangedType switch
            {
                ItemObject.ItemTypeEnum.Bow => ItemObject.ItemTypeEnum.Arrows,
                ItemObject.ItemTypeEnum.Crossbow => ItemObject.ItemTypeEnum.Bolts,
                ItemObject.ItemTypeEnum.Pistol => ItemObject.ItemTypeEnum.Bullets,
                ItemObject.ItemTypeEnum.Musket => ItemObject.ItemTypeEnum.Bullets,
#if BL13
                ItemObject.ItemTypeEnum.Sling => ItemObject.ItemTypeEnum.SlingStones,
#endif
                _ => null,
            };
        }

        private static void FillCivilianWeapons(
            WCharacter owner,
            MEquipment me,
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
            int valueLimit
        )
        {
            var oneHand = PickByTypes(
                owner,
                me,
                EquipmentIndex.Weapon0,
                true,
                minTier,
                maxTier,
                cultureIds,
                acceptNeutralCulture,
                requireSkillForItem,
                itemFilter,
                OneHandedTypes,
                pickBest,
                weightLimitActive,
                weightLimit,
                valueLimitActive,
                valueLimit
            );
            me.Set(EquipmentIndex.Weapon0, oneHand);

            me.Set(EquipmentIndex.Weapon1, null);
            me.Set(EquipmentIndex.Weapon2, null);
            me.Set(EquipmentIndex.Weapon3, null);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Picking                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static WItem PickByTypes(
            WCharacter owner,
            MEquipment me,
            EquipmentIndex slot,
            bool civilian,
            int minTier,
            int maxTier,
            HashSet<string> cultureIds,
            bool acceptNeutralCulture,
            bool requireSkillForItem,
            Func<WItem, bool> itemFilter,
            ItemObject.ItemTypeEnum[] allowed,
            bool pickBest,
            bool weightLimitActive,
            float weightLimit,
            bool valueLimitActive,
            int valueLimit
        )
        {
            HashSet<ItemObject.ItemTypeEnum> set = [.. allowed];

            return GetRandomItemForSlotFiltered(
                owner,
                me,
                slot,
                civilian,
                minTier,
                maxTier,
                cultureIds,
                acceptNeutralCulture,
                requireSkillForItem,
                predicate: it => set.Contains(it.Type),
                itemFilter: itemFilter,
                pickBest: pickBest,
                weightLimitActive: weightLimitActive,
                weightLimit: weightLimit,
                valueLimitActive: valueLimitActive,
                valueLimit: valueLimit
            );
        }

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
        //                          Utils                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void NormalizeTierRange(ref int minTier, ref int maxTier)
        {
            minTier = MBMath.ClampInt(minTier, 1, 6);
            maxTier = MBMath.ClampInt(maxTier, 1, 6);

            if (maxTier < minTier)
                (maxTier, minTier) = (minTier, maxTier);
        }

        private static bool ShouldSkipSlot(
            EquipmentIndex slot,
            Dictionary<EquipmentIndex, float> noItemChanceBySlotPercent
        )
        {
            if (noItemChanceBySlotPercent == null)
                return false;

            if (!noItemChanceBySlotPercent.TryGetValue(slot, out var chance))
                return false;

            if (chance <= 0f)
                return false;

            if (chance >= 100f)
                return true;

            var roll = MBRandom.RandomFloat * 100f;
            return roll < chance;
        }
    }
}
