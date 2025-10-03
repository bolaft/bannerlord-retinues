using System;
using System.Collections.Generic;
using Retinues.Core.Features.Stocks.Behaviors;
using Retinues.Core.Features.Unlocks.Behaviors;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

namespace Retinues.Core.Game.Wrappers
{
    [SafeClass(SwallowByDefault = false)]
    public class WItem(ItemObject itemObject) : StringIdentifier
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Base                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly ItemObject _itemObject = itemObject;

        public ItemObject Base => _itemObject;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      VM properties                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public ImageIdentifierVM Image => new(_itemObject);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Culture                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCulture Culture
        {
            get => new(_itemObject.Culture as CultureObject);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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
                    case ItemObject.ItemTypeEnum.HeadArmor:
                        slots.Add(EquipmentIndex.Head);
                        break;
                    case ItemObject.ItemTypeEnum.Cape:
                        slots.Add(EquipmentIndex.Cape);
                        break;
                    case ItemObject.ItemTypeEnum.BodyArmor:
                        slots.Add(EquipmentIndex.Body);
                        break;
                    case ItemObject.ItemTypeEnum.HandArmor:
                        slots.Add(EquipmentIndex.Gloves);
                        break;
                    case ItemObject.ItemTypeEnum.LegArmor:
                        slots.Add(EquipmentIndex.Leg);
                        break;
                    case ItemObject.ItemTypeEnum.Horse:
                        slots.Add(EquipmentIndex.Horse);
                        break;
                    case ItemObject.ItemTypeEnum.HorseHarness:
                        slots.Add(EquipmentIndex.HorseHarness);
                        break;

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Components                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public ItemComponent ItemComponent => _itemObject.ItemComponent;

        public ArmorComponent ArmorComponent => _itemObject.ArmorComponent;

        public HorseComponent HorseComponent => _itemObject.HorseComponent;

        public WeaponComponent WeaponComponent => _itemObject.WeaponComponent;

        public WeaponComponentData PrimaryWeapon => _itemObject.PrimaryWeapon;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsUniqueItem => _itemObject.IsUniqueItem;

        public bool IsCrafted => _itemObject.IsCraftedByPlayer;

        public bool IsArmor =>
            ArmorComponent != null && ItemObject.ItemTypeEnum.HorseHarness != Type;

        public bool IsHorse => HorseComponent != null;

        public bool IsWeapon => WeaponComponent != null && PrimaryWeapon != null;

        public bool IsShield => PrimaryWeapon?.IsShield ?? false;

        public bool IsRangedWeapon => PrimaryWeapon?.IsRangedWeapon ?? false;

        public bool IsMeleeWeapon => PrimaryWeapon?.IsMeleeWeapon ?? false;

        public bool IsAmmo => PrimaryWeapon?.IsAmmo ?? false;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Computed properties                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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
                if (IsArmor) // Body Armor, Gloves...
                    return Format.CamelCaseToTitle(Type.ToString());

                if (IsHorse) // Horse, War Horse, Noble Horse..
                    return Format.CamelCaseToTitle(Category.ToString());

                if (IsWeapon) // Mace, Bow...
                {
                    var cls = Format.CamelCaseToTitle(PrimaryWeapon.WeaponClass.ToString());

                    if (IsAmmo)
                    {
                        // Ensure the class name ends with 's'
                        if (!cls.EndsWith("s"))
                            cls += "s";
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
                void Add(string key, int value)
                {
                    if (value > 0)
                        stats[key] = value;
                }

                if (IsArmor)
                {
                    Add(L.S("item_head_armor", "Head Armor"), ArmorComponent.HeadArmor);
                    Add(L.S("item_body_armor", "Body Armor"), ArmorComponent.BodyArmor);
                    Add(L.S("item_arm_armor", "Arm Armor"), ArmorComponent.ArmArmor);
                    Add(L.S("item_leg_armor", "Leg Armor"), ArmorComponent.LegArmor);
                }

                if (IsHorse)
                {
                    Add(L.S("item_horse_speed", "Speed"), HorseComponent.Speed);
                    Add(L.S("item_horse_maneuver", "Maneuver"), HorseComponent.Maneuver);
                    Add(L.S("item_horse_charge", "Charge"), HorseComponent.ChargeDamage);
                    Add(L.S("item_horse_hit_points", "Hit Points"), HorseComponent.HitPoints);
                }

                if (IsShield)
                {
                    Add(L.S("item_shield_speed", "Speed"), PrimaryWeapon.Handling);
                    Add(L.S("item_shield_hit_points", "Hit Points"), PrimaryWeapon.MaxDataValue);
                }

                if (IsRangedWeapon)
                {
                    Add(
                        L.S("item_ranged_missile_speed", "Missile Speed"),
                        PrimaryWeapon.MissileSpeed
                    );
                    Add(L.S("item_ranged_damage", "Damage"), PrimaryWeapon.MissileDamage);
                    Add(L.S("item_ranged_accuracy", "Accuracy"), PrimaryWeapon.Accuracy);
                }

                if (IsMeleeWeapon)
                {
                    if (PrimaryWeapon.SwingDamage > 0 && PrimaryWeapon.SwingSpeed > 0)
                    {
                        Add(
                            L.S("item_melee_swing_damage", "Swing Damage"),
                            PrimaryWeapon.SwingDamage
                        );
                        Add(L.S("item_melee_swing_speed", "Swing Speed"), PrimaryWeapon.SwingSpeed);
                    }
                    if (PrimaryWeapon.ThrustDamage > 0 && PrimaryWeapon.ThrustSpeed > 0)
                    {
                        Add(
                            L.S("item_melee_thrust_damage", "Thrust Damage"),
                            PrimaryWeapon.ThrustDamage
                        );
                        Add(
                            L.S("item_melee_thrust_speed", "Thrust Speed"),
                            PrimaryWeapon.ThrustSpeed
                        );
                    }
                    Add(L.S("item_melee_length", "Length"), PrimaryWeapon.WeaponLength);
                    Add(L.S("item_melee_handling", "Handling"), PrimaryWeapon.Handling);
                }

                if (IsAmmo)
                {
                    Add(L.S("item_ammo_damage", "Damage"), PrimaryWeapon.ThrustDamage);
                    Add(L.S("item_ammo_stack_size", "Stack Size"), PrimaryWeapon.MaxDataValue);
                }

                if (RelevantSkill != null && Difficulty > 0)
                    Add($"{RelevantSkill.Name}", Difficulty);

                return stats;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Unlocks                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsUnlocked => UnlocksBehavior.IsUnlocked(StringId);

        public void Unlock() => UnlocksBehavior.Unlock(Base);

        public void Lock() => UnlocksBehavior.Lock(Base);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Stocks                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsStocked => StocksBehavior.HasStock(StringId);

        public static void SetStock(WItem item, int count) =>
            StocksBehavior.Set(item?.StringId, count);

        public int GetStock() => StocksBehavior.Get(StringId);

        public void Stock() => StocksBehavior.Add(StringId, 1);

        public void Unstock() => StocksBehavior.Add(StringId, -1);
    }
}
