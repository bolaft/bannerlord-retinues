using System.Collections.Generic;
using Retinues.Model.Characters;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Model.Equipments
{
    public class MEquipment(Equipment @base, WCharacter owner) : MBase<Equipment>(@base)
    {
        private readonly WCharacter _owner = owner;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Creation                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

            var me = new MEquipment(equipment, owner)
            {
                EquipmentType = civilian
                    ? Equipment.EquipmentType.Civilian
                    : Equipment.EquipmentType.Battle,
            };
            return me;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Code => Base.CalculateEquipmentCode() ?? string.Empty;

        public Equipment.EquipmentType EquipmentType
        {
            get => Reflection.GetFieldValue<Equipment.EquipmentType>(Base, "_equipmentType");
            set
            {
                // Touch the serialized attribute to ensure persistence.
                _owner.MarkEquipmentsDirty();

                Reflection.SetFieldValue(Base, "_equipmentType", value);
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
        //                        Items API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WItem GetItem(EquipmentIndex index)
        {
            var element = Base[index];
            var item = element.Item;
            return item == null ? null : WItem.Get(item);
        }

        public void SetItem(EquipmentIndex index, WItem item)
        {
            // Touch the serialized attribute to ensure persistence.
            _owner.MarkEquipmentsDirty();

            var element = item == null ? EquipmentElement.Invalid : new EquipmentElement(item.Base);
            Base[index] = element;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Serialization                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Serialize()
        {
            var data = new Dictionary<string, string>
            {
                { "Code", Code },
                {
                    "IsCivilian",
                    EquipmentType == Equipment.EquipmentType.Civilian ? "true" : "false"
                },
            };

            return Serialization.SerializeDictionary(data);
        }

        public static MEquipment Deserialize(string str, WCharacter owner)
        {
            var data = Serialization.DeserializeDictionary(str);

            data.TryGetValue("Code", out string code);
            data.TryGetValue("IsCivilian", out string isCivilian);

            var type =
                isCivilian != null && isCivilian == "true"
                    ? Equipment.EquipmentType.Civilian
                    : Equipment.EquipmentType.Battle;

            var e = Equipment.CreateFromEquipmentCode(code) ?? new Equipment();

            return new MEquipment(e, owner) { EquipmentType = type };
        }
    }
}
