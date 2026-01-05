using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Domain.Equipments.Models;
using Retinues.Editor.Controllers.Equipment;
using Retinues.Editor.Events;
using Retinues.Modules;
using Retinues.UI.Services;
using Retinues.UI.VM;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Column.Equipment
{
    public class EquipmentControlsVM : EventListenerVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Visibility                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool IsVisible => EditorVM.Page == EditorPage.Equipment;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Equipments                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool _civilian = false;

        [EventListener(UIEvent.Character)]
        private void ResetShowCivilian()
        {
            _civilian = State.Equipment != null && State.Equipment.IsCivilian;
            OnPropertyChanged(nameof(ShowCivilian));
        }

        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public string ToggleShowCivilianButtonText =>
            _civilian
                ? L.S("button_show_civilian", "Civilian")
                : L.S("button_show_battle", "Battle");

        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public bool ShowCivilian
        {
            get => _civilian;
            set
            {
                if (value == _civilian)
                    return;

                void Apply(MEquipment e)
                {
                    EventManager.FireBatch(() =>
                    {
                        _civilian = value;

                        if (State.Equipment != e)
                            State.Equipment = e; // fires UIEvent.Equipment
                        else
                            EventManager.Fire(UIEvent.Equipment);
                    });
                }

                EquipmentController.SelectFirstOrPromptCreate(
                    civilian: value,
                    applySelection: Apply,
                    allowCreate: true
                );
            }
        }

        [DataSourceMethod]
        public void ExecuteToggleShowCivilian() => ShowCivilian = !_civilian;

        private static int IndexOfByBase(List<MEquipment> list, MEquipment equipment) =>
            EquipmentController.IndexOfByBase(list, equipment);

        private List<MEquipment> Equipments => EquipmentController.GetEquipments(_civilian);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Set Controls                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Character)]
        [DataSourceProperty]
        public bool ShowSetActionButtons => State.Character != null && !State.Character.IsHero;

        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public string EquipmentLabel
        {
            get
            {
                var list = Equipments;
                var i = IndexOfByBase(list, State.Equipment);
                if (i < 0)
                    return $"0 / {list.Count}";
                return $"{i + 1} / {list.Count}";
            }
        }

        // Buttons

        [DataSourceProperty]
        public Button<bool> PrevSetButton { get; } =
            new(
                action: EquipmentController.SelectPrevSet,
                arg: () => _civilian,
                refresh: [UIEvent.Equipment]
            );

        [DataSourceProperty]
        public Button<bool> NextSetButton { get; } =
            new(
                action: EquipmentController.SelectNextSet,
                arg: () => _civilian,
                refresh: [UIEvent.Equipment]
            );

        [DataSourceProperty]
        public Button<bool> CreateSetButton { get; } =
            new(
                action: EquipmentController.CreateSet,
                arg: () => _civilian,
                refresh: [UIEvent.Equipment]
            );

        [DataSourceProperty]
        public Button<bool> DeleteSetButton { get; } =
            new(
                action: EquipmentController.DeleteSet,
                arg: () => _civilian,
                refresh: [UIEvent.Equipment]
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Battle Types                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public bool ShowBattleTypeToggles =>
            State.Mode == EditorMode.Player
            && State.Equipment != null
            && State.Equipment.IsCivilian == false;

        [EventListener(UIEvent.BattleToggle)]
        [DataSourceProperty]
        public bool FieldBattleSet
        {
            get => State.Equipment?.FieldBattleSet ?? false;
            set => EquipmentController.SetFieldBattleSet.Execute(value);
        }

        [EventListener(UIEvent.BattleToggle)]
        [DataSourceProperty]
        public bool SiegeBattleSet
        {
            get => State.Equipment?.SiegeBattleSet ?? false;
            set => EquipmentController.SetSiegeBattleSet.Execute(value);
        }

        [EventListener(UIEvent.BattleToggle)]
        [DataSourceProperty]
        public bool NavalBattleSet
        {
            get => State.Equipment?.NavalBattleSet ?? false;
            set => EquipmentController.SetNavalBattleSet.Execute(value);
        }

        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public bool ShowNavalBattleToggle =>
            Mods.NavalDLC.IsLoaded
            && State.Equipment != null
            && State.Equipment.IsCivilian == false;

        // Enabled state for the checkbox widgets

        [EventListener(UIEvent.Equipment, UIEvent.BattleToggle)]
        [DataSourceProperty]
        public bool FieldBattleCheckboxEnabled =>
            !FieldBattleSet || EquipmentController.GetFieldBattleDisableReason() == null;

        [EventListener(UIEvent.Equipment, UIEvent.BattleToggle)]
        [DataSourceProperty]
        public bool SiegeBattleCheckboxEnabled =>
            !SiegeBattleSet || EquipmentController.GetSiegeBattleDisableReason() == null;

        [EventListener(UIEvent.Equipment, UIEvent.BattleToggle)]
        [DataSourceProperty]
        public bool NavalBattleCheckboxEnabled =>
            !NavalBattleSet || EquipmentController.GetNavalBattleDisableReason() == null;

        // Tooltips (show disable reason when disabled)

        [DataSourceProperty]
        public Tooltip FieldBattleTooltip =>
            new(L.T("battle_type_field_tooltip", "Field battles."));

        [EventListener(UIEvent.Equipment, UIEvent.BattleToggle)]
        [DataSourceProperty]
        public Tooltip FieldBattleCheckboxTooltip
        {
            get
            {
                var reason = EquipmentController.GetFieldBattleDisableReason();
                if (FieldBattleSet && reason != null)
                    return new Tooltip(reason);

                return FieldBattleSet
                    ? new Tooltip(
                        L.T(
                            "battle_type_field_checkbox_tooltip_disable",
                            "Disable for field battles."
                        )
                    )
                    : new Tooltip(
                        L.T(
                            "battle_type_field_checkbox_tooltip_enable",
                            "Enable for field battles."
                        )
                    );
            }
        }

        [DataSourceProperty]
        public Tooltip SiegeBattleTooltip =>
            new(L.T("battle_type_siege_tooltip", "Siege battles."));

        [EventListener(UIEvent.Equipment, UIEvent.BattleToggle)]
        [DataSourceProperty]
        public Tooltip SiegeBattleCheckboxTooltip
        {
            get
            {
                var reason = EquipmentController.GetSiegeBattleDisableReason();
                if (SiegeBattleSet && reason != null)
                    return new Tooltip(reason);

                return SiegeBattleSet
                    ? new Tooltip(
                        L.T(
                            "battle_type_siege_checkbox_tooltip_disable",
                            "Disable for siege battles."
                        )
                    )
                    : new Tooltip(
                        L.T(
                            "battle_type_siege_checkbox_tooltip_enable",
                            "Enable for siege battles."
                        )
                    );
            }
        }

        [DataSourceProperty]
        public Tooltip NavalBattleTooltip =>
            new(L.T("battle_type_naval_tooltip", "Naval battles."));

        [EventListener(UIEvent.Equipment, UIEvent.BattleToggle)]
        [DataSourceProperty]
        public Tooltip NavalBattleCheckboxTooltip
        {
            get
            {
                var reason = EquipmentController.GetNavalBattleDisableReason();
                if (NavalBattleSet && reason != null)
                    return new Tooltip(reason);

                return NavalBattleSet
                    ? new Tooltip(
                        L.T(
                            "battle_type_naval_checkbox_tooltip_disable",
                            "Disable for naval battles."
                        )
                    )
                    : new Tooltip(
                        L.T(
                            "battle_type_naval_checkbox_tooltip_enable",
                            "Enable for naval battles."
                        )
                    );
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Preview Mode                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool ShowPreviewModeToggle =>
            EditorVM.Page == EditorPage.Equipment && State.Mode == EditorMode.Player;

        [EventListener(UIEvent.Preview)]
        [DataSourceProperty]
        public bool PreviewMode
        {
            get => PreviewController.Enabled;
            set => PreviewController.SetPreviewMode.Execute(value);
        }

        [EventListener(UIEvent.Preview)]
        [DataSourceProperty]
        public Tooltip PreviewModeTooltip =>
            PreviewController.Enabled
                ? new Tooltip(L.T("preview_enable_tooltip", "Enable preview mode."))
                : new Tooltip(L.T("preview_disable_tooltip", "Disable preview mode."));

        [EventListener(UIEvent.Character, UIEvent.Page)]
        private void DisablePreviewOnContextChange()
        {
            if (!PreviewController.Enabled)
                return;

            PreviewController.DisablePreview();
        }

        [EventListener(UIEvent.Preview)]
        [DataSourceProperty]
        public bool ShowPreviewModeText => PreviewController.Enabled;

        [DataSourceProperty]
        public string PreviewModeText => L.S("preview_mode", "Preview Mode");

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Crafted Items                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool ShowCraftedToggle =>
            EditorVM.Page == EditorPage.Equipment && State.Mode == EditorMode.Player;

        [EventListener(UIEvent.Crafted)]
        [DataSourceProperty]
        public bool ShowCrafted
        {
            get => State.ShowCrafted;
            set => State.ShowCrafted = value;
        }

        [EventListener(UIEvent.Slot, UIEvent.Crafted)]
        [DataSourceProperty]
        public Tooltip CraftedToggleTooltip =>
            State.ShowCrafted
                ? new Tooltip(L.T("crafted_items_only_tooltip", "Hide crafted weapons."))
                : new Tooltip(L.T("crafted_items_hide_tooltip", "Include crafted weapons."));
    }
}
