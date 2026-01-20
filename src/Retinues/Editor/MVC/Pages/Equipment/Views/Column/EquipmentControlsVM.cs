using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Compatibility;
using Retinues.Domain.Equipments.Models;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Pages.Equipment.Controllers;
using Retinues.Interface.Components;
using Retinues.Interface.Services;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Pages.Equipment.Views.Column
{
    /// <summary>
    /// ViewModel for the equipment controls.
    /// </summary>
    public class EquipmentControlsVM : EventListenerVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Visibility                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool IsVisible => State.Page == EditorPage.Equipment;

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
                var list = EquipmentController.GetEquipments(_civilian);
                ;
                var i = EquipmentController.IndexOfByBase(list, State.Equipment);
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
        //                  Copy / Paste Buttons                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const string HoveredColor = "#fdae1ae8";
        private const string EnabledColor = "#f8d28ab4";
        private const string DisabledColor = "#46433db4";

        [DataSourceProperty]
        public Button<bool> CopyEquipmentButton { get; } =
            new(
                action: ClipboardController.CopyEquipment,
                arg: () => false,
                refresh: [UIEvent.Equipment],
                colorFactory: () => EnabledColor,
                hoverColor: HoveredColor
            );

        [DataSourceProperty]
        public Button<bool> PasteEquipmentButton { get; } =
            new(
                action: ClipboardController.PasteEquipment,
                arg: () => false,
                refresh: [UIEvent.Equipment, UIEvent.Item, UIEvent.Clipboard],
                colorFactory: () => ClipboardController.HasClipboard ? EnabledColor : DisabledColor,
                hoverColor: HoveredColor
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

        [DataSourceProperty]
        public Tooltip FieldBattleTooltip => new(L.T("battle_type_field_tooltip", "Field Battles"));

        [DataSourceProperty]
        public Tooltip SiegeBattleTooltip => new(L.T("battle_type_siege_tooltip", "Siege Battles"));

        [DataSourceProperty]
        public Tooltip NavalBattleTooltip => new(L.T("battle_type_naval_tooltip", "Naval Battles"));

        [DataSourceProperty]
        public Checkbox FieldBattleToggle { get; } =
            new(
                action: EquipmentController.SetFieldBattleSet,
                getSelected: () => State.Equipment?.FieldBattleSet ?? false,
                refresh: [UIEvent.Equipment, UIEvent.BattleType],
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
                refresh: [UIEvent.Equipment, UIEvent.BattleType],
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
                refresh: [UIEvent.Equipment, UIEvent.BattleType],
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
            State.Page == EditorPage.Equipment && State.Mode == EditorMode.Player;

        [DataSourceProperty]
        public Icon PreviewModeIcon { get; } =
            new(
                tooltip: new(L.T("preview_mode_toggle_tooltip", "Preview Mode")),
                refresh: [UIEvent.Preview, UIEvent.Page, UIEvent.Character],
                visibilityGate: () =>
                    State.Page == EditorPage.Equipment && State.Mode == EditorMode.Player
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
        //                      Crafted Items                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool ShowCraftedToggle =>
            State.Page == EditorPage.Equipment && State.Mode == EditorMode.Player;

        [DataSourceProperty]
        public Icon CraftedIcon { get; } =
            new(
                tooltipFactory: () => new(L.T("crafted_items_toggle_tooltip", "Crafted Equipment")),
                refresh: [UIEvent.Slot, UIEvent.Crafted, UIEvent.Page],
                visibilityGate: () =>
                    State.Page == EditorPage.Equipment && State.Mode == EditorMode.Player
            );

        [DataSourceProperty]
        public Checkbox CraftedToggle { get; } =
            new(
                action: EquipmentController.SetShowCrafted,
                getSelected: () => State.ShowCrafted,
                refresh: [UIEvent.Slot, UIEvent.Crafted, UIEvent.Page],
                visibilityGate: () =>
                    State.Page == EditorPage.Equipment && State.Mode == EditorMode.Player
            );
    }
}
