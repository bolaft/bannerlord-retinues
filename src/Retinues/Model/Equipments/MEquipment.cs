using System;
using System.Collections.Generic;
using Retinues.Model.Characters;
using TaleWorlds.Core;

namespace Retinues.Model.Equipments
{
    public class MEquipment : MBase<Equipment>, IEquatable<MEquipment>
    {
        readonly WCharacter _owner;

        public MEquipment(Equipment @base, WCharacter owner)
            : base(@base)
        {
            _owner = owner;

            // Make sure serializable attributes exist even if nobody accessed the properties yet.
            EnsureSerializableAttributes();
        }

        public void EnsureSerializableAttributes()
        {
            _ = EquipmentTypeAttribute;
            _ = CodeAttribute;
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
                    _owner?.TouchEquipments();
                },
                serializable: true,
                targetName: "Code"
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Type                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<Equipment.EquipmentType> EquipmentTypeAttribute =>
            Attribute<Equipment.EquipmentType>("_equipmentType", serializable: true);

        public Equipment.EquipmentType EquipmentType
        {
            get => EquipmentTypeAttribute.Get();
            set
            {
                EquipmentTypeAttribute.Set(value);
                _owner?.TouchEquipments();
            }
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

            _owner?.TouchEquipments();
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
