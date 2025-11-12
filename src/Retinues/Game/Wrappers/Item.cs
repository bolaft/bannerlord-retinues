using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Features.Stocks;
using Retinues.Features.Unlocks;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
#if BL13
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
#endif

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for ItemObject, provides helpers for slot logic, computed stats, unlocks, and stocks.
    /// </summary>
    [SafeClass]
    public class WItem(ItemObject itemObject) : StringIdentifier
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Base                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly ItemObject _itemObject = itemObject;

        public ItemObject Base => _itemObject;

        // Construct from ID
        public WItem(string itemId)
            : this(MBObjectManager.Instance.GetObject<ItemObject>(itemId)) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      VM properties                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

#if BL13
        public ItemImageIdentifierVM Image => new(Base);
#else
        public ImageIdentifierVM Image => new(_itemObject);
#endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Culture                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCulture Culture
        {
            get => Base.Culture != null ? new(_itemObject.Culture as CultureObject) : null;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override string StringId => _itemObject.StringId;

        public string Name => _itemObject.Name.ToString();

        public bool IsCivilian => _itemObject.IsCivilian;

        public int Value => _itemObject.Value;

        public ItemCategory Category => _itemObject.ItemCategory;

        public ItemObject.ItemTypeEnum Type => _itemObject.ItemType;

        public SkillObject RelevantSkill => _itemObject.RelevantSkill;

        public int Difficulty => _itemObject.Difficulty;

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

        public bool IsCrafted => _itemObject.IsCraftedByPlayer && Base.WeaponDesign != null;

        public string CraftedCode
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Unlocks                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns true if the item is unlocked.
        /// </summary>
        public bool IsUnlocked => UnlocksBehavior.IsUnlocked(StringId);

        /// <summary>
        /// Unlocks the item.
        /// </summary>
        public void Unlock() => UnlocksBehavior.Unlock(Base);

        /// <summary>
        /// Locks the item.
        /// </summary>
        public void Lock() => UnlocksBehavior.Lock(Base);

        /// <summary>
        /// Returns true if the item is currently being unlocked.
        /// </summary>
        public bool UnlockInProgress => UnlocksBehavior.InProgress(StringId);

        /// <summary>
        /// Returns the current progress towards unlocking the item.
        /// </summary>
        public int UnlockProgress => UnlocksBehavior.GetProgress(StringId);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Stocks                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns true if the item is stocked.
        /// </summary>
        public bool IsStocked => StocksBehavior.HasStock(StringId);

        /// <summary>
        /// Sets the stock count for the item.
        /// </summary>
        public static void SetStock(WItem item, int count) =>
            StocksBehavior.Set(item?.StringId, count);

        /// <summary>
        /// Gets the current stock count for the item.
        /// </summary>
        public int GetStock() => StocksBehavior.Get(StringId);

        /// <summary>
        /// Increases the stock count for the item by 1.
        /// </summary>
        public void Stock() => StocksBehavior.Add(StringId, 1);

        /// <summary>
        /// Decreases the stock count for the item by 1.
        /// </summary>
        public void Unstock() => StocksBehavior.Add(StringId, -1);
    }
}
