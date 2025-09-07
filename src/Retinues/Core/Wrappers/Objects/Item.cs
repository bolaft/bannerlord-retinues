using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using Retinues.Core.Wrappers.Campaign;
using Retinues.Core.Utils;

namespace Retinues.Core.Wrappers.Objects
{
    public class WItem(ItemObject itemObject) : StringIdentifier, IWrapper
    {
        // =========================================================================
        // Base
        // =========================================================================

        private readonly ItemObject _itemObject = itemObject;

        public object Base => _itemObject;

        // =========================================================================
        // VM properties
        // =========================================================================

        public ImageIdentifierVM Image => new(_itemObject);

        // =========================================================================
        // Culture
        // =========================================================================

        public WCulture Culture
        {
            // Cast from BasicCultureObject to CultureObject
            get => new(_itemObject.Culture as CultureObject);
        }

        // =========================================================================
        // Main properties
        // =========================================================================

        public override string StringId => _itemObject.StringId;

        public string Name => _itemObject.Name.ToString();

        public int Value => _itemObject.Value;

        public ItemCategory Category => _itemObject.ItemCategory;

        public ItemObject.ItemTypeEnum Type => _itemObject.ItemType;

        public SkillObject RelevantSkill => _itemObject.RelevantSkill;

        public int Difficulty => _itemObject.Difficulty;

        public List<EquipmentIndex> Slots
        {
            get
            {
                List<EquipmentIndex> slots = [];

                void AddWeaponSlots()
                {
                    slots.Add(EquipmentIndex.WeaponItemBeginSlot);
                    slots.Add(EquipmentIndex.Weapon1);
                    slots.Add(EquipmentIndex.Weapon2);
                    slots.Add(EquipmentIndex.Weapon3);
                }

                switch (Type)
                {
                    case ItemObject.ItemTypeEnum.HeadArmor: slots.Add(EquipmentIndex.Head); break;
                    case ItemObject.ItemTypeEnum.Cape: slots.Add(EquipmentIndex.Cape); break;
                    case ItemObject.ItemTypeEnum.BodyArmor: slots.Add(EquipmentIndex.Body); break;
                    case ItemObject.ItemTypeEnum.HandArmor: slots.Add(EquipmentIndex.Gloves); break;
                    case ItemObject.ItemTypeEnum.LegArmor: slots.Add(EquipmentIndex.Leg); break;
                    case ItemObject.ItemTypeEnum.Horse: slots.Add(EquipmentIndex.Horse); break;
                    case ItemObject.ItemTypeEnum.HorseHarness: slots.Add(EquipmentIndex.HorseHarness); break;

                    case ItemObject.ItemTypeEnum.OneHandedWeapon:
                    case ItemObject.ItemTypeEnum.TwoHandedWeapon:
                    case ItemObject.ItemTypeEnum.Polearm:
                    case ItemObject.ItemTypeEnum.Bow:
                    case ItemObject.ItemTypeEnum.Crossbow:
                    case ItemObject.ItemTypeEnum.Arrows:
                    case ItemObject.ItemTypeEnum.Bolts:
                    case ItemObject.ItemTypeEnum.Thrown:
                    case ItemObject.ItemTypeEnum.Shield:
                    case ItemObject.ItemTypeEnum.Pistol:
                    case ItemObject.ItemTypeEnum.Musket:
                    case ItemObject.ItemTypeEnum.Bullets:
                        AddWeaponSlots();
                        break;
                }

                return slots;
            }
        }

        // =========================================================================
        // Components
        // =========================================================================

        public ItemComponent ItemComponent => _itemObject.ItemComponent;

        public ArmorComponent ArmorComponent => _itemObject.ArmorComponent;

        public HorseComponent HorseComponent => _itemObject.HorseComponent;

        public WeaponComponent WeaponComponent => _itemObject.WeaponComponent;

        public WeaponComponentData PrimaryWeapon => _itemObject.PrimaryWeapon;

        // =========================================================================
        // Flags
        // =========================================================================

        public bool IsUniqueItem => _itemObject.IsUniqueItem;

        public bool IsArmor => ArmorComponent != null;

        public bool IsHorse => HorseComponent != null;

        public bool IsWeapon => WeaponComponent != null && PrimaryWeapon != null;

        public bool IsShield => PrimaryWeapon?.IsShield ?? false;

        public bool IsRangedWeapon => PrimaryWeapon?.IsRangedWeapon ?? false;

