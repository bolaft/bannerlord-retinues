using Retinues.Domain.Equipments.Wrappers;
using Retinues.Editor.Events;
using Retinues.UI.Services;
using TaleWorlds.Core;

namespace Retinues.Editor.Controllers.Equipment
{
    public class ItemController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Equip                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<WItem> Equip { get; } =
            Action<WItem>("EquipItem")
                .AddCondition(
                    _ => State.Equipment != null,
                    L.T("cant_equip_reason_no_equipment", "No equipment set")
                )
                .AddCondition(
                    item => item.IsEquippableByCharacter(State.Character),
                    item =>
                        L.T("cant_equip_reason_skill", "{SKILL} {VALUE}")
                            .SetTextVariable("VALUE", item.Difficulty)
                            .SetTextVariable("SKILL", item.RelevantSkill?.Name)
                )
                .AddCondition(
                    item => State.Equipment?.IsCivilian == false || item.IsCivilian,
                    _ => L.T("cant_equip_reason_civilian", "Not civilian")
                )
                .AddCondition(
                    item => IsCompatibleWithCurrentEquipment(item),
                    _ => L.T("cant_equip_reason_mount_compat", "Incompatible")
                )
                .ExecuteWith(EquipItem)
                .Fire(UIEvent.Item);

        private static bool IsCompatibleWithCurrentEquipment(WItem item)
        {
            if (State.Equipment == null)
                return true;

            var slot = EditorState.Instance.Slot;

            // Only enforce when equipping a harness onto an already-equipped horse.
            // (Equipping a horse will auto-clear an incompatible harness instead.)
            if (slot == EquipmentIndex.HorseHarness)
            {
                var horse = State.Equipment.Get(EquipmentIndex.Horse);
                if (horse == null)
                    return true;

                return horse.IsCompatibleWith(item);
            }

            return true;
        }

        private static void EquipItem(WItem item)
        {
            if (State.Equipment == null)
                return;

            var slot = EditorState.Instance.Slot;
            var equipped = State.Equipment.Get(slot);

            if (equipped == item)
                return;

            // Safety: if something bypasses conditions, still prevent incompatible harness equip.
            if (slot == EquipmentIndex.HorseHarness)
            {
                var horse = State.Equipment.Get(EquipmentIndex.Horse);
                if (horse != null && !horse.IsCompatibleWith(item))
                    return;
            }

            State.Equipment.Set(slot, item);

            // If a new horse makes the currently equipped harness incompatible, clear the harness.
            if (slot == EquipmentIndex.Horse)
            {
                var harness = State.Equipment.Get(EquipmentIndex.HorseHarness);
                if (harness != null && !item.IsCompatibleWith(harness))
                {
                    State.Equipment.Set(EquipmentIndex.HorseHarness, null);
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Unequip                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<EquipmentIndex> Unequip { get; } =
            Action<EquipmentIndex>("UnequipItem")
                .AddCondition(
                    slot => State.Equipment != null && State.Equipment.Get(slot) != null,
                    L.T("cant_unequip_reason_empty", "Nothing to unequip.")
                )
                .DefaultTooltip(L.T("unequip_item_tooltip", "Unequip this item."))
                .ExecuteWith(UnequipItem)
                .Fire(UIEvent.Item);

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
