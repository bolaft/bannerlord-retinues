using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Controllers.Equipment;
using Retinues.Helpers;
using Retinues.Model.Equipments;
using Retinues.Utilities;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Column.Equipment
{
    public class EquipmentControlsVM : BaseVM
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

        /* ━━━━━ Show Civilian ━━━━ */

        private bool _civilian = false;

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

                        // If we end up keeping the same equipment instance, we still want the UI to refresh.
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

        /* ━━━━━━ Equipments ━━━━━━ */

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
        public bool CanCreateSet =>
            ShowSetActionButtons && EquipmentController.CreateSet.Allow(_civilian);

        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public bool CanDeleteSet =>
            ShowSetActionButtons && EquipmentController.DeleteSet.Allow(_civilian);

        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public Tooltip CanCreateSetTooltip
        {
            get
            {
                var reason = EquipmentController.CreateSet.Reason(_civilian);
                return reason == null
                    ? new Tooltip(L.T("create_set_tooltip", "Create equipment set"))
                    : new Tooltip(reason);
            }
        }

        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public Tooltip CanDeleteSetTooltip
        {
            get
            {
                var reason = EquipmentController.DeleteSet.Reason(_civilian);
                return reason == null
                    ? new Tooltip(L.T("delete_set_tooltip", "Delete equipment set"))
                    : new Tooltip(reason);
            }
        }

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

        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public bool CanSelectPrevSet => EquipmentController.SelectPrevSet.Allow(_civilian);

        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public bool CanSelectNextSet => EquipmentController.SelectNextSet.Allow(_civilian);

        [DataSourceMethod]
        public void ExecutePrevSet() => EquipmentController.SelectPrevSet.Execute(_civilian);

        [DataSourceMethod]
        public void ExecuteNextSet() => EquipmentController.SelectNextSet.Execute(_civilian);

        [DataSourceMethod]
        public void ExecuteCreateSet()
        {
            if (!CanCreateSet)
                return;

            EquipmentController.CreateSet.Execute(_civilian);
        }

        [DataSourceMethod]
        public void ExecuteDeleteSet()
        {
            if (!CanDeleteSet)
                return;

            EquipmentController.DeleteSet.Execute(_civilian);
        }
    }
}
