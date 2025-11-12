using System.Collections.Generic;
using Retinues.Features.Upgrade.Behaviors;
using Retinues.Utils;
using TaleWorlds.Core;

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for Equipment, provides helpers for slot/item access, skill requirements, and equipment code serialization.
    /// </summary>
    [SafeClass(SwallowByDefault = false)]
    public class WEquipment
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Equipment _equipment;
        public Equipment Base => _equipment;

        public WEquipment(Equipment equipment, WLoadout loadout, int index)
        {
            _equipment = equipment;
            Loadout = loadout;
            Index = index;

            SanitizeArmor(_equipment);
        }

        public WEquipment(Equipment equipment, WCharacter troop, int index)
            : this(equipment, troop.Loadout, index) { }

        /// <summary>
        /// Clears any item placed in an armor slot that has no ArmorComponent.
        /// Use before handing Equipment to the engine.
        /// </summary>
        public static void SanitizeArmor(Equipment eq)
        {
            if (eq == null)
                return;
            var slots = new[]
            {
                EquipmentIndex.Head,
                EquipmentIndex.Body,
                EquipmentIndex.Leg,
                EquipmentIndex.Gloves,
            };
            foreach (var s in slots)
            {
                var el = eq[s];
                var it = el.Item;
                if (it != null && it.ArmorComponent == null)
                    eq[s] = default; // clear slot
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Loadout                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WLoadout Loadout { get; private set; }
        public int Index { get; private set; }
        public EquipmentCategory Category => Loadout.GetCategory(Index);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Civilian                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Change this equipment's civilian/battle type in-place.
        /// </summary>
        public void SetCivilian(bool makeCivilian)
        {
#if BL13
            try
            {
                var want = makeCivilian
                    ? Equipment.EquipmentType.Civilian
                    : Equipment.EquipmentType.Battle;
                Reflector.SetFieldValue(_equipment, "_equipmentType", want);
            }
            catch
            {
                // best-effort; ignore if field name changes in a future version
            }
#else
            if (_equipment.IsCivilian == makeCivilian)
                return;

            // Swap backing field
            Reflector.SetFieldValue(
                Base,
                "_equipmentType",
                makeCivilian ? Equipment.EquipmentType.Civilian : Equipment.EquipmentType.Battle
            );
#endif
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Code                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Creates a WEquipment from an equipment code string.
        /// If forceCivilian is provided, the created Equipment will be coerced to that type.
        /// </summary>
        public static WEquipment FromCode(
            string code,
            WLoadout loadout,
            int index,
            bool? forceCivilian = null
        )
        {
            Equipment obj;

            if (code is null)
            {
#if BL13
                obj = new Equipment(Equipment.EquipmentType.Battle);
#else
                obj = new Equipment(false);
#endif
            }
            else
            {
                obj = Equipment.CreateFromEquipmentCode(code);
            }

            var we = new WEquipment(obj, loadout, index);

            if (forceCivilian.HasValue)
                we.SetCivilian(forceCivilian.Value);

            return we;
        }

        /// <summary>
        /// Gets the equipment code string for this equipment.
        /// </summary>
        public string Code
        {
            get
            {
                var obj = Base;
                return obj.CalculateEquipmentCode();
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Slots                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly List<EquipmentIndex> Slots =
        [
            EquipmentIndex.Head,
            EquipmentIndex.Cape,
            EquipmentIndex.Body,
            EquipmentIndex.Gloves,
            EquipmentIndex.Leg,
            EquipmentIndex.WeaponItemBeginSlot,
            EquipmentIndex.Weapon1,
            EquipmentIndex.Weapon2,
            EquipmentIndex.Weapon3,
            EquipmentIndex.Horse,
            EquipmentIndex.HorseHarness,
        ];

        private static bool IsArmorSlot(EquipmentIndex slot) =>
            slot == EquipmentIndex.Head
            || slot == EquipmentIndex.Body
            || slot == EquipmentIndex.Leg
            || slot == EquipmentIndex.Gloves;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Items                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets all equipped items in defined slots as WItem list.
        /// </summary>
        public List<WItem> Items
        {
            get
            {
                var items = new List<WItem>();
                foreach (var slot in Slots)
                {
                    if (_equipment[slot].Item != null)
                        items.Add(new WItem(_equipment[slot].Item));
                }
                return items;
            }
        }

        /// <summary>
        /// Gets the item in the specified equipment slot.
        /// </summary>
        public WItem Get(EquipmentIndex slot)
        {
            var obj = _equipment[slot].Item;
            if (obj == null)
                return null;
            return new WItem(obj);
        }

        /// <summary>
        /// Gets the staged item in the specified equipment slot.
        /// </summary>
        public WItem GetStaged(EquipmentIndex slot)
        {
            var change = TroopEquipBehavior.GetStagedChange(Loadout.Troop, slot, Index);
            if (change == null)
                return null;
            return new WItem(change.ItemId);
        }

        /// <summary>
        /// Sets the item in the specified equipment slot.
        /// </summary>
        public void SetItem(EquipmentIndex slot, WItem item)
        {
            if (item == null)
            {
                _equipment[slot] = new EquipmentElement(null);
                return;
            }

            // Block invalid armor in armor slots
            if (IsArmorSlot(slot) && item.Base?.ArmorComponent == null)
            {
                Log.Warn(
                    $"Attempted to place non-armor item '{item.Base?.Name}' into {slot}; clearing slot instead."
                );
                _equipment[slot] = new EquipmentElement(null);
                return;
            }

            _equipment[slot] = new EquipmentElement(item.Base);
        }

        /// <summary>
        /// Unsets (removes) the item in the specified equipment slot.
        /// </summary>
        public void UnsetItem(EquipmentIndex slot)
        {
            _equipment[slot] = new EquipmentElement(null);
        }

        /// <summary>
        /// Unsets (removes) all items in all defined equipment slots.
        /// </summary>
        public void UnsetAll()
        {
            foreach (var slot in Slots)
                UnsetItem(slot);
        }

        /// <summary>
        /// Unstages all staged changes in all defined equipment slots.
        /// </summary>
        public void UnstageAll()
        {
            foreach (var slot in Slots)
                UnstageItem(slot);
        }

        /// <summary>
        /// Unstages the staged change in the specified equipment slot.
        /// </summary>
        public void UnstageItem(EquipmentIndex slot)
        {
            TroopEquipBehavior.UnstageChange(Loadout.Troop, slot, Index);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Skill Requirements                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the skill requirements for all equipped items.
        /// </summary>
        public Dictionary<SkillObject, int> SkillRequirements
        {
            get
            {
                var reqs = new Dictionary<SkillObject, int>();

                foreach (var slot in Slots)
                {
                    var item = Get(slot);
                    if (item != null && item.RelevantSkill != null)
                    {
                        // Initialize if not present
                        if (!reqs.ContainsKey(item.RelevantSkill))
                            reqs[item.RelevantSkill] = 0;

                        // Update the requirement if this item's difficulty is higher
                        if (item.Difficulty > reqs[item.RelevantSkill])
                            reqs[item.RelevantSkill] = item.Difficulty;
                    }
                }

                return reqs;
            }
        }

        /// <summary>
        /// Gets the skill requirement for a specific skill.
        /// </summary>
        public int ComputeSkillRequirement(SkillObject skill)
        {
            if (SkillRequirements.TryGetValue(skill, out int req))
                return req;
            return 0;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Formation Class                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public FormationClass ComputeFormationClass()
        {
            return (HasNonThrowableRangedWeapons, HasMount) switch
            {
                (true, true) => FormationClass.HorseArcher,
                (true, false) => FormationClass.Ranged,
                (false, true) => FormationClass.Cavalry,
                (false, false) => FormationClass.Infantry,
            };
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsCivilian => _equipment.IsCivilian;

        /// <summary>
        /// Returns true if any equipped item is a ranged weapon.
        /// </summary>
        public bool HasRangedWeapons
        {
            get
            {
                foreach (var slot in Slots)
                {
                    var item = Get(slot);
                    if (item != null && item.IsRangedWeapon)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Returns true if any equipped item is a non-throwable ranged weapon.
        /// </summary>
        public bool HasNonThrowableRangedWeapons
        {
            get
            {
                foreach (var slot in Slots)
                {
                    var item = Get(slot);
                    if (
                        item != null
                        && item.IsRangedWeapon
                        && item.Type != ItemObject.ItemTypeEnum.Thrown
                    )
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Returns true if any equipped item is a mount (horse).
        /// </summary>
        public bool HasMount
        {
            get
            {
                foreach (var slot in Slots)
                {
                    var item = Get(slot);
                    if (item != null && item.IsHorse)
                        return true;
                }
                return false;
            }
        }
    }
}
