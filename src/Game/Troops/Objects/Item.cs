using System.Collections.Generic;
using TaleWorlds.Core;
using CustomClanTroops.Wrappers.Objects;

namespace CustomClanTroops.Game.Troops.Objects
{
    public class TroopItem(ItemObject item) : ItemWrapper(item)
    {
        // =========================================================================
        // Flags
        // =========================================================================

        public bool IsEquippable
        {
            get
            {
                // No unique items
                if (IsUniqueItem)
                    return false;

                switch (Type)
                {
                    // Armor pieces
                    case ItemObject.ItemTypeEnum.HeadArmor:
                    case ItemObject.ItemTypeEnum.Cape:
                    case ItemObject.ItemTypeEnum.BodyArmor:
                    case ItemObject.ItemTypeEnum.HandArmor:
                    case ItemObject.ItemTypeEnum.LegArmor:
                        return ArmorComponent != null;

                    // Mounts (exclude pack animals/livestock)
                    case ItemObject.ItemTypeEnum.Horse:
                        if (HorseComponent == null) return false;
                        if (HorseComponent.IsLiveStock) return false;
                        if (Category == DefaultItemCategories.PackAnimal) return false;
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
                        if (WeaponComponent == null) return false;
                        if (PrimaryWeapon == null) return false;

                        var cls = PrimaryWeapon.WeaponClass.ToString();
                        if (cls == "Stone" || cls == "Boulder" || cls == "LargeStone")
                            return false;

                        return true;

                    // Banner
                    case ItemObject.ItemTypeEnum.Banner:
                        return false;

                    // Defaults to false
                    default:
                        return false;
                }
            }
        }

        // =========================================================================
        // Unlocks
        // =========================================================================

        public static List<TroopItem> UnlockedItems { get; } = [];

        public bool IsUnlocked()
        {
            return UnlockedItems.Contains(this);
        }

        public void Unlock()
        {
            if (!UnlockedItems.Contains(this))
                UnlockedItems.Add(this);
        }

        public void Lock()
        {
            if (UnlockedItems.Contains(this))
                UnlockedItems.Remove(this);
        }

        // =========================================================================
        // Stocks
        // =========================================================================

        public static Dictionary<TroopItem, int> Stocks { get; } = [];

        public int GetStock()
        {
            if (Stocks.TryGetValue(this, out int count))
                return count;
            return 0;
        }

        public void Stock()
        {
            if (Stocks.ContainsKey(this))
                Stocks[this]++;
            else
                Stocks[this] = 1;
        }

        public void Unstock()
        {
            if (Stocks.ContainsKey(this))
            {
                Stocks[this]--;
                if (Stocks[this] <= 0)
                    Stocks.Remove(this);
            }
        }
    }
}
