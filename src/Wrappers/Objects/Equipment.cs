using System.Collections.Generic;
using TaleWorlds.Core;

namespace CustomClanTroops.Wrappers.Objects
{
    public class WEquipment(Equipment equipment) : IWrapper
    {
        // =========================================================================
        // Base
        // =========================================================================

        private readonly Equipment _equipment = equipment;

        public object Base => _equipment;

        // =========================================================================
        // Slot List
        // =========================================================================

        public static readonly List<EquipmentIndex> Slots =
        [
            EquipmentIndex.Head, EquipmentIndex.Cape, EquipmentIndex.Body, EquipmentIndex.Gloves, EquipmentIndex.Leg,
            EquipmentIndex.WeaponItemBeginSlot, EquipmentIndex.Weapon1, EquipmentIndex.Weapon2, EquipmentIndex.Weapon3,
            EquipmentIndex.Horse, EquipmentIndex.HorseHarness,
        ];

        // =========================================================================
        // Items
        // =========================================================================

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

        // =========================================================================
        // Slots
        // =========================================================================

        public WItem WeaponItemBeginSlot
        {
            get => new(_equipment[EquipmentIndex.WeaponItemBeginSlot].Item);
            set => _equipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement((ItemObject)value.Base);
        }
        public WItem Weapon1
        {
            get => new(_equipment[EquipmentIndex.Weapon1].Item);
            set => _equipment[EquipmentIndex.Weapon1] = new EquipmentElement((ItemObject)value.Base);
        }
        public WItem Weapon2
        {
            get => new(_equipment[EquipmentIndex.Weapon2].Item);
            set => _equipment[EquipmentIndex.Weapon2] = new EquipmentElement((ItemObject)value.Base);
        }
        public WItem Weapon3
        {
            get => new(_equipment[EquipmentIndex.Weapon3].Item);
            set => _equipment[EquipmentIndex.Weapon3] = new EquipmentElement((ItemObject)value.Base);
        }
        public WItem Head
        {
            get => new(_equipment[EquipmentIndex.Head].Item);
            set => _equipment[EquipmentIndex.Head] = new EquipmentElement((ItemObject)value.Base);
        }
        public WItem Cape
        {
            get => new(_equipment[EquipmentIndex.Cape].Item);
            set => _equipment[EquipmentIndex.Cape] = new EquipmentElement((ItemObject)value.Base);
        }
        public WItem Body
        {
            get => new(_equipment[EquipmentIndex.Body].Item);
            set => _equipment[EquipmentIndex.Body] = new EquipmentElement((ItemObject)value.Base);
        }
        public WItem Gloves
        {
            get => new(_equipment[EquipmentIndex.Gloves].Item);
            set => _equipment[EquipmentIndex.Gloves] = new EquipmentElement((ItemObject)value.Base);
        }
        public WItem Leg
        {
            get => new(_equipment[EquipmentIndex.Leg].Item);
            set => _equipment[EquipmentIndex.Leg] = new EquipmentElement((ItemObject)value.Base);
        }
        public WItem Horse
        {
            get => new(_equipment[EquipmentIndex.Horse].Item);
            set => _equipment[EquipmentIndex.Horse] = new EquipmentElement((ItemObject)value.Base);
        }
        public WItem HorseHarness
        {
            get => new(_equipment[EquipmentIndex.HorseHarness].Item);
            set => _equipment[EquipmentIndex.HorseHarness] = new EquipmentElement((ItemObject)value.Base);
        }

        public WItem GetItem(EquipmentIndex slot)
        {
            return new WItem(_equipment[slot].Item);
        }

        public void SetItem(EquipmentIndex slot, WItem item)
        {
            _equipment[slot] = new EquipmentElement((ItemObject)item?.Base);
        }

        // =========================================================================
        // Skill Requirements
        // =========================================================================

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
    }
}