using System.Collections.Generic;
using Retinues.Utils;
using TaleWorlds.Core;

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for Equipment.
    /// </summary>
    [SafeClass]
    public class WEquipment(Equipment equipment, WLoadout loadout, int index)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Equipment _equipment = equipment;
        public Equipment Base => _equipment;

        public WEquipment(Equipment equipment, WCharacter troop, int index)
            : this(equipment, troop.Loadout, index) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Loadout                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WLoadout Loadout { get; private set; } = loadout;
        public int Index { get; private set; } = index;
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
            { /* best-effort */
            }
#else
            if (_equipment.IsCivilian == makeCivilian)
                return;

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
        /// Create WEquipment from an equipment code.
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

        public string Code => Base.CalculateEquipmentCode();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Slots                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// All defined equipment slots.
        /// </summary>
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

        /// <summary>A
        /// ll non-null items in defined slots.
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
        /// Get the item in the given slot.
        /// </summary>
        public WItem Get(EquipmentIndex slot)
        {
            var obj = _equipment[slot].Item;
            return obj == null ? null : new WItem(obj);
        }

        /// <summary>
        /// Set the item in the given slot.
        /// </summary>
        public void SetItem(EquipmentIndex slot, WItem item)
        {
            _equipment[slot] = new EquipmentElement(item?.Base);
        }

        /// <summary>
        /// Unset the item in the given slot.
        /// </summary>
        public void UnsetItem(EquipmentIndex slot)
        {
            _equipment[slot] = new EquipmentElement(null);
        }

        /// <summary>
        /// Unset all items in defined slots.
        /// </summary>
        public void UnsetAll()
        {
            foreach (var slot in Slots)
                UnsetItem(slot);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Skill Requirements                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Skill requirements for this equipment.
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
                        if (!reqs.ContainsKey(item.RelevantSkill))
                            reqs[item.RelevantSkill] = 0;
                        if (item.Difficulty > reqs[item.RelevantSkill])
                            reqs[item.RelevantSkill] = item.Difficulty;
                    }
                }
                return reqs;
            }
        }

        /// <summary>
        /// Compute the skill requirement for a given skill.
        /// </summary>
        public int ComputeSkillRequirement(SkillObject skill) =>
            SkillRequirements.TryGetValue(skill, out int req) ? req : 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Formation Class                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Compute the formation class for this equipment.
        /// </summary>
        public FormationClass ComputeFormationClass() =>
            (HasNonThrowableRangedWeapons, HasMount) switch
            {
                (true, true) => FormationClass.HorseArcher,
                (true, false) => FormationClass.Ranged,
                (false, true) => FormationClass.Cavalry,
                _ => FormationClass.Infantry,
            };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsCivilian => _equipment.IsCivilian;

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
