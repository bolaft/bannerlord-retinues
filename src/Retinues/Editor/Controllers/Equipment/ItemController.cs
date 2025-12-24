using Retinues.Model.Equipments;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Editor.Controllers.Equipment
{
    public class ItemController : EditorController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Equip                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Equip an item into the currently selected slot.
        /// </summary>
        public static EditorAction<WItem> Equip { get; } =
            Action<WItem>("EquipItem")
                .AddCondition(
                    item => item.IsEquippableByCharacter(State.Character),
                    item =>
                        L.T("cant_equip_reason_skill", "{SKILL} {VALUE}")
                            .SetTextVariable("VALUE", item.Difficulty)
                            .SetTextVariable("SKILL", item.RelevantSkill?.Name)
                )
                .AddCondition(
                    item => !State.Equipment.IsCivilian || item.IsCivilian,
                    item =>
                        L.T("cant_equip_reason_civilian", "Not civilian")
                            .SetTextVariable("VALUE", item.Difficulty)
                            .SetTextVariable("SKILL", item.RelevantSkill?.Name)
                )
                .ExecuteWith(EquipItem)
                .Fire(UIEvent.Item, EventScope.Global);

        private static void EquipItem(WItem item)
        {
            if (State.Equipment == null)
                return;

            var slot = State.Instance.Slot;
            var equipped = State.Equipment.Get(slot);

            if (equipped == item)
                return;

            State.Equipment.Set(slot, item);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Unequip                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Unequip an item from a specific slot.
        /// </summary>
        public static EditorAction<EquipmentIndex> Unequip { get; } =
            Action<EquipmentIndex>("UnequipItem")
                .AddCondition(
                    slot => State.Equipment != null && State.Equipment.Get(slot) != null,
                    L.T("cant_unequip_reason_empty", "Nothing to unequip.")
                )
                .ExecuteWith(UnequipItem)
                .Fire(UIEvent.Item, EventScope.Global);

        private static void UnequipItem(EquipmentIndex slot)
        {
            if (State.Equipment == null)
                return;

            var equipped = State.Equipment.Get(slot);
            if (equipped == null)
                return;

            State.Equipment.Set(slot, null);

            if (slot == EquipmentIndex.Horse)
            {
                // No behavior change: unequipping a horse also clears harness, without double-firing.
                UnequipHarness();
            }
        }

        private static void UnequipHarness()
        {
            if (State.Equipment == null)
                return;

            var harness = State.Equipment.Get(EquipmentIndex.HorseHarness);
            if (harness == null)
                return;

            State.Equipment.Set(EquipmentIndex.HorseHarness, null);
        }
    }
}
