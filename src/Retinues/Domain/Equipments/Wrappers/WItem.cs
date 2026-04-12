using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Helpers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Model;
using Retinues.Framework.Runtime;
using Retinues.Settings;
using TaleWorlds.Core;
#if BL13 || BL14
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
#endif

namespace Retinues.Domain.Equipments.Wrappers
{
    public partial class WItem(ItemObject @base) : WBase<WItem, ItemObject>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Name => Base.Name.ToString();
        public int Tier => (int)Base.Tier + 1; // Tier is 0-indexed internally.
        public int Value => Base.Value;
        public float Weight => Base.Weight;

        public ItemCategory Category => Base.ItemCategory;
        public ItemObject.ItemTypeEnum Type => Base.ItemType;

        // Neutral culture items ("calradian") return null for Culture.
        public WCulture Culture =>
            Base.Culture != null && Base.Culture.StringId != "neutral_culture"
                ? WCulture.Get(Base.Culture.StringId)
                : null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsCivilian => Base.IsCivilian;
        public bool IsMeleeWeapon => PrimaryWeapon?.IsMeleeWeapon ?? false;
        public bool IsRangedWeapon => PrimaryWeapon?.IsRangedWeapon == true && !IsThrownWeapon;
        public bool IsThrownWeapon => Type == ItemObject.ItemTypeEnum.Thrown;
        public bool IsArmor =>
            ArmorComponent != null && ItemObject.ItemTypeEnum.HorseHarness != Type;
        public bool IsHorse => HorseComponent != null;
        public bool IsHorseHarness => Type == ItemObject.ItemTypeEnum.HorseHarness;
        public bool IsWeapon => WeaponComponent != null && PrimaryWeapon != null;
        public bool IsShield => PrimaryWeapon?.IsShield ?? false;
        public bool IsAmmo => PrimaryWeapon?.IsAmmo ?? false;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Image                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

#if BL13 || BL14
        public ItemImageIdentifierVM Image => new(Base);
#else
        public ImageIdentifierVM Image => new(Base);
#endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Equipment & Validation                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// All items that are considered valid equipment.
        /// </summary>
        private static List<WItem> _equipments;
        public static List<WItem> Equipments =>
            _equipments ??= [.. All.Where(i => i.IsValidEquipment)];

        /// <summary>
        /// Equipments indexed by their equippable slots.
        /// </summary>
        private static Dictionary<EquipmentIndex, List<WItem>> _equipmentsBySlot;
        public static Dictionary<EquipmentIndex, List<WItem>> EquipmentsBySlot
        {
            get
            {
                if (_equipmentsBySlot == null)
                {
                    _equipmentsBySlot = [];
                    foreach (var item in Equipments)
                    {
                        foreach (var slot in item.Slots)
                        {
                            if (!_equipmentsBySlot.ContainsKey(slot))
                                _equipmentsBySlot[slot] = [];

                            _equipmentsBySlot[slot].Add(item);
                        }
                    }
                }

                return _equipmentsBySlot;
            }
        }

        /// <summary>
        /// Gets all equipments that can be equipped in the given slot.
        /// </summary>
        public static List<WItem> GetEquipmentsForSlot(EquipmentIndex slot)
        {
            if (EquipmentsBySlot.TryGetValue(slot, out var items))
                return items;

            return [];
        }

        [StaticClearAction]
        public static void ClearStaticCaches()
        {
            _equipments = null;
            _equipmentsBySlot = null;
            _vassalRewards = null;
        }

        /// <summary>
        /// Indicates whether this item is considered valid equipment.
        /// </summary>
        public bool IsEquipment
        {
            get
            {
                if (
                    IsArmor
                    || IsHorseHarness
                    || IsMeleeWeapon
                    || IsRangedWeapon
                    || IsAmmo
                    || IsShield
                )
                    return true;

                if (IsHorse && !Base.HorseComponent.IsPackAnimal)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Indicates whether this item is valid to show in the equipment editor list
        /// (filters out mission-only / siege pickup items like boulders, ballista ammo, etc).
        /// </summary>
        public bool IsValidEquipment
        {
            get
            {
                if (!IsEquipment)
                    return false;

                // No banners.
                if (Type == ItemObject.ItemTypeEnum.Banner)
                    return false;

                // Filter obvious siege/pickup weapon classes.
                if (PrimaryWeapon != null)
                {
                    var wc = PrimaryWeapon.WeaponClass;

                    if (wc == WeaponClass.Boulder || wc == WeaponClass.Banner)
                        return false;

#if BL13 || BL14
                    if (wc == WeaponClass.BallistaBoulder || wc == WeaponClass.BallistaStone)
                        return false;
#endif
                }

                // Heuristic: most mission-only ammo/pickups are not merchandise.
                // Keep crafted + vassal rewards even if not merchandise.
                if (Base.NotMerchandise && !IsCrafted && !IsVassalReward)
                {
                    // This targets things like grapeshot / special ammo and floor-pickups.
                    if (IsWeapon || IsAmmo || IsShield)
                        return false;
                }

                return true;
            }
        }

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
                    slots.Add(EquipmentIndex.Weapon0);
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
#if BL13 || BL14
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

        /// <summary>
        /// Indicates whether the given character of the given tier can equip this item,
        /// considering mount restrictions for low-tier troops.
        /// </summary>
        public bool IsEquippableByCharacterOfTier(int tier)
        {
            if (IsHorse && tier < Configuration.MinTierForMounts)
                return false;

            if (
                Category == DefaultItemCategories.WarHorse
                && tier < Configuration.MinTierForWarMounts
            )
                return false;

            if (
                Category == DefaultItemCategories.NobleHorse
                && tier < Configuration.MinTierForNobleMounts
            )
                return false;

            return true;
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
        //                      Compatibility                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Indicates whether this item is compatible with another item.
        /// For now always returns true, except for horse vs harness where it
        /// validates the mount family type to prevent visual bugs (camel harness on horse etc).
        /// </summary>
        public bool IsCompatibleWith(WItem other)
        {
            if (other == null)
                return true;

            // Only enforce rules for horse <-> harness pairings.
            if (IsHorse && other.IsHorseHarness)
                return IsHorseHarnessCompatibleWithHorse(harness: other, horse: this);

            if (IsHorseHarness && other.IsHorse)
                return IsHorseHarnessCompatibleWithHorse(harness: this, horse: other);

            return true;
        }

        /// <summary>
        /// Indicates whether the given horse harness is compatible with the given horse.
        /// </summary>
        private static bool IsHorseHarnessCompatibleWithHorse(WItem harness, WItem horse)
        {
            var monster = horse?.HorseComponent?.Monster;
            var armor = harness?.ArmorComponent;

            // If we cannot read either side, do not block.
            if (monster == null || armor == null)
                return true;

            return monster.FamilyType == armor.FamilyType;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Components                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public ItemComponent ItemComponent => Base.ItemComponent;
        public ArmorComponent ArmorComponent => Base.ArmorComponent;
        public HorseComponent HorseComponent => Base.HorseComponent;
        public WeaponComponent WeaponComponent => Base.WeaponComponent;
        public WeaponComponentData PrimaryWeapon => Base.PrimaryWeapon;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Comparisons                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void GetComparisonChevrons(
            WItem other,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            ItemComparisonHelper.GetComparisonChevrons(
                this,
                other,
                out positiveChevrons,
                out negativeChevrons
            );
        }
    }
}
