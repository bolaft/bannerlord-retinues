using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Framework.Model;
using Retinues.Framework.Model.Attributes;
using TaleWorlds.Core;

namespace Retinues.Domain.Equipments.Models
{
    public class MEquipment(Equipment @base, WCharacter owner, MEquipmentRoster roster = null)
        : MBase<Equipment>(@base)
    {
        private static readonly int SlotCount = (int)EquipmentIndex.NumEquipmentSetSlots;

        private readonly MEquipmentRoster _roster = roster;

        private static bool IsValidSlot(EquipmentIndex index)
        {
            int i = (int)index;
            return i >= 0 && i < SlotCount;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Creation                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static MEquipment Create(
            WCharacter owner,
            bool civilian = false,
            MEquipment source = null,
            MEquipmentRoster roster = null
        )
        {
            owner.TouchEquipments();

            var equipment =
                source == null ? new Equipment() : Equipment.CreateFromEquipmentCode(source.Code);

            if (equipment == null)
                equipment = new Equipment();

            var me = new MEquipment(equipment, owner, roster)
            {
                EquipmentType = civilian
                    ? Equipment.EquipmentType.Civilian
                    : Equipment.EquipmentType.Battle,
            };

            roster?.InvalidateItemCountsCache();
            return me;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Code                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Code => CodeAttribute.Get();

        MAttribute<string> CodeAttribute =>
            Attribute(
                getter: _ => Base.CalculateEquipmentCode(),
                setter: (_, code) =>
                {
                    if (string.IsNullOrEmpty(code))
                        return;

                    var src = Equipment.CreateFromEquipmentCode(code);
                    if (src == null)
                        return;

                    owner.TouchEquipments();

                    // Copy only real slots: [0..NumEquipmentSetSlots-1].
                    for (int i = 0; i < SlotCount; i++)
                    {
                        var idx = (EquipmentIndex)i;
                        Base[idx] = src[idx];
                    }

                    _roster?.InvalidateItemCountsCache();
                }
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Type                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<Equipment.EquipmentType> EquipmentTypeAttribute =>
            Attribute<Equipment.EquipmentType>("_equipmentType", persistent: false);

        public Equipment.EquipmentType EquipmentType
        {
            get => EquipmentTypeAttribute.Get();
            set
            {
                owner.TouchEquipments();
                EquipmentTypeAttribute.Set(value);
            }
        }

        MAttribute<bool> IsCivilianAttribute =>
            Attribute(
                getter: _ => EquipmentType == Equipment.EquipmentType.Civilian,
                setter: (_, isCivilian) =>
                    EquipmentType = isCivilian
                        ? Equipment.EquipmentType.Civilian
                        : Equipment.EquipmentType.Battle
            );

        public bool IsCivilian
        {
            get => IsCivilianAttribute.Get();
            set => IsCivilianAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Items                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public IEnumerable<WItem> Items
        {
            get
            {
                for (int i = 0; i < SlotCount; i++)
                {
                    var item = Get((EquipmentIndex)i);
                    if (item != null)
                        yield return item;
                }
            }
        }

        public WItem Get(EquipmentIndex index)
        {
            if (!IsValidSlot(index))
                return null;

            var element = Base[index];
            var item = element.Item;
            return item == null ? null : WItem.Get(item);
        }

        public void Set(EquipmentIndex index, WItem item)
        {
            if (!IsValidSlot(index))
                return;

            owner.TouchEquipments();
            var element = item == null ? EquipmentElement.Invalid : new EquipmentElement(item.Base);
            Base[index] = element;

            _roster?.InvalidateItemCountsCache();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Roster                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Slot-aware: returns true if setting this slot to item does not require increasing
        /// the roster stock for this item (because another equipment already needs that many).
        /// </summary>
        public bool IsAvailableInRoster(EquipmentIndex slot, WItem item)
        {
            if (item == null)
                return true;

            if (_roster == null)
                return false;

            string id = item.StringId;
            if (string.IsNullOrEmpty(id))
                return false;

            var old = Get(slot);
            if (old != null && old.StringId == id)
            {
                // No net change for this item.
                return true;
            }

            int thisCount = CountInThisEquipment(id);
            int newCount = thisCount + 1;

            int otherMax = _roster.GetMaxCountExcludingEquipment(Base, id);

            // If someone else already requires >= newCount, roster already has enough copies.
            return otherMax >= newCount;
        }

        /// <summary>
        /// Non-slot-aware convenience: assumes you are adding one more copy of item somewhere.
        /// Prefer the slot-aware overload in equip actions.
        /// </summary>
        public bool IsAvailableInRoster(WItem item)
        {
            if (item == null)
                return true;

            if (_roster == null)
                return false;

            string id = item.StringId;
            if (string.IsNullOrEmpty(id))
                return false;

            int thisCount = CountInThisEquipment(id);
            int otherMax = _roster.GetMaxCountExcludingEquipment(Base, id);

            return otherMax >= thisCount + 1;
        }

        private int CountInThisEquipment(string itemId)
        {
            int count = 0;

            for (int i = 0; i < SlotCount; i++)
            {
                var w = Get((EquipmentIndex)i);
                if (w != null && w.StringId == itemId)
                    count++;
            }

            return count;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void MarkAllAttributesDirty()
        {
            EnsureAttributesCreated();

            foreach (var obj in _attributes.Values)
            {
                if (obj is IMAttribute attr)
                    attr.MarkDirty();
            }
        }
    }
}
