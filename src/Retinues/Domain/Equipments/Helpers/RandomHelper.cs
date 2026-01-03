using System;
using System.Collections.Generic;
using System.Linq;
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
    public static class RandomHelper
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
        /// Parameters:
        /// - min/max tier: inclusive item tier bounds
        /// - acceptable cultures: whitelist; null/empty means "any culture"
        /// - accept neutral culture: also allow items with Culture == null (neutral)
        /// - no item chance: per-slot percent chance to leave the slot empty (defaults to 0% if missing)
        /// </summary>
        public static MEquipment CreateRandomEquipment(
            WCharacter owner,
            bool civilian,
            int minTier,
            int maxTier,
            IEnumerable<WCulture> acceptableCultures = null,
            bool acceptNeutralCulture = false,
            Dictionary<EquipmentIndex, float> noItemChanceBySlotPercent = null,
            bool requireSkillForItem = true
        )
        {
            if (owner?.Base == null)
                return null;

            NormalizeTierRange(ref minTier, ref maxTier);

            var me = MEquipment.Create(owner, civilian: civilian, source: null);

            // Fast culture whitelist
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

            // Armor (per-slot empty chance; default 0%)
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
                    slot,
                    civilian,
                    minTier,
                    maxTier,
                    cultureIds,
                    acceptNeutralCulture,
                    requireSkillForItem,
                    predicate: it => it.IsArmor
                );

                me.Set(slot, item);
            }

            // Mounts (battle only) with strict horse/harness rule:
            // - Horse can be absent via per-slot chance (default 0%).
            // - If horse is present, harness is attempted 100% of the time.
            // - If horse is absent, harness is always null.
            if (!civilian)
                FillMounts(
                    owner,
                    me,
                    minTier,
                    maxTier,
                    cultureIds,
                    acceptNeutralCulture,
                    noItemChanceBySlotPercent,
                    requireSkillForItem
                );

            // Weapons
            if (civilian)
            {
                // Civilian: keep it simple (no presets).
                FillCivilianWeapons(
                    owner,
                    me,
                    minTier,
                    maxTier,
                    cultureIds,
                    acceptNeutralCulture,
                    requireSkillForItem
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
                    requireSkillForItem
                );
            }

            return me;
        }

        /// <summary>
        /// Gets a random equippable item for a given slot matching constraints.
        /// Returns null if no suitable item exists.
        /// </summary>
        public static WItem GetRandomItemForSlot(
            WCharacter owner,
            EquipmentIndex slot,
            bool civilian,
            int minTier,
            int maxTier,
            IEnumerable<WCulture> acceptableCultures,
            bool acceptNeutralCulture,
            bool requireSkillForItem = true
        )
        {
            HashSet<string> cultureIds = null;
            if (acceptableCultures != null)
            {
                cultureIds = [.. acceptableCultures.Where(c => c != null).Select(c => c.StringId)];

                if (cultureIds.Count == 0)
                    cultureIds = null;
            }

            return GetRandomItemForSlotFiltered(
                owner,
                slot,
                civilian,
                minTier,
                maxTier,
                cultureIds,
                acceptNeutralCulture,
                requireSkillForItem,
                predicate: null
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
            bool requireSkillForItem
        )
        {
            // Decide horse first
            if (ShouldSkipSlot(EquipmentIndex.Horse, noItemChanceBySlotPercent))
            {
                me.Set(EquipmentIndex.Horse, null);
                me.Set(EquipmentIndex.HorseHarness, null);
                return;
            }

            var horse = GetRandomItemForSlotFiltered(
                owner,
                EquipmentIndex.Horse,
                civilian: false,
                minTier,
                maxTier,
                cultureIds,
                acceptNeutralCulture,
                requireSkillForItem,
                predicate: it => it.IsHorse
            );

            if (horse == null)
            {
                me.Set(EquipmentIndex.Horse, null);
                me.Set(EquipmentIndex.HorseHarness, null);
                return;
            }

            me.Set(EquipmentIndex.Horse, horse);

            // Horse present => harness attempted 100% of the time (no skip roll).
            var harness = GetRandomItemForSlotFiltered(
                owner,
                EquipmentIndex.HorseHarness,
                civilian: false,
                minTier,
                maxTier,
                cultureIds,
                acceptNeutralCulture,
                requireSkillForItem,
                predicate: it => it.IsHorseHarness && it.IsCompatibleWith(horse)
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
            bool requireSkillForItem
        )
        {
            // Clear weapons first
            for (int i = 0; i < WeaponSlots.Length; i++)
                me.Set(WeaponSlots[i], null);

            // Try a few times to find a satisfiable preset with available items.
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
                        requireSkillForItem
                    )
                )
                    return;

                // reset and retry
                for (int i = 0; i < WeaponSlots.Length; i++)
                    me.Set(WeaponSlots[i], null);
            }

            // Fallback: just ensure at least one weapon.
            var forced = PickByTypes(
                owner,
                slot: EquipmentIndex.Weapon0,
                civilian: false,
                minTier,
                maxTier,
                cultureIds,
                acceptNeutralCulture,
                requireSkillForItem,
                allowed: RangedWeaponTypes
                    .Concat(OneHandedTypes)
                    .Concat(TwoHandedTypes)
                    .Concat(PolearmTypes)
                    .Concat(ThrownTypes)
                    .ToArray()
            );

            me.Set(EquipmentIndex.Weapon0, forced);
        }

        private static bool TryApplyWeaponPreset(
            WCharacter owner,
            MEquipment me,
            WeaponPreset preset,
            int minTier,
            int maxTier,
            HashSet<string> cultureIds,
            bool acceptNeutralCulture,
            bool requireSkillForItem
        )
        {
            switch (preset)
            {
                case WeaponPreset.ShieldAndOneHanded:
                {
                    var oneHand = PickByTypes(
                        owner,
                        EquipmentIndex.Weapon0,
                        false,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        OneHandedTypes
                    );
                    var shield = PickByTypes(
                        owner,
                        EquipmentIndex.Weapon1,
                        false,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        ShieldTypes
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
                        EquipmentIndex.Weapon0,
                        false,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        TwoHandedTypes
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
                        EquipmentIndex.Weapon0,
                        false,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        PolearmTypes
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
                        EquipmentIndex.Weapon0,
                        false,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        RangedWeaponTypes
                    );
                    if (ranged == null)
                        return false;

                    var ammoType = GetAmmoTypeForRanged(ranged.Type);
                    if (ammoType == null)
                        return false;

                    var ammo = PickByTypes(
                        owner,
                        EquipmentIndex.Weapon1,
                        false,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        [ammoType.Value]
                    );
                    var oneHand = PickByTypes(
                        owner,
                        EquipmentIndex.Weapon2,
                        false,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        OneHandedTypes
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
                            EquipmentIndex.Weapon3,
                            false,
                            minTier,
                            maxTier,
                            cultureIds,
                            acceptNeutralCulture,
                            requireSkillForItem,
                            ShieldTypes
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
                        EquipmentIndex.Weapon0,
                        false,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        ThrownTypes
                    );
                    var oneHand = PickByTypes(
                        owner,
                        EquipmentIndex.Weapon1,
                        false,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        OneHandedTypes
                    );
                    var shield = PickByTypes(
                        owner,
                        EquipmentIndex.Weapon2,
                        false,
                        minTier,
                        maxTier,
                        cultureIds,
                        acceptNeutralCulture,
                        requireSkillForItem,
                        ShieldTypes
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
            // Weights are implicit by duplication. Tweak later if needed.
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
            bool requireSkillForItem
        )
        {
            // Civilian gear can be unarmed: try a one-hander, otherwise keep empty.
            var oneHand = PickByTypes(
                owner,
                EquipmentIndex.Weapon0,
                true,
                minTier,
                maxTier,
                cultureIds,
                acceptNeutralCulture,
                requireSkillForItem,
                OneHandedTypes
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
            EquipmentIndex slot,
            bool civilian,
            int minTier,
            int maxTier,
            HashSet<string> cultureIds,
            bool acceptNeutralCulture,
            bool requireSkillForItem,
            ItemObject.ItemTypeEnum[] allowed
        )
        {
            HashSet<ItemObject.ItemTypeEnum> set = [.. allowed];

            return GetRandomItemForSlotFiltered(
                owner,
                slot,
                civilian,
                minTier,
                maxTier,
                cultureIds,
                acceptNeutralCulture,
                requireSkillForItem,
                predicate: it => set.Contains(it.Type)
            );
        }

        private static WItem GetRandomItemForSlotFiltered(
            WCharacter owner,
            EquipmentIndex slot,
            bool civilian,
            int minTier,
            int maxTier,
            HashSet<string> cultureIds,
            bool acceptNeutralCulture,
            bool requireSkillForItem,
            Func<WItem, bool> predicate
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

                // Skip female-coded items for male troops
                if (
                    !owner.IsFemale
                    && InvalidTokensForMale.Any(token => it.StringId.ToLower().Contains(token))
                )
                    continue;

                // Civilian sets: must be civilian items. Battle sets: can use anything.
                if (civilian && !it.IsCivilian)
                    continue;

                var tier = MBMath.ClampInt(it.Tier + 1, 1, 6);
                if (tier < minTier || tier > maxTier)
                    continue;

                // Culture gating + "prefer cultured, fallback neutral"
                var c = it.Culture; // null => neutral
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
                    // No culture whitelist: allow any cultured item; neutral only if requested.
                    if (isNeutral && !acceptNeutralCulture)
                        continue;
                }

                if (requireSkillForItem && !it.IsEquippableByCharacter(owner))
                    continue;

                if (predicate != null && !predicate(it))
                    continue;

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

            // Prefer cultured items if any exist; only fallback to neutral if none exist.
            var pool = (cultured != null && cultured.Count > 0) ? cultured : neutral;

            if (pool == null || pool.Count == 0)
                return null;

            return pool[MBRandom.RandomInt(pool.Count)];
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Utils                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void NormalizeTierRange(ref int minTier, ref int maxTier)
        {
            minTier = MBMath.ClampInt(minTier, 1, 6);
            maxTier = MBMath.ClampInt(maxTier, 1, 6);

            if (maxTier < minTier)
            {
                (maxTier, minTier) = (minTier, maxTier);
            }
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

            // MBRandom.RandomFloat is [0..1]
            var roll = MBRandom.RandomFloat * 100f;
            return roll < chance;
        }
    }
}
