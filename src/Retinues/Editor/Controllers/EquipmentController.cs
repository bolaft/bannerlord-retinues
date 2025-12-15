using Retinues.Model.Equipments;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.Editor.Controllers
{
    public class EquipmentController : BaseController
    {
        /// <summary>
        /// Check if the item can be equipped.
        /// </summary>
        public static bool CanEquipItem(WItem item, out TextObject reason) =>
            Check(
                [
                    (
                        () => item.IsEquippableByCharacter(State.Character),
                        L.T("cant_equip_reason_skill", "{VALUE} {SKILL}")
                            .SetTextVariable("VALUE", item.Difficulty)
                            .SetTextVariable("SKILL", item.RelevantSkill?.Name)
                    ),
                    (
                        () => !State.Equipment.IsCivilian || item.IsCivilian,
                        L.T("cant_equip_reason_civilian", "Not civilian")
                            .SetTextVariable("VALUE", item.Difficulty)
                            .SetTextVariable("SKILL", item.RelevantSkill?.Name)
                    ),
                ],
                out reason
            );

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
