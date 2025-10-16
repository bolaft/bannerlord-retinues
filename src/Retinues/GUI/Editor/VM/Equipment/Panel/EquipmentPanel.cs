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

        public readonly EditorVM Editor;

        public EquipmentPanelVM(EditorVM editor)
        {
            Log.Info("Building EquipmentPanelVM...");

            Editor = editor;

            // Components
            HeadSlot = new(editor, EquipmentIndex.Head, L.S("head_slot_text", "Head"));
            CapeSlot = new(editor, EquipmentIndex.Cape, L.S("cape_slot_text", "Cape"));
            BodySlot = new(editor, EquipmentIndex.Body, L.S("body_slot_text", "Body"));
            GlovesSlot = new(editor, EquipmentIndex.Gloves, L.S("gloves_slot_text", "Gloves"));
            LegSlot = new(editor, EquipmentIndex.Leg, L.S("leg_slot_text", "Legs"));
            HorseSlot = new(editor, EquipmentIndex.Horse, L.S("horse_slot_text", "Horse"));
            HorseHarnessSlot = new(
                editor,
                EquipmentIndex.HorseHarness,
                L.S("horse_harness_slot_text", "Horse Harness")
            );
            WeaponItemBeginSlotSlot = new(
                editor,
                EquipmentIndex.WeaponItemBeginSlot,
                L.S("weapon_1_slot_text", "Weapon 1")
            );
            Weapon1Slot = new(
                editor,
                EquipmentIndex.Weapon1,
                L.S("weapon_2_slot_text", "Weapon 2")
            );
            Weapon2Slot = new(
                editor,
                EquipmentIndex.Weapon2,
                L.S("weapon_3_slot_text", "Weapon 3")
            );
            Weapon3Slot = new(
                editor,
                EquipmentIndex.Weapon3,
                L.S("weapon_4_slot_text", "Weapon 4")
            );
        }

        public void Initialize()
        {
            Log.Info("Initializing EquipmentPanelVM...");

            // Components
            foreach (var slot in Slots)
                slot?.Initialize();

            // Default selection
            Select(WeaponItemBeginSlotSlot);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public EquipmentSlotVM HeadSlot { get; private set; }

        [DataSourceProperty]
        public EquipmentSlotVM CapeSlot { get; private set; }

        [DataSourceProperty]
        public EquipmentSlotVM BodySlot { get; private set; }

        [DataSourceProperty]
        public EquipmentSlotVM GlovesSlot { get; private set; }

        [DataSourceProperty]
        public EquipmentSlotVM LegSlot { get; private set; }

        [DataSourceProperty]
        public EquipmentSlotVM HorseSlot { get; private set; }

        [DataSourceProperty]
        public EquipmentSlotVM HorseHarnessSlot { get; private set; }

        [DataSourceProperty]
        public EquipmentSlotVM WeaponItemBeginSlotSlot { get; private set; }

        [DataSourceProperty]
        public EquipmentSlotVM Weapon1Slot { get; private set; }

        [DataSourceProperty]
        public EquipmentSlotVM Weapon2Slot { get; private set; }

        [DataSourceProperty]
        public EquipmentSlotVM Weapon3Slot { get; private set; }

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
                    if (r?.IsSelected == true)
                        return r;
                }

                return null;
            }
        }

        private bool _selecting;
        public void Select(EquipmentSlotVM slot)
        {
            if (_selecting) return;
            _selecting = true;
            try
            {
                foreach (var r in Slots) if (r != null) r.IsSelected = ReferenceEquals(r, slot);
            }
            finally { _selecting = false; }
        }
    }
}
