using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Domain.Equipments.Models;
using Retinues.Editor.Controllers.Equipment;
using Retinues.Editor.Events;
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
                refresh: [UIEvent.Character, UIEvent.Equipment]
            );

        [DataSourceProperty]
        public Button<bool> NextSetButton { get; } =
            new(
                action: EquipmentController.SelectNextSet,
                arg: () => _civilian,
                refresh: [UIEvent.Character, UIEvent.Equipment]
            );

        [DataSourceProperty]
        public Button<bool> CreateSetButton { get; } =
            new(
                action: EquipmentController.CreateSet,
                arg: () => _civilian,
                refresh: [UIEvent.Character, UIEvent.Equipment]
            );

        [DataSourceProperty]
        public Button<bool> DeleteSetButton { get; } =
            new(
                action: EquipmentController.DeleteSet,
                arg: () => _civilian,
                refresh: [UIEvent.Character, UIEvent.Equipment]
            );
    }
}
