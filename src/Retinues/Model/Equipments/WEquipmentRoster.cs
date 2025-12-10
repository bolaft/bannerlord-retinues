using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Model.Equipments
{
    public class WEquipmentRoster(MBEquipmentRoster @base)
        : WBase<WEquipmentRoster, MBEquipmentRoster>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Raw list access                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Underlying MBEquipmentRoster field; not persisted directly.
        MAttribute<MBList<Equipment>> EquipmentsAttribute =>
            Attribute<MBList<Equipment>>("_equipments");

        MBList<Equipment> RawEquipments
        {
            get => EquipmentsAttribute.Get();
            set => EquipmentsAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Public surface                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public int Count
        {
            get
            {
                var list = RawEquipments;
                return list?.Count ?? 0;
            }
        }

        /// <summary>
        /// All equipment sets as fresh MEquipment instances.
        /// </summary>
        public IReadOnlyList<MEquipment> Equipments
        {
            get
            {
                var list = RawEquipments;
                if (list == null || list.Count == 0)
                    return [];

                var result = new List<MEquipment>(list.Count);
                foreach (var equipment in list)
                {
                    if (equipment == null)
                        continue;

                    result.Add(new MEquipment(equipment));
                }

                return result;
            }
        }

        public MEquipment Get(int index)
        {
            var list = RawEquipments;
            if (list == null || index < 0 || index >= list.Count)
                return null;

            var equipment = list[index];
            return equipment == null ? null : new MEquipment(equipment);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Code-based persistence              //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Persisted as "code0;code1;code2;..."
        // Getter encodes RawEquipments; setter decodes and rewrites RawEquipments.

        MAttribute<string> _equipmentCodesAttribute;

        MAttribute<string> EquipmentCodesAttribute
        {
            get
            {
                if (_equipmentCodesAttribute != null)
                    return _equipmentCodesAttribute;

                _equipmentCodesAttribute = new MAttribute<string>(
                    baseInstance: Base,
                    getter: _ => GetEquipmentCodes(),
                    setter: (_, value) => SetEquipmentCodes(value),
                    targetName: "ret_equipment_codes",
                    persistent: true
                );

                // Keep it in the attribute map like the others.
                _attributes["ret_equipment_codes"] = _equipmentCodesAttribute;

                return _equipmentCodesAttribute;
            }
        }

        public string EquipmentCodes
        {
            get => EquipmentCodesAttribute.Get();
            set => EquipmentCodesAttribute.Set(value);
        }

        string GetEquipmentCodes()
        {
            var list = RawEquipments;
            if (list == null || list.Count == 0)
                return string.Empty;

            var codes = new List<string>(list.Count);
            foreach (var equipment in list)
            {
                if (equipment == null)
                {
                    // Preserve slot position with an empty code.
                    codes.Add(string.Empty);
                    continue;
                }

                codes.Add(equipment.CalculateEquipmentCode() ?? string.Empty);
            }

            return string.Join(";", codes);
        }

        void SetEquipmentCodes(string value)
        {
            var list = new MBList<Equipment>();

            if (!string.IsNullOrEmpty(value))
            {
                var parts = value.Split([';'], StringSplitOptions.None);
                foreach (var part in parts)
                {
                    if (string.IsNullOrEmpty(part))
                    {
                        list.Add(null);
                        continue;
                    }

                    var equipment = Equipment.CreateFromEquipmentCode(part);
                    list.Add(equipment);
                }
            }

            RawEquipments = list;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Mutations                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Inserts the given set (by code) at the given index (0 by default).
        /// Mutates via the persistent EquipmentCodes attribute.
        /// </summary>
        public void Add(MEquipment equipment, int index = 0)
        {
            if (equipment == null)
                return;

            var codesString = EquipmentCodesAttribute.Get();
            var codes = string.IsNullOrEmpty(codesString)
                ? []
                : codesString.Split([';'], StringSplitOptions.None).ToList();

            var code = equipment.Code;
            if (string.IsNullOrEmpty(code))
                return;

            if (index < 0 || index > codes.Count)
                index = codes.Count;

            codes.Insert(index, code);

            EquipmentCodesAttribute.Set(string.Join(";", codes));
        }

        /// <summary>
        /// Removes the set at the given index (if valid), via the codes string.
        /// </summary>
        public void Remove(int index)
        {
            var codesString = EquipmentCodesAttribute.Get();
            if (string.IsNullOrEmpty(codesString))
                return;

            var codes = codesString.Split([';'], StringSplitOptions.None).ToList();
            if (index < 0 || index >= codes.Count)
                return;

            codes.RemoveAt(index);

            var newString = codes.Count == 0 ? string.Empty : string.Join(";", codes);
            EquipmentCodesAttribute.Set(newString);
        }
    }
}