        public bool IsMeleeWeapon => PrimaryWeapon?.IsMeleeWeapon ?? false;

        public bool IsAmmo => PrimaryWeapon?.IsAmmo ?? false;

        // =========================================================================
        // Computed properties
        // =========================================================================

        public int Tier
        {
            get
            {
                var tierEnum = Enum.GetValues(_itemObject.Tier.GetType());
                int tierIndex = Array.IndexOf(tierEnum, _itemObject.Tier);
                return tierIndex + 1;
            }
        }

        public string Class
        {
            get
            {
                if (IsArmor)  // Body Armor, Gloves...
                    return Format.CamelCaseToTitle(Type.ToString());

                if (IsHorse)  // Horse, War Horse, Noble Horse..
                    return Format.CamelCaseToTitle(Category.ToString());

                if (IsWeapon)  // Mace, Bow...
                {
                    var cls = Format.CamelCaseToTitle(PrimaryWeapon.WeaponClass.ToString());

                    if (IsAmmo)
                    {
                        // Ensure the class name ends with 's'
                        if (!cls.EndsWith("s")) cls += "s";
                    }

                    return cls;
                }

                // Defaults to item type
                return Format.CamelCaseToTitle(Type.ToString());
            }
        }

        public Dictionary<string, int> Statistics
        {
            get
            {
                var stats = new Dictionary<string, int>();

                // Helper function to add statistics if the value is greater than 0
                void Add(string key, int value) { if (value > 0) stats[key] = value; }

                if (IsArmor)
                {
                    Add("Head Armor", ArmorComponent.HeadArmor);
                    Add("Body Armor", ArmorComponent.BodyArmor);
                    Add("Arm Armor", ArmorComponent.ArmArmor);
                    Add("Leg Armor", ArmorComponent.LegArmor);
                }

                if (IsHorse)
                {
                    Add("Speed", HorseComponent.Speed);
                    Add("Maneuver", HorseComponent.Maneuver);
                    Add("Charge", HorseComponent.ChargeDamage);
                    Add("Hit Points", HorseComponent.HitPoints);
                }

                if (IsShield)
                {
                    Add("Speed", PrimaryWeapon.Handling);
                    Add("Hit Points", PrimaryWeapon.MaxDataValue);
                }

                if (IsRangedWeapon)
                {
                    Add("Missile Speed", PrimaryWeapon.MissileSpeed);
                    Add("Damage", PrimaryWeapon.MissileDamage);
                    Add("Accuracy", PrimaryWeapon.Accuracy);
                }

                if (IsMeleeWeapon)
                {
                    if (PrimaryWeapon.SwingDamage > 0 && PrimaryWeapon.SwingSpeed > 0)
                    {
                        Add("Swing Damage", PrimaryWeapon.SwingDamage);
                        Add("Swing Speed", PrimaryWeapon.SwingSpeed);
                    }
                    if (PrimaryWeapon.ThrustDamage > 0 && PrimaryWeapon.ThrustSpeed > 0)
                    {
                        Add("Thrust Damage", PrimaryWeapon.ThrustDamage);
                        Add("Thrust Speed", PrimaryWeapon.ThrustSpeed);
                    }
                    Add("Length", PrimaryWeapon.WeaponLength);
                    Add("Handling", PrimaryWeapon.Handling);
                }

                if (IsAmmo)
                {
                    Add("Damage", PrimaryWeapon.ThrustDamage);
                    Add("Stack Size", PrimaryWeapon.MaxDataValue);
                }

                if (RelevantSkill != null && Difficulty > 0)
                    Add($"{RelevantSkill.Name}", Difficulty);

                return stats;
            }
        }


        // =========================================================================
        // Computed Flags
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

        public static HashSet<WItem> UnlockedItems { get; } = [];

        public bool IsUnlocked => UnlockedItems.Contains(this);

        public void Unlock()
        {
            if (!IsUnlocked)
                UnlockedItems.Add(this);
        }

        public void Lock()
        {
            if (IsUnlocked)
                UnlockedItems.Remove(this);
        }

        // =========================================================================
        // Stocks
        // =========================================================================

        public static Dictionary<WItem, int> Stocks { get; } = [];

        public int GetStock()
        {
            if (Stocks.TryGetValue(this, out int count))
                return count;
            return 0;
        }

        public static void SetStock(WItem item, int count)
        {
            if (count <= 0)
                Stocks.Remove(item);
            else
                Stocks[item] = count;
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