using System;
using System.Collections.Generic;
using Retinues.Model.Characters;
using TaleWorlds.Core;

namespace Retinues.Model.Equipments
{
    public class MEquipment(Equipment @base, WCharacter owner)
        : MPersistent<Equipment>(@base),
            IEquatable<MEquipment>
    {
        /// <summary>
        /// The owner character of this equipment (for persistence key purposes).
        /// </summary>
        readonly WCharacter _owner = owner;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Persistence                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override string PersistenceKey
        {
            get
            {
                int index = _owner?.Equipments.IndexOf(this) ?? -1;
                return $"{_owner?.PersistenceKey}:Equipment[{index}]";
            }
        }

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
            var equipment =
                source == null ? new Equipment() : Equipment.CreateFromEquipmentCode(source.Code);

            if (equipment == null)
                equipment = new Equipment();

            var me = new MEquipment(equipment, owner: owner)
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

        public string Code => Base.CalculateEquipmentCode();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Type                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<Equipment.EquipmentType> EquipmentTypeAttribute =>
            Attribute<Equipment.EquipmentType>("_equipmentType");

        public Equipment.EquipmentType EquipmentType
        {
            get => EquipmentTypeAttribute.Get();
            set => EquipmentTypeAttribute.Set(value);
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
            var element = item == null ? EquipmentElement.Invalid : new EquipmentElement(item.Base);
            Base[index] = element;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equality                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override bool Equals(object obj) => Equals(obj as MEquipment);

        public bool Equals(MEquipment other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (other is null)
                return false;
            return EqualityComparer<Equipment>.Default.Equals(this.Base, other.Base);
        }

        public override int GetHashCode() => Base != null ? Base.GetHashCode() : 0;

        public static bool operator ==(MEquipment left, MEquipment right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left is null || right is null)
                return false;
            return EqualityComparer<Equipment>.Default.Equals(left.Base, right.Base);
        }

        public static bool operator !=(MEquipment left, MEquipment right) => !(left == right);
    }
}
