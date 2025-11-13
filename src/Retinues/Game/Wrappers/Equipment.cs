using System.Collections.Generic;
using Retinues.Features.Staging;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for Equipment.
    /// </summary>
    [SafeClass]
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

        private static bool IsArmorSlot(EquipmentIndex slot) =>
            slot == EquipmentIndex.Head
            || slot == EquipmentIndex.Body
            || slot == EquipmentIndex.Leg
            || slot == EquipmentIndex.Gloves;

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Staging Preview                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Get a preview of this equipment with staged changes applied.
        /// </summary>
        public Equipment StagingPreview()
        {
            // clone so we don't mutate the real thing
            var eq = new Equipment(Base);

            // apply staged equip (if any)
            var pending = EquipStagingBehavior.Get(Loadout.Troop);

            if (pending == null)
                return eq; // nothing staged

            foreach (var p in pending)
            {
                if (p.EquipmentIndex != Index)
                    continue; // not for this equipment

                var item = MBObjectManager.Instance.GetObject<ItemObject>(p.ItemId);
                if (item != null)
                    eq[p.Slot] = new EquipmentElement(item);
            }

            return eq;
        }
    }
}
