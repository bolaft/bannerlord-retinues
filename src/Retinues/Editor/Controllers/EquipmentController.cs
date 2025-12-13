using Retinues.Model.Equipments;
using TaleWorlds.Core;

namespace Retinues.Editor.Controllers
{
    public class EquipmentController : BaseController
    {
        public static void EquipItem(WItem item)
        {
            var slot = State.Instance.Slot;
            var equipped = State.Equipment.GetItem(slot);
            if (equipped == item)
                return; // Already equipped.

            State.Equipment.SetItem(slot, item);

            EventManager.Fire(UIEvent.Item, EventScope.Global);
        }

        public static void UnequipItem(EquipmentIndex slot)
        {
            var equipped = State.Equipment.GetItem(slot);
            if (equipped == null)
                return; // Already empty.

            State.Equipment.SetItem(slot, null);

            EventManager.Fire(UIEvent.Item, EventScope.Global);
        }
    }
}
