using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
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

        private bool _showCivilian = false;

        [EventListener(UIEvent.Character)]
        private void ResetShowCivilian()
        {
            _showCivilian = State.Equipment.IsCivilian;
            OnPropertyChanged(nameof(ShowCivilian));
        }

        [DataSourceProperty]
        public bool ShowCivilian
        {
            get => _showCivilian;
            set
            {
                if (value == _showCivilian)
                    return;

                _showCivilian = value;

                State.Equipment = Equipments.FirstOrDefault();

                OnPropertyChanged(nameof(ShowCivilian));
            }
        }

        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public bool CanToggleShowCivilian =>
            BattleEquipments.Count > 0 && CivilianEquipments.Count > 0;

        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public Tooltip CivilianTooltip =>
            ShowCivilian
                ? CanToggleShowCivilian
                    ? new Tooltip(L.S("show_battle_tooltip", "Show battle equipments."))
                    : new Tooltip(
                        L.S("only_civilian_tooltip", "Only civilian equipments are available.")
                    )
                : CanToggleShowCivilian
                    ? new Tooltip(L.S("show_civilian_tooltip", "Show civilian equipments."))
                    : new Tooltip(
                        L.S("only_battle_tooltip", "Only battle equipments are available.")
                    );

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
                int i = Equipments.IndexOf(State.Equipment);
                return $"{i + 1} / {Equipments.Count}";
            }
        }

        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public bool CanSelectPrevSet => Equipments.IndexOf(State.Equipment) > 0;

        [EventListener(UIEvent.Equipment)]
        [DataSourceProperty]
        public bool CanSelectNextSet => Equipments.IndexOf(State.Equipment) < Equipments.Count - 1;

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
