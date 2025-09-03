using System.Collections.Generic;
using TaleWorlds.Core;

namespace CustomClanTroops.Wrappers.Objects
{
    public class EquipmentWrapper(Equipment equipment)
    {
        // =========================================================================
        // Base
        // =========================================================================

        private readonly Equipment _equipment = equipment;

        public Equipment Base => _equipment;

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

        public List<ItemWrapper> Items
        {
            get
            {
                var items = new List<ItemWrapper>();
                foreach (var slot in Slots)
                {
                    if (_equipment[slot].Item != null)
                        items.Add(new ItemWrapper(_equipment[slot].Item));
                }
                return items;
            }
        }

        // =========================================================================
        // Slots
        // =========================================================================

        public ItemWrapper WeaponItemBeginSlot
        {
            get => new(_equipment[EquipmentIndex.WeaponItemBeginSlot].Item);
            set => _equipment[EquipmentIndex.WeaponItemBeginSlot] = new EquipmentElement(value.Base);
        }
        public ItemWrapper Weapon1
        {
            get => new(_equipment[EquipmentIndex.Weapon1].Item);
            set => _equipment[EquipmentIndex.Weapon1] = new EquipmentElement(value.Base);
        }
        public ItemWrapper Weapon2
        {
            get => new(_equipment[EquipmentIndex.Weapon2].Item);
            set => _equipment[EquipmentIndex.Weapon2] = new EquipmentElement(value.Base);
        }
        public ItemWrapper Weapon3
        {
            get => new(_equipment[EquipmentIndex.Weapon3].Item);
            set => _equipment[EquipmentIndex.Weapon3] = new EquipmentElement(value.Base);
        }
        public ItemWrapper Head
        {
            get => new(_equipment[EquipmentIndex.Head].Item);
            set => _equipment[EquipmentIndex.Head] = new EquipmentElement(value.Base);
        }
        public ItemWrapper Cape
        {
            get => new(_equipment[EquipmentIndex.Cape].Item);
            set => _equipment[EquipmentIndex.Cape] = new EquipmentElement(value.Base);
        }
        public ItemWrapper Body
        {
            get => new(_equipment[EquipmentIndex.Body].Item);
            set => _equipment[EquipmentIndex.Body] = new EquipmentElement(value.Base);
        }
        public ItemWrapper Gloves
        {
            get => new(_equipment[EquipmentIndex.Gloves].Item);
            set => _equipment[EquipmentIndex.Gloves] = new EquipmentElement(value.Base);
        }
        public ItemWrapper Leg
        {
            get => new(_equipment[EquipmentIndex.Leg].Item);
            set => _equipment[EquipmentIndex.Leg] = new EquipmentElement(value.Base);
        }
        public ItemWrapper Horse
        {
            get => new(_equipment[EquipmentIndex.Horse].Item);
            set => _equipment[EquipmentIndex.Horse] = new EquipmentElement(value.Base);
        }
        public ItemWrapper HorseHarness
        {
            get => new(_equipment[EquipmentIndex.HorseHarness].Item);
            set => _equipment[EquipmentIndex.HorseHarness] = new EquipmentElement(value.Base);
        }

        public ItemWrapper GetItem(EquipmentIndex slot)
        {
            return new ItemWrapper(_equipment[slot].Item);
        }

        public void SetItem(EquipmentIndex slot, ItemWrapper item)
        {
            _equipment[slot] = new EquipmentElement(item?.Base);
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