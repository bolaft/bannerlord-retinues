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

        private static HashSet<string> _vassalRewardItemIdsCache;
        private static HashSet<string> VassalRewardItemIds
        {
            get
            {
                if (_vassalRewardItemIdsCache == null)
                {
                    _vassalRewardItemIdsCache = [];
                    foreach (var culture in WCulture.All)
                    foreach (var item in culture.Base.VassalRewardItems)
                        _vassalRewardItemIdsCache.Add(item.StringId);
                }

                return _vassalRewardItemIdsCache;
            }
        }

        public bool IsVassalRewardItem => VassalRewardItemIds.Contains(StringId);

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Comparisons                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        private struct ChevronCacheEntry
        {
            public int Positive;
            public int Negative;
        }

        private static readonly Dictionary<string, ChevronCacheEntry> _chevronCache = [];

        private static readonly object _chevronCacheLock = new object();

        private static string GetChevronCacheKey(WItem a, WItem b)
        {
            // Directional comparison: A vs B is not the same as B vs A.
            // Use StringId; fall back to GetHashCode if needed.
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

            // Same item: no icons, and no need to cache.
            if (StringId == other.StringId)
                return;

            string key = GetChevronCacheKey(this, other);

            // Try cache first
            lock (_chevronCacheLock)
            {
                if (_chevronCache.TryGetValue(key, out var cached))
                {
                    positiveChevrons = cached.Positive;
                    negativeChevrons = cached.Negative;
                    return;
                }
            }

            // Compute fresh
            ComputeComparisonChevronsCore(other, out positiveChevrons, out negativeChevrons);

            // Store in cache
            lock (_chevronCacheLock)
            {
                _chevronCache[key] = new ChevronCacheEntry
                {
                    Positive = positiveChevrons,
                    Negative = negativeChevrons,
                };
            }
        }

        private void ComputeComparisonChevronsCore(
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

                // Require same weapon class (sword vs sword, bow vs bow, shield vs shield, etc.).
                if (w1.WeaponClass != w2.WeaponClass)
                    return;

                // Shields
                if (IsShield || other.IsShield)
                {
                    if (!IsShield || !other.IsShield)
                        return;

                    CompareShieldChevrons(other, out positiveChevrons, out negativeChevrons);
                    return;
                }

                // Ammo (arrows, bolts, bullets, thrown stacks, etc.).
                if (IsAmmo || other.IsAmmo)
                {
                    if (!IsAmmo || !other.IsAmmo)
                        return;

                    CompareAmmoChevrons(other, out positiveChevrons, out negativeChevrons);
                    return;
                }

                // Pure ranged weapons (bows, crossbows, guns, throwing).
                if (IsRangedWeapon && other.IsRangedWeapon)
                {
                    CompareRangedWeaponChevrons(other, out positiveChevrons, out negativeChevrons);
                    return;
                }

                // Pure melee weapons (swords, axes, maces, polearms).
                if (IsMeleeWeapon && other.IsMeleeWeapon)
                {
                    CompareMeleeWeaponChevrons(other, out positiveChevrons, out negativeChevrons);
                    return;
                }

                // Mixed types (melee vs ranged of same WeaponClass) are not compared.
                return;
            }

            // Human armor (head, body, hands, legs).
            if (
                ArmorComponent != null
                && other.ArmorComponent != null
                && Type != ItemObject.ItemTypeEnum.HorseHarness
                && other.Type != ItemObject.ItemTypeEnum.HorseHarness
            )
            {
                // Require same armor slot type (two helmets, two body armors, etc.).
                if (Type != other.Type)
                    return;

                CompareArmorChevrons(other, out positiveChevrons, out negativeChevrons);
                return;
            }

            // Horse harness (horse armor).
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

            // Horses.
            if (IsHorse && other.IsHorse && HorseComponent != null && other.HorseComponent != null)
            {
                CompareHorseChevrons(other, out positiveChevrons, out negativeChevrons);
                return;
            }

            // Other categories not handled yet -> no icons.
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
                return; // all equal -> no icon

            // Perfect tradeoff (same number of better/worse) -> no icon.
            if (better == worse)
                return;

            // All stats in one direction.
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

            // Mixed: majority side gets 2 chevrons, minority gets 1.
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

            // Damage
            AccumulateStatComparison(
                w1.SwingDamage,
                w2.SwingDamage,
                higherIsBetter: true,
                ref better,
                ref worse
            );
            AccumulateStatComparison(
                w1.ThrustDamage,
                w2.ThrustDamage,
                higherIsBetter: true,
                ref better,
                ref worse
            );

            // Speed
            AccumulateStatComparison(
                w1.SwingSpeed,
                w2.SwingSpeed,
                higherIsBetter: true,
                ref better,
                ref worse
            );
            AccumulateStatComparison(
                w1.ThrustSpeed,
                w2.ThrustSpeed,
                higherIsBetter: true,
                ref better,
                ref worse
            );

            // Reach & handling
            AccumulateStatComparison(
                w1.WeaponLength,
                w2.WeaponLength,
                higherIsBetter: true,
                ref better,
                ref worse
            );
            AccumulateStatComparison(
                w1.Handling,
                w2.Handling,
                higherIsBetter: true,
                ref better,
                ref worse
            );

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

            // Damage
            AccumulateStatComparison(
                w1.SwingDamage,
                w2.SwingDamage,
                higherIsBetter: true,
                ref better,
                ref worse
            );
            AccumulateStatComparison(
                w1.ThrustDamage,
                w2.ThrustDamage,
                higherIsBetter: true,
                ref better,
                ref worse
            );

            // Projectile speed & accuracy
            AccumulateStatComparison(
                w1.MissileSpeed,
                w2.MissileSpeed,
                higherIsBetter: true,
                ref better,
                ref worse
            );
            AccumulateStatComparison(
                w1.Accuracy,
                w2.Accuracy,
                higherIsBetter: true,
                ref better,
                ref worse
            );

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

            // Damage & projectile behavior
            AccumulateStatComparison(
                w1.SwingDamage,
                w2.SwingDamage,
                higherIsBetter: true,
                ref better,
                ref worse
            );
            AccumulateStatComparison(
                w1.ThrustDamage,
                w2.ThrustDamage,
                higherIsBetter: true,
                ref better,
                ref worse
            );
            AccumulateStatComparison(
                w1.MissileSpeed,
                w2.MissileSpeed,
                higherIsBetter: true,
                ref better,
                ref worse
            );
            AccumulateStatComparison(
                w1.Accuracy,
                w2.Accuracy,
                higherIsBetter: true,
                ref better,
                ref worse
            );

            // Stack size (MaxDataValue): more ammo is better.
            int thisStack = w1.MaxDataValue;
            int otherStack = w2.MaxDataValue;
            AccumulateStatComparison(
                thisStack,
                otherStack,
                higherIsBetter: true,
                ref better,
                ref worse
            );

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

            // Shield durability (hit points)
            int thisHp = w1.MaxDataValue;
            int otherHp = w2.MaxDataValue;
            AccumulateStatComparison(thisHp, otherHp, higherIsBetter: true, ref better, ref worse);

            // Shield armor
            AccumulateStatComparison(
                w1.BodyArmor,
                w2.BodyArmor,
                higherIsBetter: true,
                ref better,
                ref worse
            );

            // Handling
            AccumulateStatComparison(
                w1.Handling,
                w2.Handling,
                higherIsBetter: true,
                ref better,
                ref worse
            );

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

            // Human armor: compare all four regions.
            AccumulateStatComparison(
                a1.HeadArmor,
                a2.HeadArmor,
                higherIsBetter: true,
                ref better,
                ref worse
            );
            AccumulateStatComparison(
                a1.BodyArmor,
                a2.BodyArmor,
                higherIsBetter: true,
                ref better,
                ref worse
            );
            AccumulateStatComparison(
                a1.ArmArmor,
                a2.ArmArmor,
                higherIsBetter: true,
                ref better,
                ref worse
            );
            AccumulateStatComparison(
                a1.LegArmor,
                a2.LegArmor,
                higherIsBetter: true,
                ref better,
                ref worse
            );

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

            // Horse armor: overall armor + maneuver/speed/charge bonuses.
            AccumulateStatComparison(
                a1.BodyArmor,
                a2.BodyArmor,
                higherIsBetter: true,
                ref better,
                ref worse
            );
            AccumulateStatComparison(
                a1.ManeuverBonus,
                a2.ManeuverBonus,
                higherIsBetter: true,
                ref better,
                ref worse
            );
            AccumulateStatComparison(
                a1.SpeedBonus,
                a2.SpeedBonus,
                higherIsBetter: true,
                ref better,
                ref worse
            );
            AccumulateStatComparison(
                a1.ChargeBonus,
                a2.ChargeBonus,
                higherIsBetter: true,
                ref better,
                ref worse
            );

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

            // Horse core stats
            AccumulateStatComparison(
                h1.Speed,
                h2.Speed,
                higherIsBetter: true,
                ref better,
                ref worse
            );
            AccumulateStatComparison(
                h1.Maneuver,
                h2.Maneuver,
                higherIsBetter: true,
                ref better,
                ref worse
            );
            AccumulateStatComparison(
                h1.ChargeDamage,
                h2.ChargeDamage,
                higherIsBetter: true,
                ref better,
                ref worse
            );

            // HitPoints + bonus (more durable mount is better).
            int thisHp = h1.HitPoints + h1.HitPointBonus;
            int otherHp = h2.HitPoints + h2.HitPointBonus;
            AccumulateStatComparison(thisHp, otherHp, higherIsBetter: true, ref better, ref worse);

            GetChevronsFromCounts(better, worse, out positiveChevrons, out negativeChevrons);
        }
    }
}
