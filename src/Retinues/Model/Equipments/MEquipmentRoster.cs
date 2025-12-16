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
        private readonly WCharacter _owner = owner;

        public List<MEquipment> Equipments
        {
            get
            {
                var list = Reflection.GetFieldValue<MBList<Equipment>>(Base, "_equipments") ?? [];
                return [.. list.Select(e => new MEquipment(e, _owner))];
            }
            set
            {
                // Ensure non-null list
                value ??= [];

                // Touch the serialized attribute to ensure persistence.
                _owner.MarkEquipmentsDirty();

                var mbList = new MBList<Equipment>();

                foreach (var me in value)
                    mbList.Add(me.Base);

                Reflection.SetFieldValue(Base, "_equipments", mbList);
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
            Equipments = [.. source.Equipments];
        }

        public void Reset()
        {
            Equipments = [];
            Add(new MEquipment(new Equipment(Equipment.EquipmentType.Battle), _owner));
            Add(new MEquipment(new Equipment(Equipment.EquipmentType.Civilian), _owner));
        }
    }
}
