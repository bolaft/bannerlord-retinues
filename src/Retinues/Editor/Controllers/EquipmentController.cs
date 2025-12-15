using Retinues.Model.Equipments;
using TaleWorlds.Core;

namespace Retinues.Editor.Controllers
{
    public class EquipmentController : BaseController
    {
        public static void EquipItem(WItem item)
        {
            if (State.Equipment == null)
                return;

            var slot = State.Instance.Slot;
            var equipped = State.Equipment.GetItem(slot);
            if (equipped == item)
                return;

            State.Equipment.SetItem(slot, item);

            EventManager.Fire(UIEvent.Item, EventScope.Global);
        }

        public static void UnequipItem(EquipmentIndex slot, bool fireEvent = true)
        {
            if (State.Equipment == null)
                return;

            var equipped = State.Equipment.GetItem(slot);
            if (equipped == null)
                return;

            State.Equipment.SetItem(slot, null);

            if (slot == EquipmentIndex.Horse)
                UnequipItem(EquipmentIndex.HorseHarness, fireEvent: false);

            if (fireEvent)
                EventManager.Fire(UIEvent.Item, EventScope.Global);
        }
    }
}
