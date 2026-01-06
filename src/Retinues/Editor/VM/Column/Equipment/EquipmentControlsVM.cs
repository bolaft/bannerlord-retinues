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

        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public bool ShowNavalBattleToggle =>
            Mods.NavalDLC.IsLoaded
            && State.Equipment != null
            && State.Equipment.IsCivilian == false;

        // Sprite tooltips (not action tooltips)
        [DataSourceProperty]
        public Tooltip FieldBattleTooltip =>
            new(L.T("battle_type_field_tooltip", "Field battles."));

        [DataSourceProperty]
        public Tooltip SiegeBattleTooltip =>
            new(L.T("battle_type_siege_tooltip", "Siege battles."));

        [DataSourceProperty]
        public Tooltip NavalBattleTooltip =>
            new(L.T("battle_type_naval_tooltip", "Naval battles."));

        // Checkbox VMs (tooltips + enabled reasons come from controller EditorActions)
        [DataSourceProperty]
        public Checkbox FieldBattleToggle { get; } =
            new(
                action: EquipmentController.SetFieldBattleSet,
                getSelected: () => State.Equipment?.FieldBattleSet ?? false,
                refresh: [UIEvent.Equipment, UIEvent.BattleToggle],
                visibilityGate: () =>
                    State.Mode == EditorMode.Player
                    && State.Equipment != null
                    && State.Equipment.IsCivilian == false
            );

        [DataSourceProperty]
        public Checkbox SiegeBattleToggle { get; } =
            new(
                action: EquipmentController.SetSiegeBattleSet,
                getSelected: () => State.Equipment?.SiegeBattleSet ?? false,
                refresh: [UIEvent.Equipment, UIEvent.BattleToggle],
                visibilityGate: () =>
                    State.Mode == EditorMode.Player
                    && State.Equipment != null
                    && State.Equipment.IsCivilian == false
            );

        [DataSourceProperty]
        public Checkbox NavalBattleToggle { get; } =
            new(
                action: EquipmentController.SetNavalBattleSet,
                getSelected: () => State.Equipment?.NavalBattleSet ?? false,
                refresh: [UIEvent.Equipment, UIEvent.BattleToggle],
                visibilityGate: () =>
                    Mods.NavalDLC.IsLoaded
                    && State.Mode == EditorMode.Player
                    && State.Equipment != null
                    && State.Equipment.IsCivilian == false
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Preview Mode                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool ShowPreviewModeToggle =>
            EditorVM.Page == EditorPage.Equipment && State.Mode == EditorMode.Player;

        [DataSourceProperty]
        public Icon PreviewModeIcon { get; } =
            new(
                tooltipFactory: () =>
                    new(
                        L.T(
                            "preview_mode_toggle_tooltip",
                            "Use preview mode to see how equipment looks without actually applying changes."
                        )
                    ),
                refresh: [UIEvent.Preview, UIEvent.Page, UIEvent.Character],
                visibilityGate: () =>
                    EditorVM.Page == EditorPage.Equipment && State.Mode == EditorMode.Player
            );

        [DataSourceProperty]
        public Checkbox PreviewModeToggle { get; } =
            new(
                action: PreviewController.SetPreviewMode,
                getSelected: () => PreviewController.Enabled,
                refresh: [UIEvent.Preview, UIEvent.Page, UIEvent.Character]
            );

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

        [DataSourceProperty]
        public Icon CraftedIcon { get; } =
            new(
                tooltipFactory: () =>
                    new(
                        L.T(
                            "crafted_items_toggle_tooltip",
                            "Include crafted items in the equipment list."
                        )
                    ),
                refresh: [UIEvent.Slot, UIEvent.Crafted, UIEvent.Page],
                visibilityGate: () =>
                    EditorVM.Page == EditorPage.Equipment && State.Mode == EditorMode.Player
            );

        [DataSourceProperty]
        public Checkbox CraftedToggle { get; } =
            new(
                action: EquipmentController.SetShowCrafted,
                getSelected: () => State.ShowCrafted,
                refresh: [UIEvent.Slot, UIEvent.Crafted, UIEvent.Page],
                visibilityGate: () =>
                    EditorVM.Page == EditorPage.Equipment && State.Mode == EditorMode.Player
            );
    }
}
