using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Model.Characters;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Model.Equipments
{
    public class MEquipmentRoster(MBEquipmentRoster @base, WCharacter owner)
        : MBase<MBEquipmentRoster>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Equipments                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<MBList<Equipment>> EquipmentsAttribute =>
            Attribute<MBList<Equipment>>("_equipments");

        MAttribute<List<MEquipment>> EquipmentsMAttribute =>
            Attribute<List<MEquipment>>(
                getter: _ =>
                    [
                        .. (EquipmentsAttribute.Get() ?? []).Select(
                            (e, i) => new MEquipment(e, owner)
                        ),
                    ],
                setter: (_, value) =>
                {
                    EquipmentsAttribute.Set([.. (value ?? []).Select(e => e.Base).ToList()]);
                }
            );

        public List<MEquipment> Equipments
        {
            get => EquipmentsMAttribute.Get();
            set
            {
                owner.TouchEquipments();
                EquipmentsMAttribute.Set(value);
            }
        }

        public void Add(MEquipment equipment)
        {
            var list = Equipments.ToList();
            list.Add(equipment);
            Equipments = list;
        }

        public void Remove(MEquipment equipment)
        {
            var list = Equipments.ToList();
            list.Remove(equipment);
            Equipments = list;
        }

        public void Copy(MEquipmentRoster source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var newList = new List<MEquipment>();

            var sourceEquipments = source.Equipments ?? [];

            for (int i = 0; i < sourceEquipments.Count; i++)
            {
                var src = sourceEquipments[i];

                // Keep slot structure identical: if source has an "empty" slot,
                // we create a matching empty equipment; otherwise clone from it.
                var clone =
                    src == null
                        ? MEquipment.Create(owner, civilian: false, source: null)
                        : MEquipment.Create(owner, src.IsCivilian, src);

                newList.Add(clone);
            }

            Equipments = newList;
        }

        public void Reset()
        {
            Equipments = [];

#if BL13
            Add(new MEquipment(new Equipment(Equipment.EquipmentType.Battle), owner));
            Add(new MEquipment(new Equipment(Equipment.EquipmentType.Civilian), owner));
#else
            Add(new MEquipment(new Equipment(false), owner));
            Add(new MEquipment(new Equipment(true), owner));
#endif
        }
    }
}
