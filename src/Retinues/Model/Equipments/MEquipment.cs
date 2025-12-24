using System;
using Retinues.Model.Characters;
using TaleWorlds.Core;

namespace Retinues.Model.Equipments
{
    public class MEquipment(Equipment @base, WCharacter owner) : MBase<Equipment>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Creation                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Creates a new equipment for the given owner, optionally copying from a source equipment.
        /// </summary>
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

                    // Copy all slots defensively (Bannerlord versions differ a bit).
                    foreach (EquipmentIndex idx in Enum.GetValues(typeof(EquipmentIndex)))
                    {
                        try
                        {
                            var element = src[idx];
                            Base[idx] = element;
                        }
                        catch { }
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

        public WItem Get(EquipmentIndex index)
        {
            var element = Base[index];
            var item = element.Item;
            return item == null ? null : WItem.Get(item);
        }

        public void Set(EquipmentIndex index, WItem item)
        {
            owner.TouchEquipments();
            var element = item == null ? EquipmentElement.Invalid : new EquipmentElement(item.Base);
            Base[index] = element;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Items                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void MarkAllAttributesDirty()
        {
            // Ensure all IMAttribute properties (CodeAttribute, IsCivilianAttribute, and any
            // future ones) are instantiated before we try to mark them dirty.
            EnsureAttributesCreated();

            foreach (var obj in _attributes.Values)
            {
                if (obj is IMAttribute attr)
                    attr.MarkDirty();
            }
        }
    }
}
