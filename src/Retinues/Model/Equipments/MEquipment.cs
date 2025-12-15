using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Retinues.Model.Characters;
using TaleWorlds.Core;

namespace Retinues.Model.Equipments
{
    public class MEquipment(Equipment @base) : MBase<Equipment>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Owner                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter Owner { get; set; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Code => Base.CalculateEquipmentCode();

        public Equipment.EquipmentType EquipmentType
        {
            get => Attribute<Equipment.EquipmentType>("_equipmentType").Get();
            set => Attribute<Equipment.EquipmentType>("_equipmentType").Set(value);
        }

        public bool IsCivilian
        {
            get => EquipmentType == Equipment.EquipmentType.Civilian;
            set =>
                EquipmentType = value
                    ? Equipment.EquipmentType.Civilian
                    : Equipment.EquipmentType.Battle;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Items API                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the item equipped in the given slot.
        /// </summary>
        public WItem GetItem(EquipmentIndex index)
        {
            var element = Base[index];
            var item = element.Item;
            return item == null ? null : WItem.Get(item);
        }

        /// <summary>
        /// Sets the item equipped in the given slot.
        /// </summary>
        public void SetItem(EquipmentIndex index, WItem item)
        {
            var element = item == null ? EquipmentElement.Invalid : new EquipmentElement(item.Base);

            Base[index] = element;

            // Notify owner to persist changes.
            Owner?.EquipmentCodesAttribute.Touch();
        }

        /// <summary>
        /// Enumerates all items in this equipment.
        /// </summary>
        public IEnumerable<WItem> Items()
        {
            for (int i = 0; i < Equipment.EquipmentSlotLength; i++)
            {
                var idx = (EquipmentIndex)i;
                yield return GetItem(idx);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equality                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool Equals(MEquipment other) => other != null && ReferenceEquals(Base, other.Base);

        public override bool Equals(object obj) => obj is MEquipment other && Equals(other);

        public override int GetHashCode() => Base == null ? 0 : RuntimeHelpers.GetHashCode(Base);
    }
}
