using Retinues.Configuration;
using Retinues.Editor.Controllers.Equipment;
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

        private static bool IsPlayerMode => State.Mode == EditorMode.Player;
        private static bool WeightLimitActive => IsPlayerMode && Settings.EquipmentWeightLimit;
        private static bool ValueLimitActive => IsPlayerMode && Settings.EquipmentValueLimit;

        [EventListener(UIEvent.Item)]
        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public bool IsWeightLimitVisible => WeightLimitActive;

        [EventListener(UIEvent.Item)]
        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public bool IsValueLimitVisible => ValueLimitActive;

        [EventListener(UIEvent.Item)]
        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public string WeightValueText
        {
            get
            {
                var eq = State.Equipment;
                if (eq == null)
                    return string.Empty;

                return L.T("equipment_panel_weight_value", "{WEIGHT} kg")
                    .SetTextVariable("WEIGHT", eq.Weight.ToString("0.0"))
                    .ToString();
            }
        }

        [EventListener(UIEvent.Item)]
        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public string WeightLimitText
        {
            get
            {
                if (!WeightLimitActive)
                    return string.Empty;

                int tier = State.Character?.Tier ?? 0;
                float limit = ItemController.GetEquipmentWeightLimit(tier);

                return L.T("equipment_panel_weight_limit", "{LIMIT} kg")
                    .SetTextVariable("LIMIT", limit.ToString("0.0"))
                    .ToString();
            }
        }

        [EventListener(UIEvent.Item)]
        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public string ValueValueText
        {
            get
            {
                var eq = State.Equipment;
                if (eq == null)
                    return string.Empty;

                return L.T("equipment_panel_value_value", "{VALUE} denars")
                    .SetTextVariable("VALUE", eq.Value)
                    .ToString();
            }
        }

        [EventListener(UIEvent.Item)]
        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public string ValueLimitText
        {
            get
            {
                if (!ValueLimitActive)
                    return string.Empty;

                int tier = State.Character?.Tier ?? 0;
                int limit = ItemController.GetEquipmentValueLimit(tier);

                return L.T("equipment_panel_value_limit", "{LIMIT} denars")
                    .SetTextVariable("LIMIT", limit)
                    .ToString();
            }
        }

        [EventListener(UIEvent.Item)]
        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public Color WeightValueColor
        {
            get
            {
                var eq = State.Equipment;
                if (eq == null)
                    return Color.White;

                if (!WeightLimitActive)
                    return Color.White;

                int tier = State.Character?.Tier ?? 0;
                float limit = ItemController.GetEquipmentWeightLimit(tier);
                if (limit <= 0f)
                    return Color.White;

                float ratio = eq.Weight / limit;

                return Utilities.Colors.ProximityLimitColor(
                    ratio,
                    saturation: 0.78f,
                    tintStrength: 0.88f
                );
            }
        }

        [EventListener(UIEvent.Item)]
        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public Color ValueValueColor
        {
            get
            {
                var eq = State.Equipment;
                if (eq == null)
                    return Color.White;

                if (!ValueLimitActive)
                    return Color.White;

                int tier = State.Character?.Tier ?? 0;
                int limit = ItemController.GetEquipmentValueLimit(tier);
                if (limit <= 0)
                    return Color.White;

                float ratio = (float)eq.Value / limit;

                return Utilities.Colors.ProximityLimitColor(
                    ratio,
                    saturation: 0.78f,
                    tintStrength: 0.88f
                );
            }
        }

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
