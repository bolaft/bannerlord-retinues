using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Framework.Model;
using Retinues.Framework.Model.Attributes;
using TaleWorlds.Core;

namespace Retinues.Domain.Equipments.Models
{
    public class MEquipment(Equipment @base, WCharacter owner) : MBase<Equipment>(@base)
    {
        private static readonly int SlotCount = (int)EquipmentIndex.NumEquipmentSetSlots;

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
            MEquipment source = null
        )
        {
            owner.TouchEquipments();

            var equipment =
                source == null ? new Equipment() : Equipment.CreateFromEquipmentCode(source.Code);

            if (equipment == null)
                equipment = new Equipment();

            var me = new MEquipment(equipment, owner)
            {
                EquipmentType = civilian
                    ? Equipment.EquipmentType.Civilian
                    : Equipment.EquipmentType.Battle,
            };
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

                    // Copy only real slots: [0..NumEquipmentSetSlots-1].
                    for (int i = 0; i < SlotCount; i++)
                    {
                        var idx = (EquipmentIndex)i;
                        Base[idx] = src[idx];
                    }
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
                // Only iterate real equipment slots.
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
