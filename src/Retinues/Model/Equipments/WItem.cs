using System.Collections.Generic;
using System.Linq;
using Retinues.Model.Characters;
using Retinues.Model.Factions;
using TaleWorlds.Core;
#if BL13
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
#endif

namespace Retinues.Model.Equipments
{
    public class WItem(ItemObject @base) : WBase<WItem, ItemObject>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Name => Base.Name.ToString();
        public int Tier => (int)Base.Tier;
        public int Value => Base.Value;

        public ItemCategory Category => Base.ItemCategory;
        public ItemObject.ItemTypeEnum Type => Base.ItemType;

        public WCulture Culture => WCulture.Get(Base.Culture.StringId);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsCivilian => Base.IsCivilian;
        public bool IsMeleeWeapon => PrimaryWeapon?.IsMeleeWeapon ?? false;
        public bool IsRangedWeapon => PrimaryWeapon?.IsRangedWeapon ?? false;
        public bool IsArmor =>
            ArmorComponent != null && ItemObject.ItemTypeEnum.HorseHarness != Type;
        public bool IsHorse => HorseComponent != null;
        public bool IsWeapon => WeaponComponent != null && PrimaryWeapon != null;
        public bool IsShield => PrimaryWeapon?.IsShield ?? false;
        public bool IsAmmo => PrimaryWeapon?.IsAmmo ?? false;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Image                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

#if BL13
        public ItemImageIdentifierVM Image => new(Base);
#else
        public ImageIdentifierVM Image => new(Base);
#endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Slots                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the equipment slots this item can be placed in, based on its type.
        /// </summary>
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
#if BL13
                    case ItemObject.ItemTypeEnum.Sling:
                    case ItemObject.ItemTypeEnum.SlingStones:
#endif
                        AddWeaponSlots();
                        break;
                }

                return slots;
            }
        }

        /// <summary>
        /// Indicates whether this item can be equipped in the given slot.
        /// </summary>
        public bool IsEquippableInSlot(EquipmentIndex slot) => Slots.Contains(slot);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Skill Requirement                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public SkillObject RelevantSkill => Base.RelevantSkill;
        public int Difficulty => Base.Difficulty;

        /// <summary>
        /// Indicates whether the given character can equip this item,
        /// </summary>
        public bool IsEquippableByCharacter(WCharacter character)
        {
            if (RelevantSkill is null)
                return true;

            if (Difficulty <= 0)
                return true;

            return character.Skills.Get(RelevantSkill) >= Difficulty;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Vassal Rewards                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Indicates whether this item can be given as a vassal reward.
        /// </summary>
        public bool IsVassalReward => VassalRewards.Contains(this);

        /// <summary>
        /// All items that can be given as vassal rewards.
        /// </summary>
        private static HashSet<WItem> _vassalRewards;
        public static HashSet<WItem> VassalRewards
        {
            get
            {
                if (_vassalRewards == null)
                {
                    _vassalRewards = [];
                    foreach (var culture in WCulture.All)
                    foreach (var item in culture.Base.VassalRewardItems)
                        _vassalRewards.Add(Get(item));
                }

                return _vassalRewards;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Crafted Items                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Indicates whether this item is a crafted weapon.
        /// </summary>
        public bool IsCrafted => Base.IsCraftedByPlayer && Base.WeaponDesign != null;

        /// <summary>
        /// Gets the design code for this item, if it is a crafted weapon.
        /// </summary>
        public string DesignCode
        {
            get
            {
                if (!IsCrafted || Base.WeaponDesign?.UsedPieces == null)
                    return null;

                string designCode = string.Join(
                    ":",
                    Base.WeaponDesign.UsedPieces.Select(p => p?.CraftingPiece?.StringId)
                );

                return $"{Name}:{designCode}:{Value}";
            }
        }

        /// <summary>
        /// Filters the given items to only include crafted items and
        /// remove duplicates by design code.
        /// </summary>
        public List<WItem> FilterCraftedItems(IEnumerable<WItem> items)
        {
            HashSet<string> seenDesignCodes = [];
            List<WItem> craftedItems = [];

            foreach (var item in items)
            {
                if (!item.IsCrafted)
                    continue;

                string designCode = item.DesignCode;
                if (seenDesignCodes.Contains(designCode))
                    continue;

                seenDesignCodes.Add(designCode);
                craftedItems.Add(item);
            }

            return craftedItems;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Components                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public ItemComponent ItemComponent => Base.ItemComponent;
        public ArmorComponent ArmorComponent => Base.ArmorComponent;
        public HorseComponent HorseComponent => Base.HorseComponent;
        public WeaponComponent WeaponComponent => Base.WeaponComponent;
        public WeaponComponentData PrimaryWeapon => Base.PrimaryWeapon;
    }
}
