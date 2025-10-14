using System.Collections.Generic;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Equipment.Panel
{
    [SafeClass]
    public sealed class EquipmentPanelVM : BaseComponent
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public EquipmentPanelVM()
        {
            // Select first slot by default
            Select(WeaponItemBeginSlotSlot);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public EquipmentSlotVM HeadSlot { get; } =
            new(EquipmentIndex.Head, L.S("head_slot_text", "Head"));

        [DataSourceProperty]
        public EquipmentSlotVM CapeSlot { get; } =
            new(EquipmentIndex.Cape, L.S("cape_slot_text", "Cape"));

        [DataSourceProperty]
        public EquipmentSlotVM BodySlot { get; } =
            new(EquipmentIndex.Body, L.S("body_slot_text", "Body"));

        [DataSourceProperty]
        public EquipmentSlotVM GlovesSlot { get; } =
            new(EquipmentIndex.Gloves, L.S("gloves_slot_text", "Gloves"));

        [DataSourceProperty]
        public EquipmentSlotVM LegSlot { get; } =
            new(EquipmentIndex.Leg, L.S("leg_slot_text", "Legs"));

        [DataSourceProperty]
        public EquipmentSlotVM HorseSlot { get; } =
            new(EquipmentIndex.Horse, L.S("horse_slot_text", "Horse"));

        [DataSourceProperty]
        public EquipmentSlotVM HorseHarnessSlot { get; } =
            new(EquipmentIndex.HorseHarness, L.S("horse_harness_slot_text", "Horse Harness"));

        [DataSourceProperty]
        public EquipmentSlotVM WeaponItemBeginSlotSlot { get; } =
            new(EquipmentIndex.WeaponItemBeginSlot, L.S("weapon_1_slot_text", "Weapon 1"));

        [DataSourceProperty]
        public EquipmentSlotVM Weapon1Slot { get; } =
            new(EquipmentIndex.Weapon1, L.S("weapon_2_slot_text", "Weapon 2"));

        [DataSourceProperty]
        public EquipmentSlotVM Weapon2Slot { get; } =
            new(EquipmentIndex.Weapon2, L.S("weapon_3_slot_text", "Weapon 3"));

        [DataSourceProperty]
        public EquipmentSlotVM Weapon3Slot { get; } =
            new(EquipmentIndex.Weapon3, L.S("weapon_4_slot_text", "Weapon 4"));

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Slots                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public IEnumerable<EquipmentSlotVM> Slots
        {
            get
            {
                yield return HeadSlot;
                yield return CapeSlot;
                yield return BodySlot;
                yield return GlovesSlot;
                yield return LegSlot;

                yield return WeaponItemBeginSlotSlot;
                yield return Weapon1Slot;
                yield return Weapon2Slot;
                yield return Weapon3Slot;

                yield return HorseSlot;
                yield return HorseHarnessSlot;
            }
        }

        public EquipmentSlotVM Selection
        {
            get
            {
                foreach (var r in Slots)
                {
                    if (r.IsSelected)
                        return r;
                }

                return null;
            }
        }

        public void Select(EquipmentSlotVM slot)
        {
            foreach (var r in Slots)
                r.IsSelected = ReferenceEquals(r, slot);
        }
    }
}
