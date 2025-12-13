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

        public static void UnequipItem(EquipmentIndex slot, bool fireEvent = true)
        {
            var equipped = State.Equipment.GetItem(slot);
            if (equipped == null)
                return; // Already empty.

            State.Equipment.SetItem(slot, null);

            // If we unequip the horse, also unequip the harness.
            if (slot == EquipmentIndex.Horse)
                UnequipItem(EquipmentIndex.HorseHarness, fireEvent: false);

            EventManager.Fire(UIEvent.Item, EventScope.Global);
        }
    }
}
