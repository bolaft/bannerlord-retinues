using System.Collections.Generic;
using System.Linq;
using Retinues.Model.Characters;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Model.Equipments
{
    public class MEquipmentRoster(MBEquipmentRoster @base, WCharacter owner)
        : MPersistent<MBEquipmentRoster>(@base)
    {
        /// <summary>
        /// The owner character of this roster (for persistence key purposes).
        /// </summary>
        readonly WCharacter _owner = owner;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Persistence                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override string PersistenceKey => $"{_owner?.PersistenceKey}:EquipmentRoster";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Equipments                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<MBList<Equipment>> EquipmentsAttribute =>
            Attribute<MBList<Equipment>>("_equipments");

        public List<MEquipment> Equipments
        {
            get => [.. EquipmentsAttribute.Get().Select(e => new MEquipment(e, _owner))];
            set => EquipmentsAttribute.Set([.. value.Select(e => e.Base).ToList()]);
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
#if BL13
            Add(new MEquipment(new Equipment(Equipment.EquipmentType.Battle), _owner));
            Add(new MEquipment(new Equipment(Equipment.EquipmentType.Civilian), _owner));
#else
            Add(new MEquipment(new Equipment(false), _owner));
            Add(new MEquipment(new Equipment(true), _owner));
#endif
        }
    }
}
