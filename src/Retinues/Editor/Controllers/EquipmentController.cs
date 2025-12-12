using Retinues.Model.Equipments;

namespace Retinues.Editor.Controllers
{
    public class EquipmentController : BaseController
    {
        public static void EquipItem(WItem item)
        {
            var equipped = State.Equipment.GetItem(State.Instance.Slot);
            if (equipped == item)
                return; // Already equipped.

            State.Equipment.SetItem(State.Instance.Slot, item);

            EventManager.Fire(UIEvent.Item, EventScope.Global);
        }
    }
}
