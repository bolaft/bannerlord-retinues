using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Retinues.Model.Characters;
using Retinues.Model.Factions;
using Retinues.Utilities;
using TaleWorlds.Core;
#if BL13
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
#endif

namespace Retinues.Model.Equipments
{
    public class WItem(ItemObject @base) : WBase<WItem, ItemObject>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Cached Static Lists                  //
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
            _chevronCache.Clear();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Name => Base.Name.ToString();
        public int Tier => (int)Base.Tier;
        public int Value => Base.Value;

        public ItemCategory Category => Base.ItemCategory;
        public ItemObject.ItemTypeEnum Type => Base.ItemType;

        public WCulture Culture =>
            Base.Culture != null ? WCulture.Get(Base.Culture.StringId) : null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Stock                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<int> StockAttribute => Attribute(initialValue: 0);

        public int Stock
        {
            get => StockAttribute.Get();
            set => StockAttribute.Set(value);
        }

        /// <summary>
        /// Increases the stock by the given amount.
        /// </summary>
        public void IncreaseStock(int amount = 1) => Stock += amount;

        /// <summary>
        /// Decreases the stock by the given amount.
        /// </summary>
        public void DecreaseStock(int amount = 1) => Stock = System.Math.Max(Stock - amount, 0);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Unlock                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// The unlock progress required to unlock this item.
        /// </summary>
        const int UnlockThreshold = 100;

        public bool IsUnlocked => UnlockProgress >= UnlockThreshold;

        public int UnlockProgress
        {
            get => UnlockProgressAttribute.Get();
            set => UnlockProgressAttribute.Set(value);
        }

        MAttribute<int> UnlockProgressAttribute => Attribute(initialValue: 0);

