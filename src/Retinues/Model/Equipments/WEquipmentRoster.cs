using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Model.Equipments
{
    public class WEquipmentRoster(MBEquipmentRoster @base)
        : WBase<WEquipmentRoster, MBEquipmentRoster>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Raw List Access                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Underlying MBEquipmentRoster field; not persisted directly.
        MAttribute<MBList<Equipment>> EquipmentsAttribute =>
            Attribute<MBList<Equipment>>("_equipments", persistent: false);

        MBList<Equipment> RawEquipments
        {
            get => EquipmentsAttribute.Get();
            set => EquipmentsAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Public API                      //
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
        //                    Equipment Codes                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string GetEquipmentCodes()
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

        public void SetEquipmentCodes(string value)
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
    }
}
