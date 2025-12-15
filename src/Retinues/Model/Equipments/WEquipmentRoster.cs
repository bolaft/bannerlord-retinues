using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Model.Equipments
{
    public class WEquipmentRoster(MBEquipmentRoster @base)
        : WBase<WEquipmentRoster, MBEquipmentRoster>(@base)
    {
        MAttribute<MBList<Equipment>> EquipmentsAttribute =>
            Attribute<MBList<Equipment>>("_equipments", persistent: false);

        public MBList<Equipment> RawEquipments
        {
            get => EquipmentsAttribute.Get() ?? [];
            set => EquipmentsAttribute.Set(value ?? []);
        }
    }
}
