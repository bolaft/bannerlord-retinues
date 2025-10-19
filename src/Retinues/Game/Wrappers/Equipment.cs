using System.Collections.Generic;
using Retinues.Utils;
using Retinues.Features.Upgrade.Behaviors;
using TaleWorlds.Core;

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for Equipment, provides helpers for slot/item access, skill requirements, and equipment code serialization.
    /// </summary>
    [SafeClass(SwallowByDefault = false)]
    public class WEquipment(Equipment equipment, WLoadout loadout, int index)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Equipment _equipment = equipment;
        public Equipment Base => _equipment;

        public WEquipment(Equipment equipment, WCharacter troop, int index)
            : this(equipment, troop.Loadout, index) { }

        public WEquipment(Equipment equipment, WAgent agent, int index)
            : this(equipment, agent.Character, index) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Loadout                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WLoadout Loadout { get; private set; } = loadout;
        public int Index { get; private set; } = index;
        public EquipmentCategory Category => Loadout.GetCategory(Index);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Code                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Creates a WEquipment from an equipment code string.
        /// </summary>
        public static WEquipment FromCode(string code, WLoadout loadout, int index)
        {
            bool civilian = loadout.GetCategory(index) == EquipmentCategory.Civilian;

            Equipment obj;

            if (code is null)
            {
#if BL13
                obj = new Equipment(
                    civilian ? Equipment.EquipmentType.Civilian : Equipment.EquipmentType.Battle
                );
#else
                obj = new Equipment(civilian);
#endif
            }
            else
            {
                var tmp = Equipment.CreateFromEquipmentCode(code);

#if BL13
                // Force the equipment type regardless of what the code implied
                try
                {
                    var want = civilian
                        ? Equipment.EquipmentType.Civilian
                        : Equipment.EquipmentType.Battle;
                    Reflector.SetFieldValue(tmp, "_equipmentType", want);
                    obj = tmp;
                }
                catch
                {
                    obj = tmp; // best effort
                }
#else
                // BL12 has no public setter; rebuild a fresh one of the right type and copy items
                if (tmp.IsCivilian == civilian)
                {
                    obj = tmp;
                }
                else
                {
                    obj = new Equipment(civilian);
                    // copy all slots
                    foreach (var slot in WEquipment.Slots)
                        obj[slot] = tmp[slot];
                }
#endif
            }

            return new WEquipment(obj, loadout, index);
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
            _equipment[slot] = new EquipmentElement(item?.Base);
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