        /// <summary>
        /// Increases the unlock progress by the given amount,
        /// capping it at the unlock threshold.
        /// </summary>
        public bool IncreaseUnlockProgress(int amount)
        {
            if (IsUnlocked)
                return true;

            UnlockProgress = System.Math.Min(UnlockProgress + amount, UnlockThreshold);

            return IsUnlocked;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

#if BL13
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

        public bool IsCivilian => Base.IsCivilian;
        public bool IsMeleeWeapon => PrimaryWeapon?.IsMeleeWeapon ?? false;
        public bool IsRangedWeapon => PrimaryWeapon?.IsRangedWeapon ?? false;
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Comparisons                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private struct ChevronCacheEntry
        {
            public int Positive;
            public int Negative;
        }

        private static readonly ConcurrentDictionary<string, ChevronCacheEntry> _chevronCache = [];

        private static string GetChevronCacheKey(WItem a, WItem b)
        {
            var idA = a?.StringId ?? "__NULL_A__";
            var idB = b?.StringId ?? "__NULL_B__";
            return idA + "=>" + idB;
        }

        public void GetComparisonChevrons(
            WItem other,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            positiveChevrons = 0;
            negativeChevrons = 0;

            if (other == null)
                return;

            if (other == this)
                return;

            string key = GetChevronCacheKey(this, other);

            if (_chevronCache.TryGetValue(key, out var cached))
            {
                positiveChevrons = cached.Positive;
                negativeChevrons = cached.Negative;
                return;
            }

            ComputeComparisonChevronScore(other, out positiveChevrons, out negativeChevrons);

            _chevronCache[key] = new ChevronCacheEntry
            {
                Positive = positiveChevrons,
                Negative = negativeChevrons,
            };
        }

        private void ComputeComparisonChevronScore(
            WItem other,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            positiveChevrons = 0;
            negativeChevrons = 0;

            // Weapons (melee / ranged / shields / ammo)
            if (IsWeapon && other.IsWeapon)
            {
                var w1 = PrimaryWeapon;
                var w2 = other.PrimaryWeapon;
                if (w1 == null || w2 == null)
                    return;

                if (Type != other.Type)
                    return;

                if (IsShield || other.IsShield)
                {
                    if (!IsShield || !other.IsShield)
                        return;

                    CompareShieldChevrons(other, out positiveChevrons, out negativeChevrons);
                    return;
                }

                if (IsAmmo || other.IsAmmo)
                {
                    if (!IsAmmo || !other.IsAmmo)
                        return;

                    CompareAmmoChevrons(other, out positiveChevrons, out negativeChevrons);
                    return;
                }

                if (IsRangedWeapon && other.IsRangedWeapon)
                {
                    CompareRangedWeaponChevrons(other, out positiveChevrons, out negativeChevrons);
                    return;
                }

                if (IsMeleeWeapon && other.IsMeleeWeapon)
                {
                    CompareMeleeWeaponChevrons(other, out positiveChevrons, out negativeChevrons);
                    return;
                }

                return;
            }

            // Human armor (head, body, hands, legs)
            if (
                ArmorComponent != null
                && other.ArmorComponent != null
                && Type != ItemObject.ItemTypeEnum.HorseHarness
                && other.Type != ItemObject.ItemTypeEnum.HorseHarness
            )
            {
                if (Type != other.Type)
                    return;

                CompareArmorChevrons(other, out positiveChevrons, out negativeChevrons);
                return;
            }

            // Horse harness
            if (
                Type == ItemObject.ItemTypeEnum.HorseHarness
                && other.Type == ItemObject.ItemTypeEnum.HorseHarness
                && ArmorComponent != null
                && other.ArmorComponent != null
            )
            {
                CompareHorseHarnessChevrons(other, out positiveChevrons, out negativeChevrons);
                return;
            }

            // Horses
            if (IsHorse && other.IsHorse && HorseComponent != null && other.HorseComponent != null)
            {
                CompareHorseChevrons(other, out positiveChevrons, out negativeChevrons);
                return;
            }
        }

        private static void AccumulateStatComparison(
            int thisValue,
            int otherValue,
            bool higherIsBetter,
            ref int better,
            ref int worse
        )
        {
            if (thisValue == otherValue)
                return;

            bool thisBetter = higherIsBetter ? thisValue > otherValue : thisValue < otherValue;

            if (thisBetter)
                better++;
            else
                worse++;
        }

        private static void GetChevronsFromCounts(
            int better,
            int worse,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            positiveChevrons = 0;
            negativeChevrons = 0;

            int nonEqual = better + worse;
            if (nonEqual == 0)
                return;

            if (better == worse)
                return;

            if (worse == 0)
            {
                positiveChevrons = 3;
                negativeChevrons = 0;
                return;
            }

            if (better == 0)
            {
                positiveChevrons = 0;
                negativeChevrons = 3;
                return;
            }

            if (better > worse)
            {
                positiveChevrons = 2;
                negativeChevrons = 1;
            }
            else
            {
                positiveChevrons = 1;
                negativeChevrons = 2;
            }
        }

        private void CompareMeleeWeaponChevrons(
            WItem other,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            var w1 = PrimaryWeapon;
            var w2 = other.PrimaryWeapon;

            int better = 0,
                worse = 0;

            AccumulateStatComparison(w1.SwingDamage, w2.SwingDamage, true, ref better, ref worse);
            AccumulateStatComparison(w1.ThrustDamage, w2.ThrustDamage, true, ref better, ref worse);

            AccumulateStatComparison(w1.SwingSpeed, w2.SwingSpeed, true, ref better, ref worse);
            AccumulateStatComparison(w1.ThrustSpeed, w2.ThrustSpeed, true, ref better, ref worse);

            AccumulateStatComparison(w1.WeaponLength, w2.WeaponLength, true, ref better, ref worse);
            AccumulateStatComparison(w1.Handling, w2.Handling, true, ref better, ref worse);

            GetChevronsFromCounts(better, worse, out positiveChevrons, out negativeChevrons);
        }

        private void CompareRangedWeaponChevrons(
            WItem other,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            var w1 = PrimaryWeapon;
            var w2 = other.PrimaryWeapon;

            int better = 0,
                worse = 0;

            AccumulateStatComparison(w1.SwingDamage, w2.SwingDamage, true, ref better, ref worse);
            AccumulateStatComparison(w1.ThrustDamage, w2.ThrustDamage, true, ref better, ref worse);

            AccumulateStatComparison(w1.MissileSpeed, w2.MissileSpeed, true, ref better, ref worse);
            AccumulateStatComparison(w1.Accuracy, w2.Accuracy, true, ref better, ref worse);

            GetChevronsFromCounts(better, worse, out positiveChevrons, out negativeChevrons);
        }

        private void CompareAmmoChevrons(
            WItem other,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            var w1 = PrimaryWeapon;
            var w2 = other.PrimaryWeapon;

            int better = 0,
                worse = 0;

            AccumulateStatComparison(w1.SwingDamage, w2.SwingDamage, true, ref better, ref worse);
            AccumulateStatComparison(w1.ThrustDamage, w2.ThrustDamage, true, ref better, ref worse);

            AccumulateStatComparison(w1.MissileSpeed, w2.MissileSpeed, true, ref better, ref worse);
            AccumulateStatComparison(w1.Accuracy, w2.Accuracy, true, ref better, ref worse);

            int thisStack = w1.MaxDataValue;
            int otherStack = w2.MaxDataValue;
            AccumulateStatComparison(thisStack, otherStack, true, ref better, ref worse);

            GetChevronsFromCounts(better, worse, out positiveChevrons, out negativeChevrons);
        }

        private void CompareShieldChevrons(
            WItem other,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            var w1 = PrimaryWeapon;
            var w2 = other.PrimaryWeapon;

            int better = 0,
                worse = 0;

            int thisHp = w1.MaxDataValue;
            int otherHp = w2.MaxDataValue;
            AccumulateStatComparison(thisHp, otherHp, true, ref better, ref worse);

            AccumulateStatComparison(w1.BodyArmor, w2.BodyArmor, true, ref better, ref worse);
            AccumulateStatComparison(w1.Handling, w2.Handling, true, ref better, ref worse);

            GetChevronsFromCounts(better, worse, out positiveChevrons, out negativeChevrons);
        }

        private void CompareArmorChevrons(
            WItem other,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            var a1 = ArmorComponent;
            var a2 = other.ArmorComponent;

            if (a1 == null || a2 == null)
            {
                positiveChevrons = 0;
                negativeChevrons = 0;
                return;
            }

            int better = 0,
                worse = 0;

            AccumulateStatComparison(a1.HeadArmor, a2.HeadArmor, true, ref better, ref worse);
            AccumulateStatComparison(a1.BodyArmor, a2.BodyArmor, true, ref better, ref worse);
            AccumulateStatComparison(a1.ArmArmor, a2.ArmArmor, true, ref better, ref worse);
            AccumulateStatComparison(a1.LegArmor, a2.LegArmor, true, ref better, ref worse);

            GetChevronsFromCounts(better, worse, out positiveChevrons, out negativeChevrons);
        }

        private void CompareHorseHarnessChevrons(
            WItem other,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            var a1 = ArmorComponent;
            var a2 = other.ArmorComponent;

            if (a1 == null || a2 == null)
            {
                positiveChevrons = 0;
                negativeChevrons = 0;
                return;
            }

            int better = 0,
                worse = 0;

            AccumulateStatComparison(a1.BodyArmor, a2.BodyArmor, true, ref better, ref worse);
            AccumulateStatComparison(
                a1.ManeuverBonus,
                a2.ManeuverBonus,
                true,
                ref better,
                ref worse
            );
            AccumulateStatComparison(a1.SpeedBonus, a2.SpeedBonus, true, ref better, ref worse);
            AccumulateStatComparison(a1.ChargeBonus, a2.ChargeBonus, true, ref better, ref worse);

            GetChevronsFromCounts(better, worse, out positiveChevrons, out negativeChevrons);
        }

        private void CompareHorseChevrons(
            WItem other,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            var h1 = HorseComponent;
            var h2 = other.HorseComponent;

            if (h1 == null || h2 == null)
            {
                positiveChevrons = 0;
                negativeChevrons = 0;
                return;
            }

            int better = 0,
                worse = 0;

            AccumulateStatComparison(h1.Speed, h2.Speed, true, ref better, ref worse);
            AccumulateStatComparison(h1.Maneuver, h2.Maneuver, true, ref better, ref worse);
            AccumulateStatComparison(h1.ChargeDamage, h2.ChargeDamage, true, ref better, ref worse);

            int thisHp = h1.HitPoints + h1.HitPointBonus;
            int otherHp = h2.HitPoints + h2.HitPointBonus;
            AccumulateStatComparison(thisHp, otherHp, true, ref better, ref worse);

            GetChevronsFromCounts(better, worse, out positiveChevrons, out negativeChevrons);
        }
    }
}
