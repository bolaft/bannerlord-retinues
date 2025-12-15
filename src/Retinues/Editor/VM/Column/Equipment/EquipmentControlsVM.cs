using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Helpers;
using Retinues.Model.Equipments;
using Retinues.Utilities;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.SceneNotification;

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

        private bool _showCivilian = false;

        [EventListener(UIEvent.Character)]
        private void ResetShowCivilian()
        {
            _showCivilian = State.Equipment != null && State.Equipment.IsCivilian;
            OnPropertyChanged(nameof(ShowCivilian));
        }

        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public string ToggleShowCivilianButtonText =>
            _showCivilian
                ? L.S("button_show_civilian", "Civilian")
                : L.S("button_show_battle", "Battle");

        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public bool ShowCivilian
        {
            get => _showCivilian;
            set
            {
                if (value == _showCivilian)
                    return;

                // Use the TARGET mode, not the old field.
                List<MEquipment> targetList = value ? CivilianEquipments : BattleEquipments;

                void Apply(MEquipment e)
                {
                    EventManager.FireBatch(() =>
                    {
                        _showCivilian = value;

                        // If we end up keeping the same equipment instance, we still want the UI to refresh.
                        if (!ReferenceEquals(State.Equipment, e))
                            State.Equipment = e; // fires UIEvent.Equipment
                        else
                            EventManager.Fire(UIEvent.Equipment);
                    });
                }

                var equipment = targetList.FirstOrDefault();

                if (equipment == null)
                {
                    Inquiries.Popup(
                        title: value
                            ? L.T("inquiry_no_civilian_sets", "No Civilian Equipments")
                            : L.T("inquiry_no_battle_sets", "No Battle Equipments"),
                        description: L.T(
                                "inquiry_no_equipment_sets_text",
                                "The current character has no {EQUIPMENT_TYPE}.\n\nCreate an empty one?"
                            )
                            .SetTextVariable(
                                "EQUIPMENT_TYPE",
                                value
                                    ? L.T(
                                        "inquiry_no_equipment_sets_civilian",
                                        "civilian equipments"
                                    )
                                    : L.T("inquiry_no_equipment_sets_battle", "battle equipments")
                            ),
                        onConfirm: () =>
                        {
                            var created = MEquipment.Create(civilian: value);

                            // Insert at 0 so it becomes the first of its category (matches your existing UX).
                            State.Character.AddEquipment(created, index: 0);

                            // After mutation, re-read lists and select the new first entry of that category.
                            var added = (
                                value ? CivilianEquipments : BattleEquipments
                            ).FirstOrDefault();
                            Apply(added ?? created);
                        }
                    );
                    return;
                }

                Apply(equipment);
            }
        }

        [DataSourceMethod]
        public void ExecuteToggleShowCivilian() => ShowCivilian = !_showCivilian;

        /* ━━━━━━ Equipments ━━━━━━ */

        private List<MEquipment> Equipments =>
            _showCivilian ? CivilianEquipments : BattleEquipments;
        private List<MEquipment> AllEquipments => State.Character.Editable.Equipments;
        private List<MEquipment> BattleEquipments => AllEquipments.FindAll(e => !e.IsCivilian);
        private List<MEquipment> CivilianEquipments => AllEquipments.FindAll(e => e.IsCivilian);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Set Controls                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public string EquipmentLabel
        {
            get
            {
                var list = Equipments;
                var i = list.IndexOf(State.Equipment);
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
                var i = Equipments.IndexOf(State.Equipment);
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
                var i = list.IndexOf(State.Equipment);
                return i >= 0 && i < list.Count - 1;
            }
        }

        [DataSourceMethod]
        public void ExecutePrevSet()
        {
            int index = Equipments.IndexOf(State.Equipment);
            if (index <= 0)
                return;

            State.Equipment = Equipments[index - 1];
        }

        [DataSourceMethod]
        public void ExecuteNextSet()
        {
            int index = Equipments.IndexOf(State.Equipment);
            if (index >= Equipments.Count - 1)
                return;

            State.Equipment = Equipments[index + 1];
        }
    }
}
