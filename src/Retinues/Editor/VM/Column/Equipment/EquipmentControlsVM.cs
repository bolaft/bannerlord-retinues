using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Controllers;
using Retinues.Helpers;
using Retinues.Model.Equipments;
using Retinues.Utilities;
using TaleWorlds.Library;
using TaleWorlds.Localization;

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

        private List<MEquipment> Equipments => _civilian ? CivilianEquipments : BattleEquipments;
        private List<MEquipment> AllEquipments => State.Character.Editable.Equipments;
        private List<MEquipment> BattleEquipments => AllEquipments.FindAll(e => !e.IsCivilian);
        private List<MEquipment> CivilianEquipments => AllEquipments.FindAll(e => e.IsCivilian);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Set Controls                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        int IndexOfByBase(List<MEquipment> list, MEquipment equipment) =>
            EquipmentController.IndexOfByBase(list, equipment);

        [EventListener(UIEvent.Character)]
        [DataSourceProperty]
        public bool ShowSetActionButtons => State.Character != null && !State.Character.IsHero;

        [DataSourceProperty]
        public bool CanCreateSet => ShowSetActionButtons && _cantCreateSetReason == null;

        [DataSourceProperty]
        public bool CanDeleteSet => ShowSetActionButtons && _cantDeleteSetReason == null;

        [DataSourceProperty]
        public Tooltip CanCreateSetTooltip =>
            _cantCreateSetReason == null
                ? new Tooltip(L.T("create_set_tooltip", "Create equipment set"))
                : new Tooltip(_cantCreateSetReason);

        [DataSourceProperty]
        public Tooltip CanDeleteSetTooltip =>
            _cantDeleteSetReason == null
                ? new Tooltip(L.T("delete_set_tooltip", "Delete equipment set"))
                : new Tooltip(_cantDeleteSetReason);

        private TextObject _cantCreateSetReason = null;
        private TextObject _cantDeleteSetReason = null;

        [EventListener(UIEvent.Equipment)]
        private void UpdateSetButtonStates()
        {
            EquipmentController.CanCreateSet(_civilian, out _cantCreateSetReason);
            EquipmentController.CanDeleteSet(_civilian, out _cantDeleteSetReason);

            OnPropertyChanged(nameof(CanCreateSet));
            OnPropertyChanged(nameof(CanDeleteSet));
            OnPropertyChanged(nameof(CanCreateSetTooltip));
            OnPropertyChanged(nameof(CanDeleteSetTooltip));
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
        public bool CanSelectPrevSet
        {
            get
            {
                var i = IndexOfByBase(Equipments, State.Equipment);
                return i > 0;
            }
        }

        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public bool CanSelectNextSet
        {
            get
            {
                var list = Equipments;
                var i = IndexOfByBase(list, State.Equipment);
                return i >= 0 && i < list.Count - 1;
            }
        }

        [DataSourceMethod]
        public void ExecutePrevSet()
        {
            int index = IndexOfByBase(Equipments, State.Equipment);
            if (index <= 0)
                return;

            State.Equipment = Equipments[index - 1];
        }

        [DataSourceMethod]
        public void ExecuteNextSet()
        {
            int index = IndexOfByBase(Equipments, State.Equipment);
            if (index >= Equipments.Count - 1)
                return;

            State.Equipment = Equipments[index + 1];
        }

        [DataSourceMethod]
        public void ExecuteCreateSet()
        {
            if (!CanCreateSet)
                return;

            EquipmentController.CreateSet(_civilian);
        }

        [DataSourceMethod]
        public void ExecuteDeleteSet()
        {
            if (!CanDeleteSet)
                return;

            EquipmentController.DeleteSet(_civilian);
        }
    }
}
