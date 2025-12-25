using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.VM.Column.Character;
using Retinues.Editor.VM.Column.Equipment;
using Retinues.Model;
using Retinues.Model.Characters;
using Retinues.Utilities;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Column
{
    public class ColumnVM : BaseVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Controls                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly CustomizationControlsVM _customizationControls = new();

        [DataSourceProperty]
        public CustomizationControlsVM CustomizationControls => _customizationControls;

        private readonly EquipmentControlsVM _equipmentControls = new();

        [DataSourceProperty]
        public EquipmentControlsVM EquipmentControls => _equipmentControls;

        private readonly CharacterControlsVM _characterControls = new();

        [DataSourceProperty]
        public CharacterControlsVM CharacterControls => _characterControls;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Model                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private CharacterViewModel _model;

        [DataSourceProperty]
        public CharacterViewModel Model
        {
            get => _model;
            private set
            {
                if (ReferenceEquals(value, _model))
                    return;

                _model = value;
                OnPropertyChanged(nameof(Model));
            }
        }

        [EventListener(UIEvent.Character, UIEvent.Appearance, UIEvent.Page, UIEvent.Library)]
        private void RebuildModel()
        {
            MLibrary.Item.ModelLease lease = null;

            try
            {
                WCharacter character;

                if (EditorVM.Page == EditorPage.Library)
                {
                    lease = State.Instance.LibraryItem?.LeaseModelCharacter();
                    character = lease?.Character;

                    if (character?.Base == null)
                    {
                        Model = null;
                        return;
                    }
                }
                else
                {
                    character = State.Character;

                    if (character?.Base == null)
                    {
                        Model = null;
                        return;
                    }
                }

                var co = character.Base;

                var vm = new CharacterViewModel(CharacterViewModel.StanceTypes.None);

                try
                {
                    vm.FillFrom(co, seed: -1);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                    Model = null;
                    return;
                }

                // Important: apply selected equipment after FillFrom.
                // In Library mode, we want the equipment from the XML-applied character,
                // not the editor-selected equipment.
                if (EditorVM.Page != EditorPage.Library)
                    ApplyEquipmentTo(vm);

                Model = vm;
            }
            finally
            {
                lease?.Dispose();
            }
        }

        // Apply the selected equipment without rebuilding the whole VM.
        [EventListener(UIEvent.Equipment)]
        private void RefreshModelEquipment()
        {
            if (EditorVM.Page == EditorPage.Library)
                return;

            if (Model == null)
            {
                RebuildModel();
                return;
            }

            ApplyEquipmentTo(Model);
        }

        private static void ApplyEquipmentTo(CharacterViewModel vm)
        {
            var equipment = State.Equipment?.Base;
            if (equipment == null)
                return;

            vm.SetEquipment(equipment);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Equipment Button                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool ShowEquipmentButton =>
            EditorVM.Page == EditorPage.Character || EditorVM.Page == EditorPage.Equipment;

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public string EquipmentButtonText =>
            EditorVM.Page == EditorPage.Equipment
                ? L.S("close_equipment_button_text", "Back")
                : L.S("equipment_button_text", "Equipment");

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public string EquipmentButtonBrush =>
            EditorVM.Page == EditorPage.Equipment ? "ButtonBrush3" : "ButtonBrush1";

        [DataSourceMethod]
        public void ExecuteToggleEquipmentMode()
        {
            if (EditorVM.Page == EditorPage.Equipment)
                EditorVM.SetPage(EditorPage.Character);
            else
                EditorVM.SetPage(EditorPage.Equipment);
        }
    }
}
