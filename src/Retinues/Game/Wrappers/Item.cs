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

        private static readonly Dictionary<string, bool> _isBetterThanCache = [];
        private static readonly object _isBetterThanCacheLock = new();

        /// <summary>
        /// Returns true if this item is a straight upgrade over the other item:
        /// Uses a static cache keyed by StringId to avoid recomputing.
        /// </summary>
        public bool IsBetterThan(WItem other)
        {
            if (other == null)
                return false;

            string key = $"{StringId}=>{other.StringId}";
            lock (_isBetterThanCacheLock)
            {
                if (_isBetterThanCache.TryGetValue(key, out var cached))
                {
                    return cached;
                }
            }

            bool result = ComputeIsBetter(other);

            lock (_isBetterThanCacheLock)
            {
                _isBetterThanCache[key] = result;
            }

            return result;
        }

        /// <summary>
        /// Actual computation of IsBetterThan without caching.
        /// </summary>
        private bool ComputeIsBetter(WItem other)
        {
            if (this == other)
            {
                Log.Info("WItem.IsBetterThan: comparing same item -> FALSE");
                return false;
            }

            Log.Info(
                $"WItem.IsBetterThan: comparing {Name} ({StringId})to {(other != null ? other.Name : "null")} ({(other != null ? other.StringId : "null")})"
            );
            if (other == null)
            {
                Log.Info("WItem.IsBetterThan: other is null -> FALSE");
                return false;
            }

            // Weapons (melee / ranged / shields / ammo)
            if (IsWeapon && other.IsWeapon)
            {
                var w1 = PrimaryWeapon;
                var w2 = other.PrimaryWeapon;
                if (w1 == null || w2 == null)
                {
                    Log.Info(
                        "WItem.IsBetterThan: one of the weapons has null PrimaryWeapon -> FALSE"
                    );
                    return false;
                }

                // Require same weapon class (sword vs sword, bow vs bow, shield vs shield, etc.).
                if (w1.WeaponClass != w2.WeaponClass)
                {
                    Log.Info("WItem.IsBetterThan: weapon classes differ -> FALSE");
                    return false;
                }

                // Shields
                if (IsShield || other.IsShield)
                {
                    if (!IsShield || !other.IsShield)
                    {
                        Log.Info("WItem.IsBetterThan: one item is shield, other is not -> FALSE");
                        return false;
                    }

                    var result = IsBetterShieldThan(other);
                    Log.Info($"WItem.IsBetterThan: shield comparison result -> {result}");
                    return result;
                }

                // Ammo (arrows, bolts, bullets, thrown stacks, etc.).
                if (IsAmmo || other.IsAmmo)
                {
                    if (!IsAmmo || !other.IsAmmo)
                    {
                        Log.Info("WItem.IsBetterThan: one item is ammo, other is not -> FALSE");
                        return false;
                    }

                    var result = IsBetterAmmoThan(other);
                    Log.Info($"WItem.IsBetterThan: ammo comparison result -> {result}");
                    return result;
                }

                // Pure ranged weapons (bows, crossbows, guns, throwing).
                if (IsRangedWeapon && other.IsRangedWeapon)
                {
                    var result = IsBetterRangedWeaponThan(other);
                    Log.Info($"WItem.IsBetterThan: ranged weapon comparison result -> {result}");
                    return result;
                }

                // Pure melee weapons (swords, axes, maces, polearms).
                if (IsMeleeWeapon && other.IsMeleeWeapon)
                {
                    var result = IsBetterMeleeWeaponThan(other);
                    Log.Info($"WItem.IsBetterThan: melee weapon comparison result -> {result}");
                    return result;
                }

                // Mixed types (melee vs ranged of same WeaponClass) are not compared.
                Log.Info("WItem.IsBetterThan: mixed weapon types (melee vs ranged) -> FALSE");
                return false;
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
                {
                    Log.Info("WItem.IsBetterThan: armor types differ -> FALSE");
                    return false;
                }

                var result = IsBetterArmorThan(other);
                Log.Info($"WItem.IsBetterThan: armor comparison result -> {result}");
                return result;
            }

            // Horse harness (horse armor).
            if (
                Type == ItemObject.ItemTypeEnum.HorseHarness
                && other.Type == ItemObject.ItemTypeEnum.HorseHarness
                && ArmorComponent != null
                && other.ArmorComponent != null
            )
            {
                var result = IsBetterHorseHarnessThan(other);
                Log.Info($"WItem.IsBetterThan: horse harness comparison result -> {result}");
                return result;
            }

            // Horses.
            if (IsHorse && other.IsHorse && HorseComponent != null && other.HorseComponent != null)
            {
                var result = IsBetterHorseThan(other);
                Log.Info($"WItem.IsBetterThan: horse comparison result -> {result}");
                return result;
            }

            Log.Info("WItem.IsBetterThan: other categories not handled -> FALSE");
            // Other categories not handled yet → not considered a straight upgrade.
            return false;
        }

        private static bool IsStatUpgrade(
            int thisValue,
            int otherValue,
            bool higherIsBetter,
            ref bool anyBetter
        )
        {
            if (higherIsBetter)
            {
                if (thisValue < otherValue)
                    return false;

                if (thisValue > otherValue)
                    anyBetter = true;
            }
            else
            {
                if (thisValue > otherValue)
                    return false;

                if (thisValue < otherValue)
                    anyBetter = true;
            }

            return true;
        }

        private bool IsBetterMeleeWeaponThan(WItem other)
        {
            var w1 = PrimaryWeapon;
            var w2 = other.PrimaryWeapon;

            bool anyBetter = false;

            // Damage
            if (!IsStatUpgrade(w1.SwingDamage, w2.SwingDamage, higherIsBetter: true, ref anyBetter))
                return false;
            if (
                !IsStatUpgrade(
                    w1.ThrustDamage,
                    w2.ThrustDamage,
                    higherIsBetter: true,
                    ref anyBetter
                )
            )
                return false;

            // Speed
            if (!IsStatUpgrade(w1.SwingSpeed, w2.SwingSpeed, higherIsBetter: true, ref anyBetter))
                return false;
            if (!IsStatUpgrade(w1.ThrustSpeed, w2.ThrustSpeed, higherIsBetter: true, ref anyBetter))
                return false;

            // Reach & handling
            if (
                !IsStatUpgrade(
                    w1.WeaponLength,
                    w2.WeaponLength,
                    higherIsBetter: true,
                    ref anyBetter
                )
            )
                return false;
            if (!IsStatUpgrade(w1.Handling, w2.Handling, higherIsBetter: true, ref anyBetter))
                return false;

            return anyBetter;
        }

        private bool IsBetterRangedWeaponThan(WItem other)
        {
            var w1 = PrimaryWeapon;
            var w2 = other.PrimaryWeapon;

            bool anyBetter = false;

            // Damage (bows/crossbows/guns/throwing): use both swing/thrust and let zeros pass.
            if (!IsStatUpgrade(w1.SwingDamage, w2.SwingDamage, higherIsBetter: true, ref anyBetter))
                return false;
            if (
                !IsStatUpgrade(
                    w1.ThrustDamage,
                    w2.ThrustDamage,
                    higherIsBetter: true,
                    ref anyBetter
                )
            )
                return false;

            // Projectile speed & accuracy.
            if (
                !IsStatUpgrade(
                    w1.MissileSpeed,
                    w2.MissileSpeed,
                    higherIsBetter: true,
                    ref anyBetter
                )
            )
                return false;
            if (!IsStatUpgrade(w1.Accuracy, w2.Accuracy, higherIsBetter: true, ref anyBetter))
                return false;

            return anyBetter;
        }

        private bool IsBetterAmmoThan(WItem other)
        {
            var w1 = PrimaryWeapon;
            var w2 = other.PrimaryWeapon;

            bool anyBetter = false;

            // Damage & projectile behavior.
            if (!IsStatUpgrade(w1.SwingDamage, w2.SwingDamage, higherIsBetter: true, ref anyBetter))
                return false;
            if (
                !IsStatUpgrade(
                    w1.ThrustDamage,
                    w2.ThrustDamage,
                    higherIsBetter: true,
                    ref anyBetter
                )
            )
                return false;
            if (
                !IsStatUpgrade(
                    w1.MissileSpeed,
                    w2.MissileSpeed,
                    higherIsBetter: true,
                    ref anyBetter
                )
            )
                return false;
            if (!IsStatUpgrade(w1.Accuracy, w2.Accuracy, higherIsBetter: true, ref anyBetter))
                return false;

            // Stack size (MaxDataValue): more ammo is better.
            // MaxDataValue is short, but compare as int.
            int thisStack = PrimaryWeapon.MaxDataValue;
            int otherStack = other.PrimaryWeapon.MaxDataValue;

            if (!IsStatUpgrade(thisStack, otherStack, higherIsBetter: true, ref anyBetter))
                return false;

            return anyBetter;
        }

        private bool IsBetterShieldThan(WItem other)
        {
            var w1 = PrimaryWeapon;
            var w2 = other.PrimaryWeapon;

            bool anyBetter = false;

            // Shield durability (hit points): MaxDataValue is used by GetModifiedHitPoints.
            int thisHp = w1.MaxDataValue;
            int otherHp = w2.MaxDataValue;
            if (!IsStatUpgrade(thisHp, otherHp, higherIsBetter: true, ref anyBetter))
                return false;

            // Shield armor (how much damage is blocked).
            if (!IsStatUpgrade(w1.BodyArmor, w2.BodyArmor, higherIsBetter: true, ref anyBetter))
                return false;

            // Handling (how quickly it can be moved/raised).
            if (!IsStatUpgrade(w1.Handling, w2.Handling, higherIsBetter: true, ref anyBetter))
                return false;

            return anyBetter;
        }

        private bool IsBetterArmorThan(WItem other)
        {
            var a1 = ArmorComponent;
            var a2 = other.ArmorComponent;
            if (a1 == null || a2 == null)
                return false;

            bool anyBetter = false;

            // Human armor: compare all four regions. Zeros simply compare equal.
            if (!IsStatUpgrade(a1.HeadArmor, a2.HeadArmor, higherIsBetter: true, ref anyBetter))
                return false;
            if (!IsStatUpgrade(a1.BodyArmor, a2.BodyArmor, higherIsBetter: true, ref anyBetter))
                return false;
            if (!IsStatUpgrade(a1.ArmArmor, a2.ArmArmor, higherIsBetter: true, ref anyBetter))
                return false;
            if (!IsStatUpgrade(a1.LegArmor, a2.LegArmor, higherIsBetter: true, ref anyBetter))
                return false;

            return anyBetter;
        }

        private bool IsBetterHorseHarnessThan(WItem other)
        {
            var a1 = ArmorComponent;
            var a2 = other.ArmorComponent;
            if (a1 == null || a2 == null)
                return false;

            bool anyBetter = false;

            // Horse armor: overall armor + maneuver/speed/charge bonuses.
            if (!IsStatUpgrade(a1.BodyArmor, a2.BodyArmor, higherIsBetter: true, ref anyBetter))
                return false;
            if (
                !IsStatUpgrade(
                    a1.ManeuverBonus,
                    a2.ManeuverBonus,
                    higherIsBetter: true,
                    ref anyBetter
                )
            )
                return false;
            if (!IsStatUpgrade(a1.SpeedBonus, a2.SpeedBonus, higherIsBetter: true, ref anyBetter))
                return false;
            if (!IsStatUpgrade(a1.ChargeBonus, a2.ChargeBonus, higherIsBetter: true, ref anyBetter))
                return false;

            return anyBetter;
        }

        private bool IsBetterHorseThan(WItem other)
        {
            var h1 = HorseComponent;
            var h2 = other.HorseComponent;
            if (h1 == null || h2 == null)
                return false;

            bool anyBetter = false;

            // Horse core stats: speed, maneuver, charge, hit points.
            if (!IsStatUpgrade(h1.Speed, h2.Speed, higherIsBetter: true, ref anyBetter))
                return false;
            if (!IsStatUpgrade(h1.Maneuver, h2.Maneuver, higherIsBetter: true, ref anyBetter))
                return false;
            if (
                !IsStatUpgrade(
                    h1.ChargeDamage,
                    h2.ChargeDamage,
                    higherIsBetter: true,
                    ref anyBetter
                )
            )
                return false;

            // HitPoints + bonus (more durable mount is better).
            int thisHp = h1.HitPoints + h1.HitPointBonus;
            int otherHp = h2.HitPoints + h2.HitPointBonus;
            if (!IsStatUpgrade(thisHp, otherHp, higherIsBetter: true, ref anyBetter))
                return false;

            return anyBetter;
        }
    }
}
