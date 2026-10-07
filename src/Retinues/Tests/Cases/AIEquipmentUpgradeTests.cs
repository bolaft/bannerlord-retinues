using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Retinues.Behaviors.Retinues;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Tests.Cases
{
    public static class AIEquipmentUpgradeTests
    {
        private static readonly HashSet<string> NothingCarried = new(StringComparer.Ordinal);

        private static WItem Weapon(string id, ItemObject.ItemTypeEnum type, WeaponClass weaponClass, int tier = 4)
        {
            var item = (ItemObject)FormatterServices.GetUninitializedObject(typeof(ItemObject));
            item.StringId = id;
            item.Type = type;
            var flags = type switch
            {
                ItemObject.ItemTypeEnum.Arrows or ItemObject.ItemTypeEnum.Bolts => WeaponFlags.Consumable,
                ItemObject.ItemTypeEnum.Bow or ItemObject.ItemTypeEnum.Crossbow => WeaponFlags.RangedWeapon,
                ItemObject.ItemTypeEnum.Thrown => WeaponFlags.RangedWeapon | WeaponFlags.Consumable,
                ItemObject.ItemTypeEnum.Shield => WeaponFlags.HasHitPoints | WeaponFlags.CanBlockRanged,
                _ => WeaponFlags.MeleeWeapon,
            };
            item.AddWeapon(new WeaponComponentData(item, weaponClass, flags), null);
            ItemTiers.Set(item, tier);
            var result = new WItem(item);
            Tests.AssertEqual(tier, result.Tier, "Fixture uses the real engine tier calculation.");
            Tests.AssertTrue(result.IsValidEquipment, "Fixture is valid troop equipment.");
            return result;
        }

        private static WItem Armor(string id, ItemObject.ItemTypeEnum type)
        {
            var item = (ItemObject)FormatterServices.GetUninitializedObject(typeof(ItemObject));
            item.StringId = id;
            item.Type = type;
            AccessTools.PropertySetter(typeof(ItemObject), "ItemComponent").Invoke(item,
                new[] { FormatterServices.GetUninitializedObject(typeof(ArmorComponent)) });
            ItemTiers.Set(item, 2);
            return new WItem(item);
        }

        private static WCharacter Owner()
        {
            var character = (CharacterObject)FormatterServices.GetUninitializedObject(typeof(CharacterObject));
            character.StringId = ""; // Never register fixtures in the campaign or wrapper cache.
            return new WCharacter(character);
        }

        [GameTest("AIEquipmentUpgradeKeepsWeaponAndAmmoFamilies", "equipment", RequiresCampaign = false)]
        public static void AIEquipmentUpgradeKeepsWeaponAndAmmoFamilies()
        {
            using var tiers = new ItemTiers();
            var families = new[]
            {
                (ItemObject.ItemTypeEnum.Arrows, WeaponClass.Arrow),
                (ItemObject.ItemTypeEnum.Bolts, WeaponClass.Bolt),
                (ItemObject.ItemTypeEnum.Bow, WeaponClass.Bow),
                (ItemObject.ItemTypeEnum.Crossbow, WeaponClass.Crossbow),
                (ItemObject.ItemTypeEnum.Shield, WeaponClass.SmallShield),
                (ItemObject.ItemTypeEnum.OneHandedWeapon, WeaponClass.OneHandedSword),
                (ItemObject.ItemTypeEnum.TwoHandedWeapon, WeaponClass.TwoHandedSword),
                (ItemObject.ItemTypeEnum.Polearm, WeaponClass.OneHandedPolearm),
                (ItemObject.ItemTypeEnum.Thrown, WeaponClass.Javelin),
            };
            foreach (var source in families)
            {
                var current = Weapon("current_" + source.Item1, source.Item1, source.Item2);
                foreach (var target in families)
                {
                    var next = Weapon("next_" + target.Item1, target.Item1, target.Item2, 5);
                    Tests.AssertEqual(source.Item1 == target.Item1,
                        AIClanRetinuesBehavior.IsCompatibleEquipmentUpgrade(EquipmentIndex.Weapon1, current, next, NothingCarried),
                        "An upgrade preserves the loadout role: " + source.Item1 + " -> " + target.Item1);
                }
            }
        }

        [GameTest("AIDailyPickerNeverReplacesArrowsWithShield", "equipment", RequiresCampaign = false)]
        public static void AIDailyPickerNeverReplacesArrowsWithShield()
        {
            using var tiers = new ItemTiers();
            var arrows = Weapon("bodkin_arrows_b", ItemObject.ItemTypeEnum.Arrows, WeaponClass.Arrow);
            var shield = Weapon("highland_round_shield", ItemObject.ItemTypeEnum.Shield, WeaponClass.SmallShield, 5);
            var betterArrows = Weapon("better_arrows", ItemObject.ItemTypeEnum.Arrows, WeaponClass.Arrow, 5);
            var owner = Owner();
            var pool = new List<WItem> { shield };
            using var items = new ItemPool(EquipmentIndex.Weapon1, pool);

            // Exercise the actual daily picker and its culture fallback: no matching
            // higher-tier ammo means keeping the arrows, even with a better shield available.
            Tests.AssertTrue(AIClanRetinuesBehavior.PickEquipmentUpgrade(owner, EquipmentIndex.Weapon1,
                arrows, NothingCarried, null) == null);
            pool.Add(betterArrows);
            Tests.AssertTrue(ReferenceEquals(betterArrows,
                AIClanRetinuesBehavior.PickEquipmentUpgrade(owner, EquipmentIndex.Weapon1, arrows, NothingCarried, null)));
            Tests.AssertTrue(AIClanRetinuesBehavior.PickEquipmentUpgrade(owner, EquipmentIndex.Weapon1,
                null, NothingCarried, null) == null, "Empty weapon slots remain empty.");
            Tests.AssertTrue(AIClanRetinuesBehavior.PickEquipmentUpgrade(owner, EquipmentIndex.Weapon1,
                betterArrows, NothingCarried, null) == null, "Same-tier gear is not an upgrade.");
        }

        [GameTest("AIEquipmentUpgradePreservesSpareAmmunition", "equipment", RequiresCampaign = false)]
        public static void AIEquipmentUpgradePreservesSpareAmmunition()
        {
            using var tiers = new ItemTiers();
            var arrows = Weapon("arrows", ItemObject.ItemTypeEnum.Arrows, WeaponClass.Arrow);
            var betterArrows = Weapon("better_arrows", ItemObject.ItemTypeEnum.Arrows, WeaponClass.Arrow, 5);
            var throwing = Weapon("javelins", ItemObject.ItemTypeEnum.Thrown, WeaponClass.Javelin);
            var sword = Weapon("sword", ItemObject.ItemTypeEnum.OneHandedWeapon, WeaponClass.OneHandedSword);
            var carried = new HashSet<string> { betterArrows.StringId, throwing.StringId, sword.StringId };
            Tests.AssertTrue(AIClanRetinuesBehavior.IsCompatibleEquipmentUpgrade(EquipmentIndex.Weapon1, arrows, betterArrows, carried),
                "Two quivers can both upgrade to the same ammunition.");
            Tests.AssertTrue(AIClanRetinuesBehavior.IsCompatibleEquipmentUpgrade(EquipmentIndex.Weapon1, throwing, throwing, carried),
                "Multiple throwing stacks are valid loadouts.");
            Tests.AssertFalse(AIClanRetinuesBehavior.IsCompatibleEquipmentUpgrade(EquipmentIndex.Weapon1, sword, sword, carried),
                "The duplicate melee-weapon guard still applies.");
        }

        [GameTest("AIDailyPickerRejectsPlayerCraftedWeapons", "equipment", RequiresCampaign = false)]
        public static void AIDailyPickerRejectsPlayerCraftedWeapons()
        {
            using var tiers = new ItemTiers();
            var sword = Weapon("sword", ItemObject.ItemTypeEnum.OneHandedWeapon, WeaponClass.OneHandedSword);
            var crafted = Weapon("crafted_item_upgrade_probe", ItemObject.ItemTypeEnum.OneHandedWeapon, WeaponClass.OneHandedSword, 5);
            AccessTools.PropertySetter(typeof(ItemObject), "WeaponDesign").Invoke(crafted.Base,
                new[] { FormatterServices.GetUninitializedObject(typeof(WeaponDesign)) });
            Tests.AssertTrue(crafted.IsCrafted);
            using var items = new ItemPool(EquipmentIndex.Weapon0, new List<WItem> { crafted });
            Tests.AssertTrue(AIClanRetinuesBehavior.PickEquipmentUpgrade(Owner(), EquipmentIndex.Weapon0,
                sword, NothingCarried, null) == null, "The daily picker cannot take a player's crafted weapon.");
        }

        [GameTest("AIEquipmentUpgradeStillFillsEmptyArmorSlots", "equipment", RequiresCampaign = false)]
        public static void AIEquipmentUpgradeStillFillsEmptyArmorSlots()
        {
            using var tiers = new ItemTiers();
            var armor = Armor("body_armor", ItemObject.ItemTypeEnum.BodyArmor);
            var helmet = Armor("helmet", ItemObject.ItemTypeEnum.HeadArmor);
            using var items = new ItemPool(EquipmentIndex.Body, new List<WItem> { helmet, armor });
            Tests.AssertTrue(ReferenceEquals(armor,
                AIClanRetinuesBehavior.PickEquipmentUpgrade(Owner(), EquipmentIndex.Body, null, NothingCarried, null)),
                "The empty-weapon restriction does not stop armor improvements.");
        }

        private sealed class ItemTiers : IDisposable
        {
#if BL12
            // 1.2 computes Tierf through Game.Current and has no TierfOverride. Supply
            // Tierf only for our unregistered fixtures; keep the real Tier getter and picker.
            private static Dictionary<ItemObject, float> _values = new();
            private static readonly MBFastRandom _headlessRandom = new();
            private readonly Dictionary<ItemObject, float> _previous = _values;
            private readonly Harmony _harmony = new("retinues.tests.item-tiers." + Guid.NewGuid().ToString("N"));

            public ItemTiers()
            {
                _harmony.Patch(AccessTools.PropertyGetter(typeof(ItemObject), "Tierf"),
                    prefix: new HarmonyMethod(typeof(ItemTiers), nameof(ReadTier)));
                if (Game.Current == null)
                {
                    // The 1.2 random source also lives on Game.Current. Provide a seeded
                    // managed source in the headless host without replacing the picker.
                    _headlessRandom.SetSeed(12345, 67890);
                    _harmony.Patch(AccessTools.PropertyGetter(typeof(MBRandom), "Random"),
                        prefix: new HarmonyMethod(typeof(ItemTiers), nameof(ReadRandom)));
                }
                _values = new Dictionary<ItemObject, float>();
            }

            private static bool ReadTier(ItemObject __instance, ref float __result) =>
                !_values.TryGetValue(__instance, out __result);

            private static bool ReadRandom(ref MBFastRandom __result)
            {
                __result = _headlessRandom;
                return false;
            }

            public static void Set(ItemObject item, int tier) => _values[item] = tier;

            public void Dispose()
            {
                _values = _previous;
                _harmony.UnpatchAll(_harmony.Id);
            }
#else
            public static void Set(ItemObject item, int tier) =>
                AccessTools.PropertySetter(typeof(ItemObject), "TierfOverride")
                    .Invoke(item, new object[] { (float)tier + 1f });

            public void Dispose() { }
#endif
        }

        private sealed class ItemPool : IDisposable
        {
            private readonly FieldInfo _field = AccessTools.Field(typeof(WItem), "_equipmentsBySlot");
            private readonly object _previous;

            public ItemPool(EquipmentIndex slot, List<WItem> items)
            {
                _previous = _field.GetValue(null);
                _field.SetValue(null, new Dictionary<EquipmentIndex, List<WItem>> { [slot] = items });
            }

            public void Dispose() => _field.SetValue(null, _previous);
        }
    }
}
