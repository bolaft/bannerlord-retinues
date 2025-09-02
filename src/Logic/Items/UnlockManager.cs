using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem;
using CustomClanTroops.Wrappers.Campaign;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.Logic.Items
{
    public static class UnlockManager
    {
        // Pre-initialized with empty lists.
        public static readonly Dictionary<ItemObject.ItemTypeEnum, List<ItemObject>> Unlocked = InitEmptyUnlocked();

        // Unlock an item
        public static void Unlock(ItemObject item)
        {
            if (item == null) return;
            if (!IsSupportedType(item.ItemType)) return;
            if (!IsValidEquipment(item)) return;

            var list = Unlocked[item.ItemType];
            if (!list.Contains(item))
                list.Add(item);
        }

        // Unlock all items used by a troop across all its equipment sets (battle/civilian/etc).
        public static void UnlockFromTroop(CharacterWrapper troop)
        {
            foreach (var eq in troop.Equipments)
            {
                foreach (var item in EnumerateItems(eq))
                    Unlock(item);
            }
        }

        // Unlock all items that belong to the given culture (excludes neutral items with null culture).
        public static void UnlockFromCulture(CultureWrapper culture)
        {
            var allItems = MBObjectManager.Instance.GetObjectTypeList<ItemObject>();
            foreach (var io in allItems)
            {
                if (io?.Culture?.StringId == culture.StringId && IsSupportedType(io.ItemType))
                    Unlock(io);
            }
        }

        // Get all unlocked items valid for the given slot. (Weapons return the union of all weapon types.)
        public static List<ItemObject> GetUnlockedItems(EquipmentIndex slot)
        {
            var result = new List<ItemObject>();
            foreach (var t in TypesPerSlot(slot))
            {
                // No unlock restrictions: return all valid items
                if (Config.AllEquipmentUnlocked)
                {
                    // get all items of this type
                    var allItems = MBObjectManager.Instance.GetObjectTypeList<ItemObject>();
                    foreach (var item in allItems)
                    {
                        if (item?.ItemType == t && IsValidEquipment(item))
                            result.Add(item);
                    }
                }
                // Only return unlocked items
                else
                {
                    if (Unlocked.TryGetValue(t, out var list) && list.Count > 0)
                        result.AddRange(list);
                }
            }
            
            // Deduplicate by reference (same ItemObject) just in case
            return result.Distinct().ToList();
        }

        // Mapping: slot -> item types
        public static IEnumerable<ItemObject.ItemTypeEnum> TypesPerSlot(EquipmentIndex slot) => slot switch
        {
            EquipmentIndex.Head => new[] { ItemObject.ItemTypeEnum.HeadArmor },
            EquipmentIndex.Cape => new[] { ItemObject.ItemTypeEnum.Cape },
            EquipmentIndex.Body => new[] { ItemObject.ItemTypeEnum.BodyArmor },
            EquipmentIndex.Gloves => new[] { ItemObject.ItemTypeEnum.HandArmor },
            EquipmentIndex.Leg => new[] { ItemObject.ItemTypeEnum.LegArmor },
            EquipmentIndex.Horse => new[] { ItemObject.ItemTypeEnum.Horse },
            EquipmentIndex.HorseHarness => new[] { ItemObject.ItemTypeEnum.HorseHarness },

            // All weapon slots accept these (plus shields)
            EquipmentIndex.WeaponItemBeginSlot or EquipmentIndex.Weapon1
            or EquipmentIndex.Weapon2 or EquipmentIndex.Weapon3
                => new[]
                {
                    ItemObject.ItemTypeEnum.OneHandedWeapon,
                    ItemObject.ItemTypeEnum.TwoHandedWeapon,
                    ItemObject.ItemTypeEnum.Polearm,
                    ItemObject.ItemTypeEnum.Bow,
                    ItemObject.ItemTypeEnum.Crossbow,
                    ItemObject.ItemTypeEnum.Thrown,
                    ItemObject.ItemTypeEnum.Shield,
                    ItemObject.ItemTypeEnum.Bolts,
                    ItemObject.ItemTypeEnum.Arrows,
                    ItemObject.ItemTypeEnum.Pistol,
                    ItemObject.ItemTypeEnum.Musket,
                    ItemObject.ItemTypeEnum.Bullets
                },

            _ => Array.Empty<ItemObject.ItemTypeEnum>()
        };

        // --------------------------------------------------------------------
        // Internals
        // --------------------------------------------------------------------
        private static Dictionary<ItemObject.ItemTypeEnum, List<ItemObject>> InitEmptyUnlocked()
        {
            var dict = new Dictionary<ItemObject.ItemTypeEnum, List<ItemObject>>();

            // Collect all types that can appear in any slot
            var allTypes = new HashSet<ItemObject.ItemTypeEnum>();
            foreach (EquipmentIndex slot in Enum.GetValues(typeof(EquipmentIndex)))
                foreach (var t in TypesPerSlot(slot))
                    allTypes.Add(t);

            foreach (var t in allTypes)
                dict[t] = new List<ItemObject>();

            return dict;
        }

        private static bool IsSupportedType(ItemObject.ItemTypeEnum t)
        {
            // Keys we pre-initialize define support
            return Unlocked.ContainsKey(t);
        }

        /// Returns true if the item is a valid piece of troop equipment (ignores pack animals, boulders, etc.).
        public static bool IsValidEquipment(ItemObject item)
        {
            if (item == null)
                return false;

            if (item.IsUniqueItem)
                return false;

            switch (item.ItemType)
            {
                // Armor pieces
                case ItemObject.ItemTypeEnum.HeadArmor:
                case ItemObject.ItemTypeEnum.Cape:
                case ItemObject.ItemTypeEnum.BodyArmor:
                case ItemObject.ItemTypeEnum.HandArmor:
                case ItemObject.ItemTypeEnum.LegArmor:
                    return item.ArmorComponent != null;

                // Mounts (exclude pack animals/livestock)
                case ItemObject.ItemTypeEnum.Horse:
                    var hc = item.HorseComponent;
                    if (hc == null) return false;
                    if (hc.IsLiveStock) return false;
                    if (item.ItemCategory == DefaultItemCategories.PackAnimal) return false;
                    return true;

                // Harness
                case ItemObject.ItemTypeEnum.HorseHarness:
                    return true;

                // Weapons
                case ItemObject.ItemTypeEnum.OneHandedWeapon:
                case ItemObject.ItemTypeEnum.TwoHandedWeapon:
                case ItemObject.ItemTypeEnum.Polearm:
                case ItemObject.ItemTypeEnum.Bow:
                case ItemObject.ItemTypeEnum.Crossbow:
                case ItemObject.ItemTypeEnum.Thrown:
                case ItemObject.ItemTypeEnum.Shield:
                case ItemObject.ItemTypeEnum.Pistol:
                case ItemObject.ItemTypeEnum.Musket:
                case ItemObject.ItemTypeEnum.Bullets:
                case ItemObject.ItemTypeEnum.Bolts:
                case ItemObject.ItemTypeEnum.Arrows:
                    var wc = item.WeaponComponent;
                    if (wc == null) return false;
                    var w = wc.PrimaryWeapon;
                    if (w == null) return false;

                    var cls = w.WeaponClass.ToString();
                    if (cls == "Stone" || cls == "Boulder" || cls == "LargeStone")
                        return false;

                    return true;

                // Banner
                case ItemObject.ItemTypeEnum.Banner:
                    return true;

                // Everything else (trade goods, animals, props, food, etc.)
                default:
                    return false;
            }
        }

        private static IEnumerable<ItemObject> EnumerateItems(Equipment eq)
        {
            if (eq == null) yield break;

            // Iterate all indices we care about (same mapping we expose)
            var indices =
                new[]
                {
                    EquipmentIndex.Head, EquipmentIndex.Cape, EquipmentIndex.Body, EquipmentIndex.Gloves, EquipmentIndex.Leg,
                    EquipmentIndex.Horse, EquipmentIndex.HorseHarness,
                    EquipmentIndex.WeaponItemBeginSlot, EquipmentIndex.Weapon1, EquipmentIndex.Weapon2, EquipmentIndex.Weapon3,
                };

            foreach (var idx in indices)
            {
                ItemObject item = default;
                try { item = eq[idx].Item; } catch { /* ignore out-of-range for some builds */ }

                if (item != null)
                    yield return item;
            }
        }
    }
}
