using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using CustomClanTroops.Wrappers.Campaign;
using CustomClanTroops.Utils;

namespace CustomClanTroops.Wrappers.Objects
{
    public class ItemWrapper(ItemObject itemObject)
    {
        // =========================================================================
        // Base
        // =========================================================================

        private readonly ItemObject _itemObject = itemObject;

        public ItemObject Base => _itemObject;

        // =========================================================================
        // VM properties
        // =========================================================================

        public ImageIdentifierVM Image => new(_itemObject);

        // =========================================================================
        // Culture
        // =========================================================================

        public CultureWrapper Culture
        {
            // Cast from BasicCultureObject to CultureObject
            get => new(_itemObject.Culture as CultureObject);
        }

        // =========================================================================
        // Main properties
        // =========================================================================

        public string StringId => _itemObject.StringId;

        public string Name => _itemObject.Name.ToString();

        public int Value => _itemObject.Value;

        public ItemCategory Category => _itemObject.ItemCategory;

        public ItemObject.ItemTypeEnum Type => _itemObject.ItemType;

        public SkillObject RelevantSkill => _itemObject.RelevantSkill;

        public int Difficulty => _itemObject.Difficulty;

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
                if (IsArmor)
                    // Body Armor, Gloves...
                    return Format.CamelCaseToTitle(Type.ToString());

                if (IsHorse)
                    // Horse, War Horse, Noble Horse...
                    return Format.CamelCaseToTitle(Category.ToString());

                if (IsWeapon)
                    // Mace, Bow...
                    return Format.CamelCaseToTitle(PrimaryWeapon.WeaponClass.ToString());

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

                if (RelevantSkill != null && Difficulty > 0)
                    Add($"{RelevantSkill.Name}", Difficulty);

                return stats;
            }
        }
    }
}