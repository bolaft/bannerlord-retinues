using System.Collections.Generic;
using TaleWorlds.Core;

namespace Retinues.Core.Game.Wrappers
{
    public class WEquipment(Equipment equipment)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Base                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Equipment _equipment = equipment;

        public Equipment Base => _equipment;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Code                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static WEquipment FromCode(string code)
        {
            Equipment obj;

            if (code is null)
                obj = new Equipment(false);
            else
                obj = Equipment.CreateFromEquipmentCode(code);

            return new WEquipment(obj);
        }

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

        public WItem GetItem(EquipmentIndex slot)
        {
            var obj = _equipment[slot].Item;
            if (obj == null)
                return null;
            return new WItem(obj);
        }

        public void SetItem(EquipmentIndex slot, WItem item)
        {
            _equipment[slot] = new EquipmentElement(item?.Base);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Skill Requirements                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public Dictionary<SkillObject, int> SkillRequirements
        {
            get
            {
                var reqs = new Dictionary<SkillObject, int>();

                foreach (var slot in Slots)
                {
                    var item = GetItem(slot);
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

        public int GetSkillRequirement(SkillObject skill)
        {
            if (SkillRequirements.TryGetValue(skill, out int req))
                return req;
            return 0;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool HasRangedWeapons
        {
            get
            {
                foreach (var slot in Slots)
                {
                    var item = GetItem(slot);
                    if (item != null && item.IsRangedWeapon)
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
                    var item = GetItem(slot);
                    if (item != null && item.IsHorse)
                        return true;
                }
                return false;
            }
        }
    }
}
