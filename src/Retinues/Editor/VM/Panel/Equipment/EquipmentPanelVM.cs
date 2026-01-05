using Retinues.Editor.Events;
using Retinues.UI.Services;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Panel.Equipment
{
    /// <summary>
    /// Character details panel.
    /// </summary>
    public partial class EquipmentPanelVM : EventListenerVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Visibility                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool IsVisible => EditorVM.Page == EditorPage.Equipment;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Infos                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Item)]
        [DataSourceProperty]
        public string InfoText =>
            L.T("equipment_panel_info", "{WEIGHT} kg / {VALUE} denars")
                .SetTextVariable("WEIGHT", State.Equipment.Weight)
                .SetTextVariable("VALUE", State.Equipment.Value)
                .ToString();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Weapons                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public EquipmentSlotVM Weapon0Slot { get; } =
            new(EquipmentIndex.Weapon0, L.S("equipment_slot_weapon_0", "Weapon 1"));

        [DataSourceProperty]
        public EquipmentSlotVM Weapon1Slot { get; } =
            new(EquipmentIndex.Weapon1, L.S("equipment_slot_weapon_1", "Weapon 2"));

        [DataSourceProperty]
        public EquipmentSlotVM Weapon2Slot { get; } =
            new(EquipmentIndex.Weapon2, L.S("equipment_slot_weapon_2", "Weapon 3"));

        [DataSourceProperty]
        public EquipmentSlotVM Weapon3Slot { get; } =
            new(EquipmentIndex.Weapon3, L.S("equipment_slot_weapon_3", "Weapon 4"));

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Armor                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public EquipmentSlotVM HeadSlot { get; } =
            new(EquipmentIndex.Head, L.S("equipment_slot_head", "Head"));

        [DataSourceProperty]
        public EquipmentSlotVM CapeSlot { get; } =
            new(EquipmentIndex.Cape, L.S("equipment_slot_cape", "Cape"));

        [DataSourceProperty]
        public EquipmentSlotVM BodySlot { get; } =
            new(EquipmentIndex.Body, L.S("equipment_slot_body", "Body"));

        [DataSourceProperty]
        public EquipmentSlotVM GlovesSlot { get; } =
            new(EquipmentIndex.Gloves, L.S("equipment_slot_gloves", "Gloves"));

        [DataSourceProperty]
        public EquipmentSlotVM LegSlot { get; } =
            new(EquipmentIndex.Leg, L.S("equipment_slot_leg", "Legs"));

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Mount                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public EquipmentSlotVM HorseSlot { get; } =
            new(EquipmentIndex.Horse, L.S("equipment_slot_horse", "Horse"));

        [DataSourceProperty]
        public EquipmentSlotVM HorseHarnessSlot { get; } =
            new(EquipmentIndex.HorseHarness, L.S("equipment_slot_horse_harness", "Harness"));
    }
}
